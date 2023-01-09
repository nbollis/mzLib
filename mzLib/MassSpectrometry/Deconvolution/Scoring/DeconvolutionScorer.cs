using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;

namespace MassSpectrometry
{
    public class DeconvolutionScorer
    {
        private static List<IsotopicEnvelope> isotopicEnvelopes;
        private static List<ChargeStateEnvelope> chargeStateEnvelopes;
        private static MzSpectrum deconSpectrum;

        public static DeconvolutionScore Score { get; set; }
        public static void ScoreDeconvolution(IEnumerable<IsotopicEnvelope> envelopes, MzSpectrum spec)
        {
            isotopicEnvelopes = envelopes.ToList();
            chargeStateEnvelopes = envelopes.ToChargeStateEnvelopes().ToList();
            deconSpectrum = isotopicEnvelopes.ToDeconvolutedSpectrum();

            foreach (var envelope in isotopicEnvelopes)
            {
                Score = new();
                FindMScore(envelope, spec);
                FindUScore();
                FindCSScore();
                FindFScore();
                Score.SetDScore();
            }


        
        }



        private static void FindMScore(IsotopicEnvelope envelope, MzSpectrum spectrum)
        {
            double fwhm = envelope.GetFullWidthHalfMax();

        }

        private static void FindUScore()
        {

        }

        private static void FindCSScore()
        {

        }

        private static void FindFScore()
        {

        }

    }




    public class DeconvolutionScore
    {
        public double DScore { get; private set; }

        /// <summary>
        /// Reports on the relative area of the signal that is unique captured by the deconvolution for a particular mass in teh m/z data
        /// Overlapping charge states, harmonics, noise, and baseline will decrease the UScore Values
        /// </summary>
        public double UScore { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double MScore { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double CSScore { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double FScore { get; set; }

        public DeconvolutionScore()
        {

        }

        public void SetDScore()
        {
            var scores = new double[] { UScore, MScore, CSScore, FScore };
            var nonZero = scores.Where(p => p != 0);
            var dScore = nonZero.Aggregate<double, double>(1, (current, score) => current * score);
            DScore = dScore;
        }
    }

    public class ToleranceEqualityComparer : IEqualityComparer<double>
    {
        public double Tolerance { get; set; }

        public ToleranceEqualityComparer(double tolerance = 1)
        {
            Tolerance = tolerance;
        }

        public bool Equals(double x, double y)
        {
            return x - Tolerance <= y && x + Tolerance > y;

        }

        public int GetHashCode(double obj) => 1;
    }

    public interface IDeconvolutable
    {
        public DeconvolutionScore Score { get; }
    }

}
