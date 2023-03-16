using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easy.Common.Extensions;
using MassSpectrometry;
using Microsoft.FSharp.Core;
using MzLibUtil;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.LayoutObjects;
using Chart = Plotly.NET.CSharp.Chart;

namespace Test.AveragingTests
{
    public class MassAccuracy
    {
        
        public MassAccuracy(string name, double[] theoreticalMzs, List<MsDataScan> scans, int decimalPlaces = 2)
        {
            Name = name;
            MostAbundantPeaks = theoreticalMzs.ToDictionary(p => p, p => new List<MzPeak>());
            var ms1Scans = scans.Where(p => p.MsnOrder == 1).ToList();
            CalculateMassDistribution(ms1Scans);
            BinPeaks(decimalPlaces);
        }

        public string Name { get; }
        public Dictionary<double, List<MzPeak>> MostAbundantPeaks { get; }
        private List<(double mz, int count)> binnedPeaks;

        private void CalculateMassDistribution(List<MsDataScan> scans)
        {
            foreach (var scan in scans)
            {
                foreach (var peakEntry in MostAbundantPeaks)
                {
                    var peaksInWindow = scan.MassSpectrum.Extract(peakEntry.Key - 5, peakEntry.Key + 5);
                    if (peaksInWindow.Any())
                        peakEntry.Value.Add(peaksInWindow.MaxBy(p => p.Intensity));
                }
            }
        }

        private void BinPeaks(int decimalPlaces = 2)
        {
            var allPeaks = MostAbundantPeaks.Values.SelectMany(p => p).ToList();
            var masses = allPeaks.Select(p => Math.Round(p.Mz, decimalPlaces))
                .Distinct().ToList();
            binnedPeaks = new List<(double mz, int count)>();
            foreach (var mz in allPeaks.Select(p => Math.Round(p.Mz, decimalPlaces))
                         .Distinct())
            {
                //binnedPeaks.Add((mz - 0.005, 0));

                var count = allPeaks.Count(p => Math.Abs(Math.Round(p.Mz, 2) - mz) < 0.0001);
                binnedPeaks.Add((mz, count));

                //binnedPeaks.Add((mz + 0.005, 0));
            }
            binnedPeaks.Add((masses.Min() - 10, 0));
            binnedPeaks.Add((masses.Max() + 10, 0));
        }

        public GenericChart.GenericChart GetDistributionLinePlot(string name = "")
        {
            var xArray = binnedPeaks.Select(p => p.mz).ToArray();
            var yArray = binnedPeaks.Select(p => p.count).ToArray();

            var wholeChart = Chart.Column<int, double, string>(yArray, xArray, Name: name)
                .WithSize(900, 600)
                .WithShapes(GetTheoreticalIntensityLines());

            return wholeChart;
        }

        public GenericChart.GenericChart GetBigPlot(int[] chargeStates,  Color color, string title = "" )
        {
            if (chargeStates.Length != MostAbundantPeaks.Count)
                throw new MzLibException("Charges and Mz Values do not match");
            if (chargeStates.Length != 9)
                throw new MzLibException("Currently hardcoded to plot 9 charge states");

            int miniGraphTolerance = 1;

            List<GenericChart.GenericChart> charts = new();
            var keys = MostAbundantPeaks.Keys.ToArray();
            for (int i = 0; i < MostAbundantPeaks.Count; i++)
            {
                string name = $"Charge State {chargeStates[i]}";
                var xArray = binnedPeaks.Where(p => p.mz >= keys[i] - miniGraphTolerance 
                                                    && p.mz <= keys[i] + miniGraphTolerance)
                    .Select(p => p.mz).ToArray();
                var yArray = binnedPeaks.Where(p => p.mz >= keys[i] - miniGraphTolerance 
                                                    && p.mz <= keys[i] + miniGraphTolerance)
                    .Select(p => p.count).ToArray();

                var gridY = i / 3;
                var gridX = Math.Abs(i % 3);


                var line = GetSingleIntensityLine(keys[i], yArray.Max() * 1.1, i + 1);


                var specificChart = Chart.Column<int, double, string>
                    (
                        yArray, xArray,
                        MarkerColor: color,
                        Name: title
                    )
                    .WithTitle(title )
                    .WithSize(300, 300)
                    //.WithShape(line)
                    .WithXAxisStyle(Title.init(name));
                charts.Add(specificChart);
            }

            var chargeStateGrid = Chart.Grid(charts, 3, 3)
                .WithSize(900, 900)
                .WithTitle(title);
            return chargeStateGrid;
        }

