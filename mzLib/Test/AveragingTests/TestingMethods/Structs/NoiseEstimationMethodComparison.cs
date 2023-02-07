using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Easy.Common.Extensions;
using MassSpectrometry;
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
        public MzSpectrum Spectrum { get; private set; }
        public IntensityHistogram IntensityHistogram { get; private set; }
        public double MrsNoiseEstimation { get; private set; }
        public double AverageOfMostAbundantHistogramBin { get; private set; }
        public double AverageOfLastHistogramNoiseBin { get; private set; }
        public double MaxSignalOverMrsNoise { get; private set; }
        public double MaxSignalOverMaxHistogramNoise { get; private set; }
        public double HistogramNoiseOverSignalIntegration { get; private set; }
        public double AverageOverStDevOfPeaks { get; private set; }


        public NoiseEstimationMethodComparison(List<MzSpectrum> spectra, int histogramPercentageOfPeaksToKeep)
        {
            Spectrum = null;
            IntensityHistogram = new IntensityHistogram(spectra, 10000, histogramPercentageOfPeaksToKeep);
            AverageOverStDevOfPeaks = spectra.SelectMany(p => p.YArray).Average() /
                                      spectra.SelectMany(p => p.YArray).StandardDeviation();

            AverageOfMostAbundantHistogramBin = IntensityHistogram.MostAbundantBin.AverageBinValue;
            AverageOfLastHistogramNoiseBin = IntensityHistogram.NoiseEndBin.AverageBinValue;
            HistogramNoiseOverSignalIntegration = IntensityHistogram.NoiseIntegrated / IntensityHistogram.SignalIntegrated;

            double[] mrsNoise = new double[spectra.Count];
            for (int i = 0; i < spectra.Count; i++)
            {
                MRSNoiseEstimator.MRSNoiseEstimation(spectra[i].YArray, 0.01, out double noise);
                mrsNoise[i] = noise;
            }
            MrsNoiseEstimation = mrsNoise.Average();

            MaxSignalOverMrsNoise = Spectrum.YArray.Max() / MrsNoiseEstimation;
            MaxSignalOverMaxHistogramNoise =
                Spectrum.YArray.Max() / IntensityHistogram.NoiseEndBin.AverageBinValue;
        }


        //public void OutputToPlotly()
        //{
        //    if (Spectrum == null)
        //        throw new MzLibException("Cannot output for multi-spectra noise estimations");

        //    var specrumChart = Chart.Line<double, double, string>(Spectrum.XArray, Spectrum.YArray, Name: "Spectrum",
        //        LineColor: new Optional<Color>(Color.fromKeyword(ColorKeyword.Blue), true),
        //        LineWidth: new Optional<double>(0.5, true));

        //    var mrsNoiseChart = Chart.Line<double, double, string>(Spectrum.XArray,
        //        Enumerable.Repeat(MrsNoiseEstimation, Spectrum.XArray.Length),
        //        Name: "Mrs Noise Estimation",
        //        LineColor: new Optional<Color>(Color.fromKeyword(ColorKeyword.Red), true));

        //    var lastHistogramBinChart = Chart.Line<double, double, string>(Spectrum.XArray,
        //        Enumerable.Repeat(AverageOfLastHistogramNoiseBin, Spectrum.XArray.Length),
        //        Name: "Histogram Bin",
        //        LineColor: new Optional<Color>(Color.fromKeyword(ColorKeyword.Green), true));

        //    var finalChart = Chart.Combine(new List<GenericChart.GenericChart>() { specrumChart, mrsNoiseChart, lastHistogramBinChart });

        //    Plotly.NET.CSharp.GenericChartExtensions.Show(finalChart);
        //}

        public string TabSeparatedHeader
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("Mrs Noise\t");
                sb.Append("Most Abundant Hist\t");
                sb.Append("Last Noise Hist\t");
                sb.Append("Max Signal / Mrs\t");
                sb.Append("Max Signal / Last Hist\t");
                sb.Append("Hist Noise / Signal Integration\t");
                sb.Append("Avg / Stdev\t");

                var tsvString = sb.ToString().TrimEnd('\t');
                return tsvString;
            }
        }


        public string ToTsvString()
        {
            var sb = new StringBuilder();
            sb.Append($"{MrsNoiseEstimation}\t");
            sb.Append($"{AverageOfMostAbundantHistogramBin}\t");
            sb.Append($"{AverageOfLastHistogramNoiseBin}\t");
            sb.Append($"{MaxSignalOverMrsNoise}\t");
            sb.Append($"{MaxSignalOverMaxHistogramNoise}\t");
            sb.Append($"{HistogramNoiseOverSignalIntegration}\t");
            sb.Append($"{AverageOverStDevOfPeaks}\t");

            var tsvString = sb.ToString().TrimEnd('\t');
            return tsvString;
        }
    }
}
