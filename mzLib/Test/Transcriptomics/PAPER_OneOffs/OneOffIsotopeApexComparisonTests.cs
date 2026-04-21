using MassSpectrometry;
using NUnit.Framework;
using Omics;
using Omics.Modifications;
using Plotly.NET;
using Proteomics.ProteolyticDigestion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Transcriptomics.Digestion;
using UsefulProteomicsDatabases;
using UsefulProteomicsDatabases.Transcriptomics;

namespace Test.Transcriptomics.PAPER_OneOffs
{
    using CSharpChart = Plotly.NET.CSharp.Chart;

    [TestFixture]
    public class OneOffIsotopeApexComparisonTests
    {
        private const int MaxPeptidePrecursorsToScan = 10000000;
        private const int MaxOligoPrecursorsToScan = 10000000;

        [Test]
        [Explicit("One-off report: formula vs average residue isotope apex offsets")]
        public static void OneOff_CompareFormulaVsAverageResidueApexOffsets()
        {
            var averagine = new Averagine();
            var oxyriboAveragine = new OxyriboAveragine();
            var noMods = new List<Modification>();

            string cacheRoot = Path.Combine("D:\\Projects\\SingleOligoPaperGit\\results\\averatide", "AverageResidueCache");
            var peptideCache = new AverageResidueModelCache(cacheRoot, averagine);
            var oligoCache = new AverageResidueModelCache(cacheRoot, oxyriboAveragine);

            string proteinDbPath = @"B:\Users\Nic\TopDownEntrapment\Databases\Human_AND_model_organism_9606_2025_09_22.fasta";
            var proteins = ProteinDbLoader.LoadProteinFasta(proteinDbPath, true, DecoyType.None, false, out _);
            var peptideStream = proteins
                .SelectMany(p => p.Digest(new DigestionParams(minPeptideLength: 1, maxMissedCleavages: 12), noMods, noMods))
                .Concat(proteins.SelectMany(p => p.Digest(new DigestionParams("StcE-trypsin", minPeptideLength: 1, maxMissedCleavages: 12), noMods, noMods)))
                .Cast<IBioPolymerWithSetMods>();

            string[] rnaDbPaths =
            [
                @"D:\Projects\SingleOligoPaperGit\datasets\fluc\FLuc_new.fasta",
                @"D:\Projects\SingleOligoPaperGit\datasets\malat\MALAT from plasmid.fasta",
                @"D:\Projects\SingleOligoPaperGit\datasets\pfizer\PfizerBNT-162b2.fasta"
            ];

            var rnas = rnaDbPaths.SelectMany(path => RnaDbLoader.LoadRnaFasta(path, true, DecoyType.None, false, out _)).ToList();
            var oligoStream = rnas
                .SelectMany(r => r.Digest(new RnaDigestionParams("RNase T1", 10, 1), noMods, noMods))
                .Concat(rnas.SelectMany(r => r.Digest(new RnaDigestionParams("colicin_E5", 12, 1), noMods, noMods)))
                .Concat(rnas.SelectMany(r => r.Digest(new RnaDigestionParams("top-down", 0, 1), noMods, noMods)))
                .Cast<IBioPolymerWithSetMods>();

            CacheUpdateStats peptideUpdate = peptideCache.UpdateFromBioPolymers(peptideStream, MaxPeptidePrecursorsToScan, "ProteinDigest");
            CacheUpdateStats oligoUpdate = oligoCache.UpdateFromBioPolymers(oligoStream, MaxOligoPrecursorsToScan, "RnaDigest");

            TestContext.Out.WriteLine("# One-off isotope apex comparison report");
            TestContext.Out.WriteLine($"# Cache root: {cacheRoot}");
            TestContext.Out.WriteLine($"# Protein DB: {proteinDbPath}");
            TestContext.Out.WriteLine($"# RNA DBs: {string.Join("; ", rnaDbPaths)}");
            TestContext.Out.WriteLine($"# Peptides scanned: {peptideUpdate.ScannedCount}; eligible: {peptideUpdate.EligibleCount}; updated: {peptideUpdate.UpdatedCount}; skipped-filled: {peptideUpdate.SkippedAlreadyFilledCount}; found bins: {peptideUpdate.FoundCount}; missing bins: {peptideUpdate.MissingCount}");
            TestContext.Out.WriteLine($"# Oligos scanned: {oligoUpdate.ScannedCount}; eligible: {oligoUpdate.EligibleCount}; updated: {oligoUpdate.UpdatedCount}; skipped-filled: {oligoUpdate.SkippedAlreadyFilledCount}; found bins: {oligoUpdate.FoundCount}; missing bins: {oligoUpdate.MissingCount}");
            TestContext.Out.WriteLine("Type\tSequence\tLength\tChemicalFormula\tExpMonoMass\tExpMostIntenseMass\tExpDiffToMono\tTheoModel\tModelIndex\tIndexBin\tTheoMonoMass\tTheoMostIntenseMass\tTheoDiffToMono\tDeltaDa\tDeltaPpm");

            WriteRowsFromCache(peptideCache.Records, "Peptide", peptideCache.ModelName);
            WriteRowsFromCache(oligoCache.Records, "Oligo", oligoCache.ModelName);

            string plotRoot = Path.Combine(cacheRoot, "plots");
            string peptidePlotPath = GenerateScatterPlot(peptideCache.Records, peptideCache.ModelName, "Peptide", plotRoot);
            string oligoPlotPath = GenerateScatterPlot(oligoCache.Records, oligoCache.ModelName, "Oligo", plotRoot);
            TestContext.Out.WriteLine($"# Peptide plot: {peptidePlotPath}");
            TestContext.Out.WriteLine($"# Oligo plot: {oligoPlotPath}");

            Assert.That(peptideCache.FoundCount + oligoCache.FoundCount, Is.GreaterThan(0), "No rows generated for report.");
        }

