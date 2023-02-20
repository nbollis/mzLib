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
using Microsoft.FSharp.Core;
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
        private int binsToOutput = 150;

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

        public IntensityHistogram(MzSpectrum spectrum, int numberOfBins, int percentageOfPeaksToKeep)
        {
            percentOfPeaksToKeep = percentageOfPeaksToKeep;
            var peaks = ExtractPeaks(new List<MzSpectrum>() {spectrum}).ToList();
            Bins = BinPeaks(peaks, numberOfBins);
            CalculateSpecialBins();
        }

        public IEnumerable<MzPeak> ExtractPeaks(List<MzSpectrum> spectra)
        {
            foreach (var spectrum in spectra)
            {
                List<MzPeak> allPeaks = new List<MzPeak>();
                for (int i = 0; i < spectrum.YArray.Length; i++)
                {
                    allPeaks.Add(new MzPeak(spectrum.XArray[i], spectrum.YArray[i]));
                }

                var extractedPeaks = allPeaks.OrderBy(p => p.Intensity).ToList();
                int peaksToKeepCount = (int)(extractedPeaks.Count * (percentOfPeaksToKeep / 100.0));

                // add count for number of peaks extracted that did not make it to the histogram
                SignalIntegrated = +extractedPeaks.SubSequence(peaksToKeepCount, extractedPeaks.Count).Count();
                foreach (var mzPeak in extractedPeaks.SubSequence(0, peaksToKeepCount)) yield return mzPeak;
            }
        }

        public List<MzPeakHistogramBin> BinPeaks(List<MzPeak> peaks, int numberOfBins)
        {
            List<MzPeakHistogramBin> bins = new List<MzPeakHistogramBin>();
            double maxIntensity = peaks.Max(p => p.Intensity);
            var binWidth = maxIntensity / numberOfBins;

            bool foundFirstBin = false;
            int binIndex = 1;
            for (int i = 0; i < numberOfBins; i++)
            {
                double start = binWidth * i;
                double end = binWidth * (i + 1);
                var peaksInBin = peaks.Where(p => p.Intensity >= start && p.Intensity < end).ToList();

                if (!foundFirstBin && !peaksInBin.Any()) continue;
                bins.Add(new(binIndex, start, end, peaksInBin));
                binIndex++;
                foundFirstBin = true;
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

            // fit bins to a polynomial
            var xValues = Bins.Select(p => (double)p.BinIndex).ToArray();
            var yValues = Bins.Select(p => (double)p.PeakCount).ToArray();

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
            // first fallback value is first bin with 1% of most abundant bin
            int noiseCutoffBinIndex = Bins.FirstOrDefault(p => 
                p.BinIndex > MostAbundantBin.BinIndex 
                && p.PeakCount <= MostAbundantBin.PeakCount / 100).BinIndex;
            for (int i = MostAbundantBin.BinIndex; i < Bins.Count; i++)
            {
                if (firstDerivativeCurveValues[i] < 0 && firstDerivativeCurveValues[i + 1] > 0)
                {
                    while (Bins.First(p => p.BinIndex == i).PeakCount <= 0)
                    {
                        i++;
                    }
                    noiseCutoffBinIndex = i;
                    break;
                }
            }

            if (noiseCutoffBinIndex != -1)
                NoiseEndBin = Bins.First(p => p.BinIndex == noiseCutoffBinIndex);
            else
                NoiseEndBin = Bins[MostAbundantBin.BinIndex + NoiseStartBin.BinIndex];
            // second fallback value is bin that is the same distance from the start of the noise region to the maximum


        }

        #region Output Methods


        public void OutputWithPlotly(string title = "")
        {
            Plotly.NET.CSharp.GenericChartExtensions.Show(GetPlot(title));
        }

        public GenericChart.GenericChart GetPlot(string title = "", string yAxis = "BinValues")
        {
            switch (yAxis)
            {
                case "BinValues":
                {
                    var chartValues = Bins.Select(p => p.PeakCount).ToList();
                    var binKeys = Bins.Select(p => p.BinStringForOutput).ToArray();
                    var chartKeys = new Optional<IEnumerable<string>>(binKeys, true);


                    var noiseCutoffLine = Shape.init<string, string, int, int>(StyleParam.ShapeType.Line,
                    NoiseEndBin.BinStringForOutput,
                    NoiseEndBin.BinStringForOutput,
                    0,
                    chartValues.Max(),
                        Fillcolor: new FSharpOption<Color>(Color.fromKeyword(ColorKeyword.Purple)));

                    var fitCurve = Chart.Line<string, double, string>(
                        binKeys, polynomialCurve, 
                        Name: "Fit Curve",
                        LineColor: new Optional<Color>(Color.fromKeyword(ColorKeyword.Red), true))
                        .WithTitle("Polynomial Fit");

                    var noiseHistogram = Chart.Column<int, string, string>(chartValues, chartKeys,
                        Name: "Binned Int Values",
                        MarkerColor: new Optional<Color>(Color.fromKeyword(ColorKeyword.Blue), true))
                        .WithTitle(title)
                        .WithShapes(new List<Shape>() { noiseCutoffLine });

                    return Chart.Combine(new List<GenericChart.GenericChart>() { fitCurve, noiseHistogram })
                        .WithSize(1000, 600);
                }
                case "BinNumbers":
                {
                    var chartValues = Bins.Select(p => p.PeakCount).ToList();
                    var binKeys = Bins.Select(p => p.BinIndex).ToArray();
                    var chartKeys = new Optional<IEnumerable<int>>(binKeys, true);


                    var noiseCutoffLine = Shape.init<int, int, int, int>(StyleParam.ShapeType.Line,
                        NoiseEndBin.BinIndex,
                        NoiseEndBin.BinIndex,
                        0,
                        chartValues.Max());

                    var fitCurve = Chart.Line<int, double, string>(
                            binKeys, polynomialCurve, LineColor: new Optional<Color>(Color.fromKeyword(ColorKeyword.Red), true))
                        .WithTitle("Polynomial Fit");

                    var noiseHistogram = Chart.Column<int, int, string>(chartValues, chartKeys,
                            MarkerColor: new Optional<Color>(Color.fromKeyword(ColorKeyword.Blue), true))
                        .WithTitle(title)
                        .WithShapes(new List<Shape>() { noiseCutoffLine });

                    return Chart.Combine(new List<GenericChart.GenericChart>() { fitCurve, noiseHistogram });
                }
                default:
                    throw new MzLibException("yAxis is not implemented");
            }
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
