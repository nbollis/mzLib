using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Easy.Common.Extensions;
using MassSpectrometry;
using MathNet.Numerics.RootFinding;
using MathNet.Numerics.Statistics;
using Microsoft.FSharp.Core;
using MzLibUtil;
using MzLibUtil.MrsNoiseEstimation;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.LayoutObjects;
using Chart = Plotly.NET.CSharp.Chart;

namespace Test.AveragingTests
{
    public record struct NoiseEstimationMethodComparison : ITsv
    {
        public MzSpectrum Spectrum { get; init; }
        public IntensityHistogram IntensityHistogram { get; init; }
        public double ScanNumber { get; init; }
        public double Tic { get; init; }
        public double MrsNoiseEstimation { get; init; }
        public double AverageOfMostAbundantHistogramBin { get; init; }
        public double AverageOfLastHistogramNoiseBin { get; init; }
        public double MaxSignalOverMrsNoise { get; init; }
        public double MaxSignalOverMaxHistogramNoise { get; init; }
        public double HistSignalOverNoiseByHist { get; init; }
        public double HistSignalOverNoiseByMrs { get; init; }
        public double AverageOverStDevOfPeaks { get; init; }
        public double MedianOfAllPeaks { get; init; }


        public NoiseEstimationMethodComparison(MsDataScan scan, int numberOfBins, int histogramPercentageOfPeaksToKeep)
        {
            Spectrum = scan.MassSpectrum;
            ScanNumber = scan.OneBasedScanNumber;
            Tic = Spectrum.YArray.Sum();
            IntensityHistogram = new IntensityHistogram(Spectrum, numberOfBins, histogramPercentageOfPeaksToKeep);
            AverageOverStDevOfPeaks = Spectrum.YArray.Average() /
                                      Spectrum.YArray.StandardDeviation();
            MedianOfAllPeaks = Spectrum.YArray.Median();

            AverageOfMostAbundantHistogramBin = IntensityHistogram.MostAbundantBin.AverageBinValue;
            HistSignalOverNoiseByHist = IntensityHistogram.SignalIntegrated / IntensityHistogram.NoiseIntegrated;
            AverageOfLastHistogramNoiseBin = IntensityHistogram.NoiseEndBin.AverageBinValue;
         

            MRSNoiseEstimator.MRSNoiseEstimation(Spectrum.YArray, 0.01, out double noise);
            MrsNoiseEstimation = noise;
            MaxSignalOverMrsNoise = Spectrum.YArray.Max() / MrsNoiseEstimation;
            MaxSignalOverMaxHistogramNoise =
                Spectrum.YArray.Max() / IntensityHistogram.NoiseEndBin.AverageBinValue;
            HistSignalOverNoiseByMrs = IntensityHistogram.Bins.Count(p => p.End >= noise) /
                                       (double)IntensityHistogram.Bins.Count(p => p.End < noise);
        }

        public double CalculateSnr(double[] array, double referenceMedian)
        {
            double median = array.Median();
            double scaling = referenceMedian / median;
            var stddev = array.Where(i => i < median)
                .Select(i => i * scaling)
                .StandardDeviation();
            var mean = array.Select(i => i * scaling).Where(i => i > 3 * stddev)
                .Mean();
            return mean / stddev;
        }

        #region Output Methods

        public void ShowSpectrumPlot(string title = "")
        {
            Plotly.NET.CSharp.GenericChartExtensions.Show(GetSpectraPlot(title));
        }

