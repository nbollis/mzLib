﻿using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using Chemistry;

namespace MassSpectrometry
{
    public class IsotopicEnvelope : IHasMass
    {
        public readonly List<(double mz, double intensity)> Peaks;
        public double MonoisotopicMass { get; private set; }

        /// <summary>
        /// Mass of most abundant observed isotopic peak, not accounting for addition or subtraction or protons due to ESI charge state induction
        /// </summary>
        internal double MostAbundantObservedIsotopicMass { get; private set; }
        public readonly int Charge;
        public readonly double TotalIntensity;

        public double Score { get; private set; }

        /// <summary>
        /// Used for an isotopic envelope that mzLib deconvoluted (e.g., from a mass spectrum)
        /// </summary>
        public IsotopicEnvelope(List<(double mz, double intensity)> bestListOfPeaks, double bestMonoisotopicMass, int bestChargeState, double bestTotalIntensity, double bestStDev)
        {
            Peaks = bestListOfPeaks;
            MonoisotopicMass = bestMonoisotopicMass;
            MostAbundantObservedIsotopicMass = bestListOfPeaks.MaxBy(p => p.intensity).mz * Math.Abs(bestChargeState);
            Charge = bestChargeState;
            TotalIntensity = bestTotalIntensity;
            Score = ScoreIsotopeEnvelope(bestStDev);
        }

        /// <summary>
        /// Used for a neutral mass read in from a deconvoluted file
        /// Assumes the mass is correct: score is max value
        /// </summary>
        public IsotopicEnvelope(double monoisotopicMass, double intensity, int charge)
        {
            MonoisotopicMass = monoisotopicMass;
            Charge = charge;
            TotalIntensity = intensity;
            Score = double.MaxValue;
            Peaks = [(monoisotopicMass.ToMz(charge), intensity)];
        }

        public override string ToString()
        {
            return Charge + "\t" + Peaks[0].mz.ToString("G8") + "\t" + Peaks.Count + "\t" + TotalIntensity;
        }

        private double ScoreIsotopeEnvelope(double stDev) //likely created by Stefan Solntsev using peptide data
        {
            return Peaks.Count >= 2 ?
                TotalIntensity / Math.Pow(stDev, 0.13) * Math.Pow(Peaks.Count, 0.4) / Math.Pow(Math.Abs(Charge), 0.06) :
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
    }
}