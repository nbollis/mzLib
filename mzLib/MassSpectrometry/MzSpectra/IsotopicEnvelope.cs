using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.Providers.LinearAlgebra;

namespace MassSpectrometry
{
    public class IsotopicEnvelope
    {
        public DeconvolutionScore DeconScore { get; set; }
        public readonly List<(double mz, double intensity)> Peaks;
        public double MonoisotopicMass { get; private set; }
        public double MostAbundantObservedIsotopicMass { get; private set; }
        public readonly int Charge;
        public readonly double TotalIntensity;
        public readonly double StDev;
        public readonly int MassIndex;

        public double Score { get; private set; }

        public IsotopicEnvelope(List<(double mz, double intensity)> bestListOfPeaks, double bestMonoisotopicMass, int bestChargeState, double bestTotalIntensity, double bestStDev, int bestMassIndex)
        {
            Peaks = bestListOfPeaks.OrderBy(p => p.mz).ToList();
            MonoisotopicMass = bestMonoisotopicMass;
            MostAbundantObservedIsotopicMass = GetMostAbundantObservedIsotopicMass(bestListOfPeaks, bestChargeState);
            Charge = bestChargeState;
            TotalIntensity = bestTotalIntensity;
            StDev = bestStDev;
            MassIndex = bestMassIndex;
            Score = ScoreIsotopeEnvelope();
        }

        public double GetMostAbundantObservedIsotopicMass(List<(double mz, double intensity)> peaks, int charge)
        {
            return (peaks.OrderByDescending(p => p.intensity).ToList()[0].Item1)* charge;
        }

        public override string ToString()
        {
            return MonoisotopicMass + ":" + Charge + ":" + Peaks[0].mz.ToString("G8") + ":" + Peaks.Count;
        }

        private double ScoreIsotopeEnvelope() //likely created by Stefan Solntsev using peptide data
        {
            return Peaks.Count >= 2 ?
                TotalIntensity / Math.Pow(StDev, 0.13) * Math.Pow(Peaks.Count, 0.4) / Math.Pow(Charge, 0.06) :
                0;
        }

        public void AggregateChargeStateScore(IsotopicEnvelope chargeStateEnvelope)
        {
            Score += chargeStateEnvelope.Score;
        }

        public void SetMedianMonoisotopicMass(List<double> monoisotopicMassPredictions)
        {
            MonoisotopicMass = monoisotopicMassPredictions.Median();
        }

        public double GetFullWidthHalfMax()
        {
            double fwhm = 0;
            var maxPeak = Peaks.MaxBy(p => p.intensity);
            var maxIndex = Peaks.IndexOf(maxPeak);
            var halfIntensity = Peaks[maxIndex].intensity / 2;
            var intensities = Peaks.Select(p => p.intensity).ToArray();

            // if first peak
            if (maxIndex == 0)
            {
                int firstSmallerIndex = 1;
                for (; firstSmallerIndex < Peaks.Count; firstSmallerIndex++)
                {
                    if (Peaks[firstSmallerIndex].intensity > halfIntensity) continue;
                    else break;
                }

                var xAtHalfIntensity = LinearInterpolate(halfIntensity, Peaks[firstSmallerIndex - 1], Peaks[firstSmallerIndex]);
                fwhm = Math.Abs(maxPeak.mz - xAtHalfIntensity) * 2;
            }
            // if last peak
            else if (maxIndex == Peaks.Count - 1)
            {
                int firstSmallerIndex = maxIndex;
                for (; firstSmallerIndex > 0; firstSmallerIndex--)
                {
                    if (Peaks[firstSmallerIndex].intensity > halfIntensity) continue;
                    else break;
                }

                var xAtHalfIntensity = LinearInterpolate(halfIntensity, Peaks[firstSmallerIndex + 1], Peaks[firstSmallerIndex]);
                fwhm = Math.Abs(maxPeak.mz - xAtHalfIntensity) * 2;
            }
            // if any peak in the center
            else
            {

            }



            return fwhm;
        }

        private double LinearInterpolate(double halfIntensity, (double mz, double intensity) peak1,
            (double mz, double intensity) peak2)
        {
            return (halfIntensity - peak1.intensity)*(peak2.mz - peak1.mz) / (peak2.intensity - peak1.intensity) + peak1.mz;
        }

    }
}