        private static string GenerateScatterPlot(
            IEnumerable<AverageResidueCacheRecord> records,
            string modelName,
            string label,
            string plotRoot)
        {
            var allRecords = records.ToList();

            var theoretical = allRecords
                .Select(r => (x: r.TheoMonoMass, y: r.TheoMostIntenseMass))
                .ToList();

            var experimental = allRecords
                .Where(r => r.HasObservation && r.ExpMonoMass.HasValue && r.ExpMostIntenseMass.HasValue)
                .Select(r => (x: r.ExpMonoMass!.Value, y: r.ExpMostIntenseMass!.Value))
                .ToList();

            var charts = new List<GenericChart>
            {
                CSharpChart.Point<double, double, string>(
                    theoretical.Select(p => p.x),
                    theoretical.Select(p => p.y),
                    Name: $"{label} Theoretical"),
                CSharpChart.Point<double, double, string>(
                    experimental.Select(p => p.x),
                    experimental.Select(p => p.y),
                    Name: $"{label} Experimental")
            };

            if (TryFitLine(theoretical, out var theoSlope, out var theoIntercept, out var theoMinX, out var theoMaxX))
            {
                charts.Add(CSharpChart.Line<double, double, string>(
                    new[] { theoMinX, theoMaxX },
                    new[] { theoSlope * theoMinX + theoIntercept, theoSlope * theoMaxX + theoIntercept },
                    Name: $"{label} Theoretical Trend"));
            }

            if (TryFitLine(experimental, out var expSlope, out var expIntercept, out var expMinX, out var expMaxX))
            {
                charts.Add(CSharpChart.Line<double, double, string>(
                    new[] { expMinX, expMaxX },
                    new[] { expSlope * expMinX + expIntercept, expSlope * expMaxX + expIntercept },
                    Name: $"{label} Experimental Trend"));
            }

            var figure = Plotly.NET.Chart.Combine(charts);
            Directory.CreateDirectory(plotRoot);
            string filePath = Path.Combine(plotRoot, $"{label}_{modelName}_MonoVsMostAbundant_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html");
            Plotly.NET.CSharp.GenericChartExtensions.SaveHtml(figure, filePath);
            return filePath;
        }

        private static bool TryFitLine(
            IReadOnlyList<(double x, double y)> points,
            out double slope,
            out double intercept,
            out double minX,
            out double maxX)
        {
            slope = 0;
            intercept = 0;
            minX = 0;
            maxX = 0;

            if (points.Count < 2)
            {
                return false;
            }

            minX = points.Min(p => p.x);
            maxX = points.Max(p => p.x);

            double meanX = points.Average(p => p.x);
            double meanY = points.Average(p => p.y);
            double numerator = 0;
            double denominator = 0;

            foreach (var point in points)
            {
                double dx = point.x - meanX;
                numerator += dx * (point.y - meanY);
                denominator += dx * dx;
            }

            if (denominator == 0)
            {
                return false;
            }

            slope = numerator / denominator;
            intercept = meanY - slope * meanX;
            return true;
        }

        private static void WriteRowsFromCache(IEnumerable<AverageResidueCacheRecord> records, string type, string modelName)
        {
            foreach (var record in records.OrderBy(p => p.ModelIndex))
            {
                if (!record.HasObservation)
                {
                    TestContext.Out.WriteLine(
                        $"{type}\tNA\tNA\tNA\tNA\tNA\tNA\t{modelName}\t{record.ModelIndex}\t{record.ModelIndex}\t{record.TheoMonoMass:F6}\t{record.TheoMostIntenseMass:F6}\t{record.TheoDiffToMono:F6}\tNA\tNA");
                    continue;
                }

                string chemicalFormula = string.IsNullOrWhiteSpace(record.ChemicalFormula) ? "NA" : record.ChemicalFormula;
                string expMonoMass = record.ExpMonoMass?.ToString("F6") ?? "NA";
                string expMostIntenseMass = record.ExpMostIntenseMass?.ToString("F6") ?? "NA";
                string expDiffToMono = record.ExpDiffToMono?.ToString("F6") ?? "NA";
                string deltaDa = record.DeltaDa?.ToString("F6") ?? "NA";
                string deltaPpm = record.DeltaPpm?.ToString("F2") ?? "NA";

                TestContext.Out.WriteLine(
                    $"{type}\t{record.Sequence}\t{record.Length}\t{chemicalFormula}\t{expMonoMass}\t{expMostIntenseMass}\t{expDiffToMono}\t{modelName}\t{record.ModelIndex}\t{record.ModelIndex}\t{record.TheoMonoMass:F6}\t{record.TheoMostIntenseMass:F6}\t{record.TheoDiffToMono:F6}\t{deltaDa}\t{deltaPpm}");
            }
        }
    }
}
