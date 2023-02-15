using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Easy.Common.Extensions;
using MassSpectrometry;
using MassSpectrometry.MzSpectra;
using MathNet.Numerics;
using MathNet.Numerics.Differentiation;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Interpolation;
using Plotly.NET;
using Plotly.NET.CSharp;
using SpectralAveraging;
using TopDownProteomics;
using Chart = Plotly.NET.CSharp.Chart;

namespace Test.AveragingTests
{
    public static class ThrashSnrCalculator
    {
        private record struct SpectrumRegion(MzPeak[] Peaks);
        private record struct IntensityCurve(double[] XValues, double[] YValues);

        public static void CalculateSnr(MzSpectrum spec)
        {
            var spectrum = new MzSpectrum(spec.XArray, spec.YArray, true);
            spectrum.NormalizeSpectrum();

            foreach (var spectrumRegion in ExtractSpectrumRegions(spectrum))
            {
                IntensityCurve initialCurve = GenerateCurveOfPeaks(spectrumRegion);
                IntensityCurve derivedCurve = DeriveCurve(initialCurve, FitType.Basic);
                if (spectrumRegion.Peaks.Any(p => p.Mz >= 752 && p.Mz <= 762))
                {
                    ShowCompositePlotWithPlotly(spectrumRegion, initialCurve, derivedCurve);
                  
                }
                    


            }
        }

        /// <summary>
        /// Get 4 mz regions of the spectrum, iterating every 2 mz
        /// </summary>
        /// <param name="spec"></param>
        /// <param name="windowSize">size of window for each region, each region will overlap by 1/2 window size</param>
        /// <returns></returns>
        private static IEnumerable<SpectrumRegion> ExtractSpectrumRegions(MzSpectrum spec, int windowSize = 12)
        {
            int start = (int)spec.XArray.First();
            int end = (int)(spec.XArray.Last() + 1);
            for (int i = start; i < end; i+=(windowSize / 2))
            {
                var startValue = spec.XArray.First(p => p >= i);
                var startIndex = spec.XArray.IndexOf(startValue);
                var endValue = spec.XArray.SubSequence(startIndex, spec.XArray.Length).FirstOrDefault(p => p >= i + windowSize);
                if (endValue.IsDefault())
                    endValue = spec.XArray.Last();
                var endIndex = spec.XArray.IndexOf(endValue) - 1;

                var xVals = spec.XArray.SubSequence(startIndex, endIndex).ToArray();
                var yVals = spec.YArray.SubSequence(startIndex, endIndex).ToArray();

                MzPeak[] peaks = xVals.Select((t, j) => new MzPeak(t, yVals[j])).ToArray();
                if (peaks.Count() > 1)
                    yield return new SpectrumRegion(peaks);
            }
        }

        /// <summary>
        /// Generates a plot of spectral intensity versus the number of data points of this intensity or less in the region
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        private static IntensityCurve GenerateCurveOfPeaks(SpectrumRegion region)
        {
            int peakCount = region.Peaks.Count();
            var xVals = new double[peakCount];
            var yVals = new double[peakCount];

            var orderedPeaks = region.Peaks.OrderBy(p => p.Intensity).ToList();
            for (int i = 0; i < peakCount; i++)
            {
                xVals[i] = orderedPeaks[i].Intensity;
                yVals[i] = orderedPeaks.Count(p => p.Intensity <= orderedPeaks[i].Intensity);
            }

            return new IntensityCurve(xVals, yVals);
        }

        private  enum FitType
        {
            Basic,
            PolyFit,
            Logistic
        }

        /// <summary>
        /// Takes the derivative of the curve and returns a new curve
        /// </summary>
        /// <param name="initialCurve"></param>
        /// <returns></returns>
        private static IntensityCurve DeriveCurve(IntensityCurve initialCurve, FitType fitType)
        {
            
            double[] diffArray = new double[initialCurve.XValues.Length];
            switch (fitType)
            {
                case FitType.Basic:
                    for (int i = 0; i < initialCurve.XValues.Length - 1; i++)
                    {
                        var dmz = initialCurve.XValues[i + 1] - initialCurve.XValues[i];
                        var dInt = initialCurve.YValues[i + 1] - initialCurve.YValues[i];
                        diffArray[i] = dmz / dInt;
                    }
                    break;

                case FitType.PolyFit:
                    var poly = Polynomial.Fit(initialCurve.XValues, initialCurve.YValues,
                        10);
                    var derivedPoly = poly.Differentiate();
                    diffArray = derivedPoly.Evaluate(initialCurve.XValues).ToArray();
                    break;

                case FitType.Logistic:
                    for (int i = 0; i < initialCurve.XValues.Length - 1; i++)
                    {
                        diffArray[i] = initialCurve.YValues[i] * (1 - initialCurve.YValues[i]);
                    }
                    break;
            }

            return initialCurve with { YValues = diffArray };
        }


        #region Plotly For Troubleshooting

        private static void ShowIntensityCurveWithPlotly(IntensityCurve curve)
        {
            Plotly.NET.CSharp.GenericChartExtensions.Show(GetIntensityCurveWithPlotly(curve));
        }

        private static GenericChart.GenericChart GetIntensityCurveWithPlotly(IntensityCurve curve)
        {
            var plot = Chart.Line<double, double, string>(curve.XValues, curve.YValues, Name: "Intensity Cumulative");
            return plot;
        }

        private static GenericChart.GenericChart GetSpectrumRegionWithPlotly(SpectrumRegion region)
        {
            var peaks = region.Peaks.ToList();

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
            return specrumChart;
        }


        private static GenericChart.GenericChart GetDifferentiatedCurveWithPlotly(IntensityCurve curve)
        {
            var plot = Chart.Line<double, double, string>(curve.XValues, curve.YValues, Name: "Derived Intensity Cumulative");
            return plot;
        }

        private static void ShowCompositePlotWithPlotly(SpectrumRegion region, IntensityCurve curve, IntensityCurve derivedCurve)
        {
            Plotly.NET.CSharp.GenericChartExtensions.Show(GetCompositePlotWithPlotly(region, curve, derivedCurve));
        }

        private static GenericChart.GenericChart GetCompositePlotWithPlotly(SpectrumRegion region, IntensityCurve curve, IntensityCurve derivedCurve)
        {
            var combined =
                Chart.Grid(
                    new List<GenericChart.GenericChart>()
                        { GetSpectrumRegionWithPlotly(region), GetIntensityCurveWithPlotly(curve), GetDifferentiatedCurveWithPlotly(derivedCurve) },
                    3, 1)
                    .WithSize(1000, 1000);
            return combined;
        }

        #endregion



    }
}
