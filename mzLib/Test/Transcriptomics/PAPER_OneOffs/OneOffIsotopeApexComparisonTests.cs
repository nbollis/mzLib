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
using Transcriptomics;
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

        private sealed class ModelPlotInput
        {
            public required IEnumerable<AverageResidueCacheRecord> Records { get; init; }

            public required string ModelName { get; init; }

            public required string Label { get; init; }
        }

        [Test]
        [Explicit("One-off report: formula vs average residue isotope apex offsets")]
        public static void OneOff_CompareFormulaVsAverageResidueApexOffsets()
        {
            var averagine = new Averagine();
            var oxyriboAveragine = new OxyriboAveragine();
            var noMods = new List<Modification>();

            string cacheRoot = Path.Combine("D:\\Projects\\SingleOligoPaperGit\\results\\averatide", "AverageResidueCache2");
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
                .Cast<IBioPolymerWithSetMods>()
                .ToList();



            CacheUpdateStats peptideUpdate = peptideCache.UpdateFromBioPolymers(peptideStream, MaxPeptidePrecursorsToScan, "ProteinDigest");
            CacheUpdateStats oligoUpdate = oligoCache.UpdateFromBioPolymers(oligoStream, MaxOligoPrecursorsToScan, "RnaDigest");


            var emp = new EmpiricalAverageResidue(oligoStream);
            var empCache = new AverageResidueModelCache(cacheRoot, emp);
            CacheUpdateStats empUpdates = empCache.UpdateFromBioPolymers(oligoStream, MaxOligoPrecursorsToScan, "RnaDigest");

            string plotRoot = Path.Combine(cacheRoot, "plots");
            _ = GenerateCombinedScatterFigure(peptideCache.Records, peptideCache.ModelName, "Peptide", plotRoot);
            _ = GenerateCombinedScatterFigure(oligoCache.Records, oligoCache.ModelName, "Oligo", plotRoot);
            _ = GenerateCombinedScatterFigure(empCache.Records, empCache.ModelName, "Empirical Oligo", plotRoot);
            _ = GenerateStackedModelsFigure(
                [
                    new ModelPlotInput
                    {
                        Records = peptideCache.Records,
                        ModelName = peptideCache.ModelName,
                        Label = "Peptide"
                    },
                    new ModelPlotInput
                    {
                        Records = oligoCache.Records,
                        ModelName = oligoCache.ModelName,
                        Label = "Oligo"
                    },
                    new ModelPlotInput
                    {
                        Records = empCache.Records,
                        ModelName = empCache.ModelName,
                        Label = "Empirical Oligo"
                    }
                ],
                plotRoot);

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

        private static string GenerateStackedModelsFigure(IReadOnlyList<ModelPlotInput> modelInputs, string plotRoot)
        {
            if (modelInputs.Count == 0)
            {
                throw new ArgumentException("At least one model is required for stacked plotting.", nameof(modelInputs));
            }

            var allPanels = new List<GenericChart>(modelInputs.Count * 3);
            var subplotTitles = new List<string>(modelInputs.Count * 3);

            foreach (ModelPlotInput modelInput in modelInputs)
            {
                var plot = CreateScatterPanel(
                    modelInput.Records,
                    modelInput.Label,
                    r => r.TheoMonoMass,
                    r => r.TheoDiffToMono,
                    r => r.ExpMonoMass,
                    r => r.ExpDiffToMono,
                    "Mono Mass (Da)",
                    "Diff to Mono (Da)");

                allPanels.Add(plot);
                subplotTitles.Add($"{modelInput.Label} ({modelInput.ModelName})");
            }

            var grid = CSharpChart.Grid(
                allPanels,
                1,
                modelInputs.Count,
                SubPlotTitles: subplotTitles);

            var figure = Plotly.NET.CSharp.GenericChartExtensions.WithSize(grid, Width: 1800, Height: 600);
            Directory.CreateDirectory(plotRoot);
            string filePath = Path.Combine(plotRoot, $"AllModels_{modelInputs.Count}Models_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html");
            Plotly.NET.CSharp.GenericChartExtensions.SaveHtml(figure, filePath);
            return filePath;
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

            var trendEquations = new List<string>(2);
            double? theoreticalSlope = null;
            double? experimentalSlope = null;

            if (TryFitLine(theoretical, out var theoSlope, out var theoIntercept, out var theoMinX, out var theoMaxX))
            {
                charts.Add(CSharpChart.Line<double, double, string>(
                    new[] { theoMinX, theoMaxX },
                    new[] { theoSlope * theoMinX + theoIntercept, theoSlope * theoMaxX + theoIntercept },
                    Name: $"{label} Theoretical Trend",
                    LineColor: TheoreticalColor));
                theoreticalSlope = theoSlope;
                trendEquations.Add($"      Theo: y = {theoSlope:F6}x + {theoIntercept:F3}");
            }

            if (TryFitLine(experimental, out var expSlope, out var expIntercept, out var expMinX, out var expMaxX))
            {
                charts.Add(CSharpChart.Line<double, double, string>(
                    new[] { expMinX, expMaxX },
                    new[] { expSlope * expMinX + expIntercept, expSlope * expMaxX + expIntercept },
                    Name: $"{label} Experimental Trend",
                    LineColor: ExperimentalColor));
                experimentalSlope = expSlope;
                trendEquations.Add($"      Exp: y = {expSlope:F6}x + {expIntercept:F3}");
            }

            if (theoreticalSlope.HasValue && experimentalSlope.HasValue)
            {
                double tanTheta = Math.Abs((experimentalSlope.Value - theoreticalSlope.Value)
                    / (1 + (theoreticalSlope.Value * experimentalSlope.Value)));
                double angleDegrees = Math.Atan(tanTheta) * (180.0 / Math.PI);
                trendEquations.Add($"      Angle: {angleDegrees:F5} deg");
            }

            if (trendEquations.Count > 0)
            {
                var allX = theoretical.Select(p => p.x).Concat(experimental.Select(p => p.x)).ToList();
                var allY = theoretical.Select(p => p.y).Concat(experimental.Select(p => p.y)).ToList();

                if (allX.Count > 0 && allY.Count > 0)
                {
                    double minX = allX.Min();
                    double maxX = allX.Max();
                    double minY = allY.Min();
                    double maxY = allY.Max();
                    double xPadding = Math.Max((maxX - minX) * 0.02, 1e-6);
                    double yPadding = Math.Max((maxY - minY) * 0.02, 1e-6);

                    charts.Add(CSharpChart.Scatter<double, double, string>(
                        new[] { minX + (xPadding * 28.0) },
                        new[] { maxY - (yPadding * 4.0) },
                        StyleParam.Mode.Text,
                        ShowLegend: false,
                        MultiText: new[] { string.Join("<br>", trendEquations) },
                        TextPosition: StyleParam.TextPosition.MiddleLeft));
                }
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
    }
}