        public GenericChart.GenericChart GetSpectraPlot(string title = "")
        {
            if (Spectrum == null)
                throw new MzLibException("Cannot output for multi-spectra noise estimations");

            // zero fill around each peak in spectra +- 0.005 
            List<MzPeak> peaks = Spectrum.Extract(Spectrum.XArray.First(), Spectrum.XArray.Last()).ToList();
            List<MzPeak> peaksToAdd = new();
            foreach (var peak in peaks)
            {
                peaksToAdd.Add(new MzPeak(peak.Mz - 0.005, 0));
                peaksToAdd.Add(new MzPeak(peak.Mz + 0.005, 0));
            }
            peaks.AddRange(peaksToAdd);
            var orderedPeaks = peaks.OrderBy(p => p.Mz).ToArray();
            var xArray = orderedPeaks.Select(p => p.Mz).ToArray();
            var yArray = orderedPeaks.Select(p => p.Intensity).ToArray();


            var specrumChart = Chart.Line<double, double, string>(
                xArray,
                yArray,
                Name: "Spectrum",
                LineColor: new Optional<Color>(Color.fromKeyword(ColorKeyword.Blue), true),
                LineWidth: new Optional<double>(0.5, true));

            var mrsNoiseChart = Chart.Line<double, double, string>(xArray,
                Enumerable.Repeat(MrsNoiseEstimation, xArray.Length),
                Name: "Mrs Noise Estimation",
                LineColor: new Optional<Color>(Color.fromKeyword(ColorKeyword.Green), true));

            var lastHistogramBinChart = Chart.Line<double, double, string>(xArray,
                Enumerable.Repeat(AverageOfLastHistogramNoiseBin, xArray.Length),
                Name: "Histogram Bin",
                LineColor: new Optional<Color>(Color.fromKeyword(ColorKeyword.Red), true));

            return Chart.Combine(new List<GenericChart.GenericChart>() { specrumChart, mrsNoiseChart, lastHistogramBinChart })
                .WithTitle(title)
                .WithSize(1000, 600);
        }

        public void ShowCompositePlot(string title = "")
        {
            Plotly.NET.CSharp.GenericChartExtensions.Show(GetCompositePlot(title));
        }

        public GenericChart.GenericChart GetCompositePlot(string title = "")
        {
            var noiseCutoffLine = Shape.init<string, string, int, int>(StyleParam.ShapeType.Line,
                IntensityHistogram.NoiseEndBin.BinStringForOutput,
                IntensityHistogram.NoiseEndBin.BinStringForOutput,
                0,
                IntensityHistogram.Bins.Max(p => p.PeakCount),
                Fillcolor: new FSharpOption<Color>(Color.fromKeyword(ColorKeyword.Purple)));

            var histPlot = IntensityHistogram.GetPlot().WithShape(noiseCutoffLine);
            var specPlot = GetSpectraPlot();

            var compositePlot = Chart.Grid(new List<GenericChart.GenericChart>() { specPlot, histPlot }, 2, 1)
                .WithTitle(title)
                .WithSize(1000, 1200);
            return compositePlot;
        }

        public string TabSeparatedHeader
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("Scan Number\t");
                sb.Append("Tic\t");
                sb.Append("Mrs Noise\t");
                sb.Append("Most Abundant Hist\t");
                sb.Append("Last Noise Hist\t");
                sb.Append("Max Signal / Mrs\t");
                sb.Append("Max Signal / Last Hist\t");
                sb.Append("Hist Signal / Noise Integration By Last Hist\t");
                sb.Append("Hist Signal / Noise Integration By Mrs\t");
                sb.Append("Avg / Stdev\t");

                var tsvString = sb.ToString().TrimEnd('\t');
                return tsvString;
            }
        }

        public string ToTsvString()
        {
            var sb = new StringBuilder();
            sb.Append($"{ScanNumber}\t");
            sb.Append($"{Tic}\t");
            sb.Append($"{MrsNoiseEstimation}\t");
            sb.Append($"{AverageOfMostAbundantHistogramBin}\t");
            sb.Append($"{AverageOfLastHistogramNoiseBin}\t");
            sb.Append($"{MaxSignalOverMrsNoise}\t");
            sb.Append($"{MaxSignalOverMaxHistogramNoise}\t");
            sb.Append($"{HistSignalOverNoiseByHist}\t");
            sb.Append($"{HistSignalOverNoiseByMrs}\t");
            sb.Append($"{AverageOverStDevOfPeaks}\t");

            var tsvString = sb.ToString().TrimEnd('\t');
            return tsvString;
        }

        #endregion


    }
}
