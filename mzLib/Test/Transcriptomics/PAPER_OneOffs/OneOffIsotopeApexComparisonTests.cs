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
        private static readonly Color TheoreticalColor = Color.fromHex("#1f77b4");
        private static readonly Color ExperimentalColor = Color.fromHex("#d62728");
        private const double ScatterOpacity = 0.5;

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
            string peptideCombinedPlot = GenerateCombinedScatterFigure(peptideCache.Records, peptideCache.ModelName, "Peptide", plotRoot);
            string oligoCombinedPlot = GenerateCombinedScatterFigure(oligoCache.Records, oligoCache.ModelName, "Oligo", plotRoot);
            string stackedModelsPlot = GenerateStackedModelsFigure(
                peptideCache.Records,
                peptideCache.ModelName,
                "Peptide",
                oligoCache.Records,
                oligoCache.ModelName,
                "Oligo",
                plotRoot);
            TestContext.Out.WriteLine($"# Peptide combined plot (3 panels): {peptideCombinedPlot}");
            TestContext.Out.WriteLine($"# Oligo combined plot (3 panels): {oligoCombinedPlot}");
            TestContext.Out.WriteLine($"# Stacked model plot (2x3 panels): {stackedModelsPlot}");

            Assert.That(peptideCache.FoundCount + oligoCache.FoundCount, Is.GreaterThan(0), "No rows generated for report.");
        }

        private static string GenerateCombinedScatterFigure(
            IEnumerable<AverageResidueCacheRecord> records,
            string modelName,
            string label,
            string plotRoot)
        {
            GenericChart monoVsMostAbundant = CreateScatterPanel(
                records,
                label,
                r => r.TheoMonoMass,
                r => r.TheoMostIntenseMass,
                r => r.ExpMonoMass,
                r => r.ExpMostIntenseMass,
                "Mono Mass (Da)",
                "Most Abundant Mass (Da)");

            GenericChart monoVsDiff = CreateScatterPanel(
                records,
                label,
                r => r.TheoMonoMass,
                r => r.TheoDiffToMono,
                r => r.ExpMonoMass,
                r => r.ExpDiffToMono,
                "Mono Mass (Da)",
                "Diff to Mono (Da)");

            GenericChart mostAbundantVsDiff = CreateScatterPanel(
                records,
                label,
                r => r.TheoMostIntenseMass,
                r => r.TheoDiffToMono,
                r => r.ExpMostIntenseMass,
                r => r.ExpDiffToMono,
                "Most Abundant Mass (Da)",
                "Diff to Mono (Da)");

            var grid = CSharpChart.Grid(
                new[] { monoVsMostAbundant, monoVsDiff, mostAbundantVsDiff },
                1,
                3,
                SubPlotTitles: new[]
                {
                    "Mono vs Most Abundant",
                    "Mono vs Diff to Mono",
                    "Most Abundant vs Diff to Mono"
                });

            var figure = Plotly.NET.CSharp.GenericChartExtensions.WithSize(grid, Width: 1800, Height: 600);
            Directory.CreateDirectory(plotRoot);
            string filePath = Path.Combine(plotRoot, $"{label}_{modelName}_ThreePanelScatter_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html");
            Plotly.NET.CSharp.GenericChartExtensions.SaveHtml(figure, filePath);
            return filePath;
        }

        private static string GenerateStackedModelsFigure(
            IEnumerable<AverageResidueCacheRecord> recordsA,
            string modelNameA,
            string labelA,
            IEnumerable<AverageResidueCacheRecord> recordsB,
            string modelNameB,
            string labelB,
            string plotRoot)
        {
            GenericChart[] topRow = BuildThreePanels(recordsA, labelA);
            GenericChart[] bottomRow = BuildThreePanels(recordsB, labelB);

            var grid = CSharpChart.Grid(
                new[]
                {
                    topRow[0], topRow[1], topRow[2],
                    bottomRow[0], bottomRow[1], bottomRow[2]
                },
                2,
                3,
                SubPlotTitles: new[]
                {
                    $"{labelA} ({modelNameA}) Mono vs Most Abundant",
                    $"{labelA} ({modelNameA}) Mono vs Diff to Mono",
                    $"{labelA} ({modelNameA}) Most Abundant vs Diff to Mono",
                    $"{labelB} ({modelNameB}) Mono vs Most Abundant",
                    $"{labelB} ({modelNameB}) Mono vs Diff to Mono",
                    $"{labelB} ({modelNameB}) Most Abundant vs Diff to Mono"
                });

            var figure = Plotly.NET.CSharp.GenericChartExtensions.WithSize(grid, Width: 1800, Height: 1200);
            Directory.CreateDirectory(plotRoot);
            string filePath = Path.Combine(plotRoot, $"StackedModels_{modelNameA}_and_{modelNameB}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html");
            Plotly.NET.CSharp.GenericChartExtensions.SaveHtml(figure, filePath);
            return filePath;
        }

        private static GenericChart[] BuildThreePanels(IEnumerable<AverageResidueCacheRecord> records, string label)
        {
            return
            [
                CreateScatterPanel(
                    records,
                    label,
                    r => r.TheoMonoMass,
                    r => r.TheoMostIntenseMass,
                    r => r.ExpMonoMass,
                    r => r.ExpMostIntenseMass,
                    "Mono Mass (Da)",
                    "Most Abundant Mass (Da)"),
                CreateScatterPanel(
                    records,
                    label,
                    r => r.TheoMonoMass,
                    r => r.TheoDiffToMono,
                    r => r.ExpMonoMass,
                    r => r.ExpDiffToMono,
                    "Mono Mass (Da)",
                    "Diff to Mono (Da)"),
                CreateScatterPanel(
                    records,
                    label,
                    r => r.TheoMostIntenseMass,
                    r => r.TheoDiffToMono,
                    r => r.ExpMostIntenseMass,
                    r => r.ExpDiffToMono,
                    "Most Abundant Mass (Da)",
                    "Diff to Mono (Da)")
            ];
        }

        private static GenericChart CreateScatterPanel(
            IEnumerable<AverageResidueCacheRecord> records,
            string label,
            Func<AverageResidueCacheRecord, double> theoX,
            Func<AverageResidueCacheRecord, double> theoY,
            Func<AverageResidueCacheRecord, double?> expX,
            Func<AverageResidueCacheRecord, double?> expY,
            string xAxisLabel,
            string yAxisLabel)
        {
            var allRecords = records.ToList();

            var theoretical = allRecords
                .Select(r => (x: theoX(r), y: theoY(r)))
                .ToList();

            var experimental = allRecords
                .Where(r => r.HasObservation && expX(r).HasValue && expY(r).HasValue)
                .Select(r => (x: expX(r)!.Value, y: expY(r)!.Value))
                .ToList();

            var charts = new List<GenericChart>
            {
                CSharpChart.Point<double, double, string>(
                    theoretical.Select(p => p.x),
                    theoretical.Select(p => p.y),
                    Name: $"{label} Theoretical",
                    Opacity: ScatterOpacity,
                    MarkerColor: TheoreticalColor),
                CSharpChart.Point<double, double, string>(
                    experimental.Select(p => p.x),
                    experimental.Select(p => p.y),
                    Name: $"{label} Experimental",
                    Opacity: ScatterOpacity,
                    MarkerColor: ExperimentalColor)
            };

            if (TryFitLine(theoretical, out var theoSlope, out var theoIntercept, out var theoMinX, out var theoMaxX))
            {
                charts.Add(CSharpChart.Line<double, double, string>(
                    new[] { theoMinX, theoMaxX },
                    new[] { theoSlope * theoMinX + theoIntercept, theoSlope * theoMaxX + theoIntercept },
                    Name: $"{label} Theoretical Trend",
                    LineColor: TheoreticalColor));
            }

            if (TryFitLine(experimental, out var expSlope, out var expIntercept, out var expMinX, out var expMaxX))
            {
                charts.Add(CSharpChart.Line<double, double, string>(
                    new[] { expMinX, expMaxX },
                    new[] { expSlope * expMinX + expIntercept, expSlope * expMaxX + expIntercept },
                    Name: $"{label} Experimental Trend",
                    LineColor: ExperimentalColor));
            }

            var panel = Plotly.NET.Chart.Combine(charts);
            panel = Plotly.NET.CSharp.GenericChartExtensions.WithXAxisStyle<double, double, string>(
                panel,
                TitleText: xAxisLabel);
            panel = Plotly.NET.CSharp.GenericChartExtensions.WithYAxisStyle<double, double, string>(
                panel,
                TitleText: yAxisLabel);
            return panel;
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
