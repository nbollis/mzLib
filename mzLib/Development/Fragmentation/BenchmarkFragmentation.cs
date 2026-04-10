// Copyright 2012, 2013, 2014 Derek J. Bailey
// Modified work copyright 2016 Stefan Solntsev
//
// This file (TestFragments.cs) is part of Proteomics.
//
// Proteomics is free software: you can redistribute it and/or modify it
// under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Proteomics is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public
// License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with Proteomics. If not, see <http://www.gnu.org/licenses/>.

using MassSpectrometry;
using NUnit.Framework;
using Omics.Fragmentation;
using Omics.Modifications;
using Proteomics.ProteolyticDigestion;
using UsefulProteomicsDatabases;
using Assert = NUnit.Framework.Legacy.ClassicAssert;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Development
{
    [TestFixture]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public sealed class BenchmarkFragmentation
    {
        
        [Test]
        public static void Benchmark_ParallelFragmentation()
        {
            // ── Edit this label before each run to identify what changed ──────
            const string runLabel = "baseline";
            // ─────────────────────────────────────────────────────────────────

            // Load proteins from the cRAP database with reverse decoys to maximize peptide count
            //var dbPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "DatabaseTests", "cRAP_databaseGPTMD.xml");

            // This path points to a human database where I have run GPTMD with a large number of variable modifications, which should yield a very large number of peptides and fragments to maximize the fragmentation time and thus the potential speedup from parallelization. Adjust the path as needed to point to a suitable test database on your machine.
            // I'm specifically interested in benchmarking performance with neutral losses, and a standard uniprot xml does not contain neutral losses
            var dbPath = @"D:\Proteomes\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";

            var loadSw = Stopwatch.StartNew();
            var proteins = ProteinDbLoader.LoadProteinXML(dbPath, true, DecoyType.Reverse, Mods.AllKnownMods, false, null, out _, maxHeterozygousVariants: 0);
            loadSw.Stop();
            var loadElapsed = loadSw.Elapsed;

            // Digest all proteins into peptides
            var digestionParams = new DigestionParams();
            var digestionSw = Stopwatch.StartNew();
            var peptides = proteins
                .SelectMany(p => p.Digest(digestionParams, new List<Modification>(), new List<Modification>()))
                .ToList();
            digestionSw.Stop();
            var digestionElapsed = digestionSw.Elapsed;

            // Peptide distribution statistics
            int minLength = peptides.Min(p => p.Length);
            int maxLength = peptides.Max(p => p.Length);
            double avgLength = peptides.Average(p => p.Length);
            int peptidesWithMods = peptides.Count(p => p.NumMods > 0);

            // Warm up: trigger JIT compilation and lazy initialization before timing
            var warmupProducts = new List<Product>();
            peptides[0].Fragment(DissociationType.HCD, FragmentationTerminus.Both, warmupProducts);

            // Serial benchmark
            long serialFragmentCount = 0;
            var serialProducts = new List<Product>();
            var sw = Stopwatch.StartNew();
            foreach (var peptide in peptides)
            {
                peptide.Fragment(DissociationType.HCD, FragmentationTerminus.Both, serialProducts);
                serialFragmentCount += serialProducts.Count;
            }
            sw.Stop();
            var serialElapsed = sw.Elapsed;

            // Parallel benchmark: each thread owns its List<Product> to avoid contention
            long parallelFragmentCount = 0;
            sw.Restart();
            Parallel.ForEach(
                peptides,
                () => new List<Product>(),
                (peptide, _, localProducts) =>
                {
                    peptide.Fragment(DissociationType.HCD, FragmentationTerminus.Both, localProducts);
                    Interlocked.Add(ref parallelFragmentCount, localProducts.Count);
                    return localProducts;
                },
                _ => { });
            sw.Stop();
            var parallelElapsed = sw.Elapsed;

            int processorCount = Environment.ProcessorCount;
            double digestionThroughput = peptides.Count / digestionElapsed.TotalSeconds;
            double serialThroughput = peptides.Count / serialElapsed.TotalSeconds;
            double parallelThroughput = peptides.Count / parallelElapsed.TotalSeconds;
            double speedup = serialElapsed.TotalSeconds / parallelElapsed.TotalSeconds;
            double parallelEfficiency = speedup / processorCount * 100.0;

            const int labelWidth = -30;
            void Section(string title) 
            {
                TestContext.Out.WriteLine();
                TestContext.Out.WriteLine($"  {title}");
                TestContext.Out.WriteLine($"  {new string('-', title.Length)}");
            }
            void Row(string label, string value) =>
                TestContext.Out.WriteLine($"  {label,labelWidth}  {value}");

            TestContext.Out.WriteLine();
            TestContext.Out.WriteLine("========================================");
            TestContext.Out.WriteLine("  Fragmentation Benchmark Results");
            TestContext.Out.WriteLine("========================================");

            Section("Database Loading");
            Row("Proteins loaded:",      $"{proteins.Count:N0}");
            Row("Loading elapsed:",      $"{loadElapsed.TotalSeconds:F3} s");

            Section("Digestion");
            Row("Peptides digested:",    $"{peptides.Count:N0}");
            Row("Elapsed:",              $"{digestionElapsed.TotalSeconds:F3} s");
            Row("Throughput:",           $"{digestionThroughput:N0} peptides/s");
            Row("Length (min/avg/max):", $"{minLength} / {avgLength:F1} / {maxLength}");
            Row("Peptides with mods:",   $"{peptidesWithMods:N0}  ({100.0 * peptidesWithMods / peptides.Count:F1}%)");

            Section("Serial Fragmentation");
            Row("Fragment count:",       $"{serialFragmentCount:N0}");
            Row("Avg fragments/peptide:",$"{(double)serialFragmentCount / peptides.Count:F1}");
            Row("Elapsed:",              $"{serialElapsed.TotalSeconds:F3} s");
            Row("Throughput:",           $"{serialThroughput:N0} peptides/s");

            Section("Parallel Fragmentation");
            Row("Fragment count:",       $"{parallelFragmentCount:N0}");
            Row("Elapsed:",              $"{parallelElapsed.TotalSeconds:F3} s");
            Row("Throughput:",           $"{parallelThroughput:N0} peptides/s");

            Section("Parallelization Summary");
            Row("Logical processors:",   $"{processorCount}");
            Row("Speedup:",              $"{speedup:F2}x");
            Row("Parallel efficiency:",  $"{parallelEfficiency:F1}%");

            TestContext.Out.WriteLine();
            TestContext.Out.WriteLine("========================================");

            // ── Persist this run and print the comparison table ───────────────
            var record = new BenchmarkRecord
            {
                Timestamp              = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                Label                  = runLabel,
                Branch                 = BenchmarkResultsWriter.GetGitBranch(),
                Proteins               = proteins.Count,
                Peptides               = peptides.Count,
                LoadElapsed_s          = loadElapsed.TotalSeconds,
                DigestionElapsed_s     = digestionElapsed.TotalSeconds,
                DigestionPps           = digestionThroughput,
                AvgPeptideLength       = avgLength,
                PeptidesWithMods_pct   = 100.0 * peptidesWithMods / peptides.Count,
                SerialFragments        = serialFragmentCount,
                AvgFragsPerPeptide     = (double)serialFragmentCount / peptides.Count,
                SerialElapsed_s        = serialElapsed.TotalSeconds,
                SerialPps              = serialThroughput,
                ParallelElapsed_s      = parallelElapsed.TotalSeconds,
                ParallelPps            = parallelThroughput,
                LogicalProcessors      = processorCount,
                Speedup                = speedup,
                ParallelEfficiency_pct = parallelEfficiency
            };

            BenchmarkResultsWriter.Append(record);

            var history = BenchmarkResultsWriter.ReadLast(10);
            TestContext.Out.WriteLine();
            TestContext.Out.WriteLine("  Run History (last 10 runs)");
            TestContext.Out.WriteLine("  --------------------------");
            TestContext.Out.Write(BenchmarkResultsWriter.FormatComparisonTable(history));
            // ─────────────────────────────────────────────────────────────────

            Assert.That(serialFragmentCount, Is.GreaterThan(0));
            Assert.That(serialFragmentCount, Is.EqualTo(parallelFragmentCount));
        }
    }
}