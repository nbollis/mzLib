using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easy.Common.Extensions;
using MassSpectrometry;
using MathNet.Numerics;
using MathNet.Numerics.LinearRegression;
using MathNet.Numerics.Statistics;
using MzLibUtil;
using NUnit.Framework.Constraints;
using TopDownProteomics;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.LayoutObjects;
using Chart = Plotly.NET.CSharp.Chart;

namespace Test.AveragingTests
{


    public record IntensityHistogram : IEnumerable<MzPeakHistogramBin>
    {
        private int percentOfPeaksToKeep;
        private double[] polynomialCurve;
        private int binsToOutput;

        public List<MzPeakHistogramBin> Bins { get; init; }
        public List<MzPeakHistogramBin> NoiseRegion { get; private set; }
        public List<MzPeakHistogramBin> SignalRegion { get; private set; }
        public MzPeakHistogramBin MostAbundantBin { get; private set; }
        public MzPeakHistogramBin NoiseStartBin { get; private set; }
        public MzPeakHistogramBin NoiseEndBin { get; private set; }
        public double NoiseIntegrated { get; private set; }
        public double SignalIntegrated { get; private set; }

        public IntensityHistogram(List<MzSpectrum> spectra, int numberOfBins, int percentageOfPeaksToKeep)
        {
            percentOfPeaksToKeep = percentageOfPeaksToKeep;
            var peaks = ExtractPeaks(spectra).ToList();
            Bins = BinPeaks(peaks, numberOfBins);
            CalculateSpecialBins();
        }

        public IEnumerable<MzPeak> ExtractPeaks(List<MzSpectrum> spectra)
        {
            foreach (var spectrum in spectra)
            {
                double maxIntensity = spectrum.YArray.Max();
                foreach (var peak in spectrum.Extract(spectrum.XArray.First(), spectrum.XArray.Last()))
                {
                    if (peak.Intensity <= maxIntensity * (percentOfPeaksToKeep / 100.0))
                        yield return peak;
                    else
                        SignalIntegrated += peak.Intensity;
                }
            }
        }

        public List<MzPeakHistogramBin> BinPeaks(List<MzPeak> peaks, int numberOfBins)
        {
            List<MzPeakHistogramBin> bins = new List<MzPeakHistogramBin>();
            double maxIntensity = peaks.Max(p => p.Intensity);
            var binWidth = maxIntensity / numberOfBins;

            for (int i = 0; i < numberOfBins; i++)
            {
                double start = binWidth * i;
                double end = binWidth * (i + 1);
                var peaksInBin = peaks.Where(p => p.Intensity >= start && p.Intensity < end).ToList();

                MzPeakHistogramBin bin = new(i, start, end, peaksInBin);
                bins.Add(bin);
            }

            return bins;
        }

        public void CalculateSpecialBins()
        {
            // find most abundant bin
            var mostAbundantBin = Bins.MaxBy(p => p.PeakCount);
            MostAbundantBin = mostAbundantBin;
            NoiseStartBin = Bins.First(p => p.PeakCount > 0);
            
            FitCurveToPolynomial();
            NoiseRegion = Bins.SubSequence(0, NoiseEndBin.BinIndex).ToList();
            SignalRegion = Bins.SubSequence(NoiseEndBin.BinIndex + 1, Bins.Count).ToList();
            NoiseIntegrated = NoiseRegion.Sum(p => p.PeakCount);
            SignalIntegrated += SignalRegion.Sum(p => p.PeakCount);
        }