        public static GenericChart.GenericChart GetIsotopicEnvelopeChart(double[] predictedMzs, List<MsDataScan> scans, Color color)
        {
            double miniGraphTolerance = 0.5;
            List<GenericChart.GenericChart> charts = new();
            for (var i = 0; i < predictedMzs.Length; i++)
            {
                var mz = predictedMzs[i];
                var gridY = i / 3;
                var gridX = Math.Abs(i % 3);

                var mzMin = mz - miniGraphTolerance;
                var mzMax = mz + miniGraphTolerance;
                List<GenericChart.GenericChart> scanCharts = new();
                foreach (var scan in scans)
                {
                    var peaks = scan.MassSpectrum.Extract(mzMin, mzMax).ToList();
                    var peakCount = peaks.Count;
                    for (int j = 0; j < peakCount; j++)
                    {
                        peaks.Add(new MzPeak(peaks[j].Mz - 0.05, 0));
                        peaks.Add(new MzPeak(peaks[j].Mz + 0.05, 0));
                    }
                    var orderedPeaks = peaks.OrderBy(p => p.Mz).ToList();
                    var xArray = orderedPeaks.Select(p => p.Mz).ToList();
                    var yArray = orderedPeaks.Select(p => p.Intensity).ToList();

                    

                    var scanChart = Chart.Spline<double, double, string>
                    (
                        xArray,
                        yArray,
                        LineColor: color
                    );
                    scanCharts.Add( scanChart );
                }

                var combinedIEChart = Chart.Combine(scanCharts);
                charts.Add( combinedIEChart );
            }

            var envelopeGrid = Chart.Grid(charts, 3, 3)
                .WithSize(900, 900);

            return envelopeGrid;
        }


        public GenericChart.GenericChart GetDistributionScatterPlot( string name = "")
        {
            var allPeaks = MostAbundantPeaks.Values.SelectMany(p => p).ToList();
            var xArray = allPeaks.Select(p => p.Mz).ToArray();
            var yArray = allPeaks.Select(p => p.Intensity).ToArray();

            var chart = Chart.Scatter<double, double, string>(xArray, yArray, StyleParam.Mode.Markers)
                .WithSize(1000, 600)
                .WithShapes(GetTheoreticalIntensityLines());
            return chart;
        }

        public static List<double> RangeIncrement(double start, double end, double increment)
        {
            return Enumerable
                .Repeat(start, (int)((end - start) / increment) + 1)
                .Select((tr, ti) => tr + (increment * ti))
                .ToList();
        }

        private List<Shape> GetTheoreticalIntensityLines()
        {
            var max = binnedPeaks.IsDefault()
                ? MostAbundantPeaks.Max(p => p.Value.Max(m => m.Intensity))
                : binnedPeaks.Max(p => p.count);

            return MostAbundantPeaks.Keys.Select(theoreticalMz => GetSingleIntensityLine(theoreticalMz, max)).ToList();
        }

        private static Shape GetSingleIntensityLine(double mz, double max, int xRef = 0)
        {
            return Shape.init<double, double, int, double>
            (
                StyleParam.ShapeType.Line,
                mz,
                mz,
                0,
                max,
                Fillcolor: new FSharpOption<Color>(Color.fromKeyword(ColorKeyword.Green)),
                Opacity: 0.5, 
                Xref: new FSharpOption<string>("x"+xRef),
                Yref: new FSharpOption<string>("y"+xRef)
            );
        }

    }
}