        public void FitCurveToPolynomial()
        {
            // get bins from beginning until value is 1% of maximum
            var lastBinToConsider = Bins.First(p =>
                p.BinIndex > MostAbundantBin.BinIndex && p.PeakCount <= MostAbundantBin.PeakCount / 100.0);
            var trimmedBins = Bins.SubSequence(NoiseStartBin.BinIndex, lastBinToConsider.BinIndex).ToList();
            binsToOutput = trimmedBins.Count;

            // fit these bins to a polynomial
            var xValues = trimmedBins.Select(p => (double)p.BinIndex).ToArray();
            var yValues = trimmedBins.Select(p => (double)p.PeakCount).ToArray();

            int denominator = 1;
            Polynomial polynomial;
            do
            {
                denominator++;
                polynomial = Polynomial.Fit(xValues, yValues, binsToOutput / denominator, DirectRegressionMethod.QR);
            } while (polynomial.Coefficients.Any(p => double.IsNaN(p)));
            
            Polynomial firstDerivative = polynomial.Differentiate();

            // calculate values for curve and first derivative
            double[] curveValues = new double[xValues.Length];
            double[] firstDerivativeCurveValues = new double[xValues.Length];
            List<double> minima = new();
            for (int i = 0; i < xValues.Length; i++)
            {
                curveValues[i] = polynomial.Evaluate(xValues[i]);
                firstDerivativeCurveValues[i] = firstDerivative.Evaluate(xValues[i]);
            }
            polynomialCurve = curveValues;
            
            // find local minimum
            int maxIndex = MostAbundantBin.BinIndex - NoiseStartBin.BinIndex;
            // fallback value is bin that is the same distance from the start of the noise region to the maximum
            int noiseCutoffBinIndex = Bins[MostAbundantBin.BinIndex + (MostAbundantBin.BinIndex - NoiseStartBin.BinIndex)].BinIndex;
            for (int i = maxIndex; i < noiseCutoffBinIndex; i++)
            {
                if (!(firstDerivativeCurveValues[i] < 0) || !(firstDerivativeCurveValues[i + 1] > 0)) continue;
                noiseCutoffBinIndex = i + NoiseStartBin.BinIndex;
                break;

            }
            NoiseEndBin = Bins[noiseCutoffBinIndex];
        }
        
        #region Output Methods


        public void OutputWithPlotly(string title = "")
        {
            var chartValues = Bins.SubSequence(NoiseStartBin.BinIndex, binsToOutput).Select(p => p.PeakCount).ToList();
            var binKeys = Bins.SubSequence(NoiseStartBin.BinIndex, binsToOutput).Select(p => p.BinStringForOutput).ToArray();
            var chartKeys = new Optional<IEnumerable<string>>(binKeys, true);


            var noiseCutoffLine = Shape.init<string, string, int, int>(StyleParam.ShapeType.Line, 
                NoiseEndBin.BinStringForOutput,
                NoiseEndBin.BinStringForOutput, 
                0, 
                chartValues.Max());

            var fitCurve = Chart.Line<string, double, string>(
                    binKeys, polynomialCurve, LineColor: new Optional<Color>(Color.fromKeyword(ColorKeyword.LightPink), true))
                .WithTitle("Polynomial Fit");

            var noiseHistogram = Chart.Column<int, string, string>(chartValues, chartKeys,
                    MarkerColor: new Optional<Color>(Color.fromKeyword(ColorKeyword.LightAkyBlue), true))
                    .WithTitle(title)
                    .WithShapes(new List<Shape>() { noiseCutoffLine });

            var finalChart = Chart.Combine(new List<GenericChart.GenericChart>() { fitCurve, noiseHistogram });

            Plotly.NET.CSharp.GenericChartExtensions.Show(finalChart);
        }

        public void OutputToCsv(string filepath)
        {
            if (!filepath.EndsWith(".csv"))
            {
                filepath = filepath.Concat(".csv").ToString();
            }

            using (var sw = new StreamWriter(filepath))
            {
                sw.WriteLine("BinRange,Frequency");
                Bins.ForEach(p => sw.WriteLine($"{p.BinStringForOutput},{p.PeakCount}"));
            }
        }

        #endregion

        #region IEnummerable Implementation

        public IEnumerator<MzPeakHistogramBin> GetEnumerator()
        {
            return Bins.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
