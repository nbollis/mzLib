﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MzLibSpectralAveraging
{
    /// <summary>
    /// Reject outliers from a given set of one dimensional data
    /// </summary>
    public static class OutlierRejection
    {
        /// <summary>
        /// Calls the specific rejection function based upon the current static field RejectionType
        /// Can be used for any array of values
        /// </summary>
        /// <param name="valuesToCheckForRejection">list of mz values to evaluate</param>
        /// <param name="parameters">how to reject outliers</param>
        /// <returns>double array of unrejected values</returns>
        public static double[] RejectOutliers(double[] valuesToCheckForRejection, SpectralAveragingParameters parameters)
        {
            var trimmedMzValues = valuesToCheckForRejection;
            switch (parameters.OutlierRejectionType)
            {
                case OutlierRejectionType.NoRejection:
                    break;

                case OutlierRejectionType.MinMaxClipping:
                    trimmedMzValues = MinMaxClipping(valuesToCheckForRejection);
                    break;

                case OutlierRejectionType.PercentileClipping:
                    trimmedMzValues = PercentileClipping(valuesToCheckForRejection, parameters.Percentile);
                    break;

                case OutlierRejectionType.SigmaClipping:
                    trimmedMzValues = SigmaClipping(valuesToCheckForRejection, parameters.MinSigmaValue, parameters.MaxSigmaValue);
                    break;

                case OutlierRejectionType.WinsorizedSigmaClipping:
                    trimmedMzValues = WinsorizedSigmaClipping(valuesToCheckForRejection, parameters.MinSigmaValue, parameters.MaxSigmaValue);
                    break;

                case OutlierRejectionType.AveragedSigmaClipping:
                    trimmedMzValues = AveragedSigmaClipping(valuesToCheckForRejection, parameters.MinSigmaValue, parameters.MaxSigmaValue);
                    break;

                case OutlierRejectionType.BelowThresholdRejection:
                    trimmedMzValues = BelowThresholdRejection(valuesToCheckForRejection);
                    break;
            }
            return trimmedMzValues;
        }

        /// <summary>
        /// Overload for internal spectral averaging BinnedPeak structure
        /// </summary>
        /// <param name="peaks">list of peaks to reject outliers</param>
        /// <param name="parameters">how to reject outliers</param>
        /// <returns></returns>
        internal static List<BinnedPeak> RejectOutliers(List<BinnedPeak> peaks, SpectralAveragingParameters parameters)
        {
            var unrejected = RejectOutliers(peaks.Select(p => p.Intensity).ToArray(), parameters);
            for (var i = 0; i < peaks.Count; i++)
            {
                if (!unrejected.Contains(peaks[i].Intensity))
                {
                    peaks.RemoveAt(i);
                }
            }

            return peaks;
        }

        #region Rejection Functions

        /// <summary>
        /// Reject the max and min of the set
        /// </summary>
        /// <param name="initialValues">array of mz values to evaluate</param>
        /// <returns>list of mz values with outliers rejected</returns>
        private static double[] MinMaxClipping(double[] initialValues)
        {
            var max = initialValues.Max();
            var min = initialValues.Min();

            return initialValues.Where(p => p < max && p > min).ToArray();
        }

        /// <summary>
        /// Removes values that fall outside of the central value by the defined percentile exclusively
        /// </summary>
        /// <param name="initialValues">list of mz values to evaluate</param>
        /// <param name="percentile"></param>
        /// <returns>list of mz values with outliers rejected</returns>
        private static double[] PercentileClipping(double[] initialValues, double percentile)
        {
            var trim = (1 - percentile) / 2;
            var highPercentile = 1 - trim;
            var median = BasicStatistics.CalculateMedian(initialValues);
            var highCutoff = median * (1 + highPercentile);
            var lowCutoff = median * (1 - highPercentile);
            // round to 4-6 decimal places
            return initialValues.Where(p => highCutoff > p && p > lowCutoff).ToArray();
        }

        /// <summary>
        /// Iteratively removes values that fall outside of the central value by the defined StandardDeviation amount
        /// </summary>
        /// <param name="initialValues">list of mz values to evaluate</param>
        /// <param name="sValueMin">the lower limit of inclusion in sigma (standard deviation) units</param>
        /// <param name="sValueMax">the higher limit of inclusion in sigma (standard deviation) units</param>
        /// <returns></returns>
        private static double[] SigmaClipping(double[] initialValues, double sValueMin, double sValueMax)
        {
            var values = initialValues.ToList();
            var n = 0;
            do
            {
                var median = BasicStatistics.CalculateMedian(values);
                var standardDeviation = BasicStatistics.CalculateStandardDeviation(values);
                n = 0;
                for (var i = 0; i < values.Count; i++)
                {
                    if (SigmaClipping(values[i], median, standardDeviation, sValueMin, sValueMax))
                    {
                        values.RemoveAt(i);
                        n++;
                        i--;
                    }
                }
            } while (n > 0);
            var val = values.ToArray();
            return val;
        }

        /// <summary>
        /// Iteratively replaces values that fall outside of the central value by the defined StandardDeviation amount with the values of the median * that standard deviation amount
        /// </summary>
        /// <param name="initialValues">list of mz values to evaluate</param>
        /// <param name="sValueMin">the lower limit of inclusion in sigma (standard deviation) units</param>
        /// <param name="sValueMax">the higher limit of inclusion in sigma (standard deviation) units</param>
        /// <returns></returns>
        private static double[] WinsorizedSigmaClipping(double[] initialValues, double sValueMin, double sValueMax)
        {
            var values = initialValues.ToList();
            int n;
            const double iterationLimitForHuberLoop = 0.00005;
            double medianLeftBound;
            double medianRightBound;
            double windsorizedStandardDeviation;
            do
            {
                if (!values.Any())
                    break;
                var median = BasicStatistics.CalculateNonZeroMedian(values);
                var standardDeviation = BasicStatistics.CalculateNonZeroStandardDeviation(values);
                var toProcess = values.ToArray();
                do // calculates a new median and standard deviation based on the values to do sigma clipping with (Huber loop)
                {
                    medianLeftBound = median - 1.5 * standardDeviation;
                    medianRightBound = median + 1.5 * standardDeviation;
                    toProcess.Winsorize(medianLeftBound, medianRightBound);
                    median = BasicStatistics.CalculateMedian(toProcess);
                    windsorizedStandardDeviation = standardDeviation;
                    standardDeviation = BasicStatistics.CalculateStandardDeviation(toProcess) * 1.134;
                } while (Math.Abs(standardDeviation - windsorizedStandardDeviation) / windsorizedStandardDeviation > iterationLimitForHuberLoop);

                n = 0;
                for (var i = 0; i < values.Count; i++)
                {
                    if (SigmaClipping(values[i], median, standardDeviation, sValueMin, sValueMax))
                    {

                        values.RemoveAt(i);
                        n++;
                        i--;
                    }
                }
            } while (n > 0 && values.Count > 1); // break loop if nothing was rejected, or only one value remains
            return values.ToArray();
        }

        /// <summary>
        /// Iteratively removes values that fall outside of a calculated deviation based upon the median of the values
        /// </summary>
        /// <param name="initialValues">list of mz values to evaluate</param>
        /// <param name="sValueMin">the lower limit of inclusion in sigma (standard deviation) units</param>
        /// <param name="sValueMax">the higher limit of inclusion in sigma (standard deviation) units</param>
        /// <returns></returns>
        private static double[] AveragedSigmaClipping(double[] initialValues, double sValueMin, double sValueMax)
        {
            var values = initialValues.ToList();
            var median = BasicStatistics.CalculateNonZeroMedian(initialValues);
            var deviation = BasicStatistics.CalculateNonZeroStandardDeviation(initialValues, median);
            var n = 0;
            double standardDeviation;
            do
            {
                median = BasicStatistics.CalculateNonZeroMedian(values);
                standardDeviation = deviation * Math.Sqrt(median) / 10;

                n = 0;
                for (var i = 0; i < values.Count; i++)
                {
                    var temp = (values[i] - median) / standardDeviation;
                    if (SigmaClipping(values[i], median, standardDeviation, sValueMin, sValueMax))
                    {

                        values.RemoveAt(i);
                        n++;
                        i--;
                    }
                }
            } while (n > 0);
            return values.ToArray();
        }

        /// <summary>
        /// Sets the array of mz values to null if they have 20% or fewer values than the number of scans
        /// </summary>
        /// <param name="initialValues">array of mz values to evaluate</param>
        /// <param name="cutoffValue">percent in decimal form of where to cutoff </param>
        /// <returns></returns>
        private static double[] BelowThresholdRejection(double[] initialValues, double cutoffValue = 0.2)
        {
            var scanCount = initialValues.Length;
            if (initialValues.Count(p => p != 0) <= scanCount * cutoffValue)
            {
                initialValues = new double[scanCount];
            }
            return initialValues;
        }

        #endregion

        #region Private Helpers


        /// <summary>
        /// Helper method to mutate the array of doubles based upon the median value
        /// </summary>
        /// <param name="initialValues">initial values to process</param>
        /// <param name="medianLeftBound">minimum the element in the data set is allowed to be</param>
        /// <param name="medianRightBound">maximum the element in the data set is allowed to be</param>
        /// <returns></returns>
        private static void Winsorize(this double[] initialValues, double medianLeftBound, double medianRightBound)
        {
            for (var i = 0; i < initialValues.Length; i++)
            {
                if (initialValues[i] < medianLeftBound)
                {
                    if (i < initialValues.Length && initialValues.Any(p => p > medianLeftBound))
                        initialValues[i] = initialValues.First(p => p > medianLeftBound);
                    else
                        initialValues[i] = medianLeftBound;
                }
                else if (initialValues[i] > medianRightBound)
                {
                    if (i != 0 && initialValues.Any(p => p < medianRightBound))
                        initialValues[i] = initialValues.Last(p => p < medianRightBound);
                    else
                        initialValues[i] = medianRightBound;
                }
            }
        }

        /// <summary>
        /// Helper delegate method for sigma clipping
        /// </summary>
        /// <param name="value">the value in question</param>
        /// <param name="median">median of the data set</param>
        /// <param name="standardDeviation">standard dev of the data set</param>
        /// <param name="sValueMin">the lower limit of inclusion in sigma (standard deviation) units</param>
        /// <param name="sValueMax">the higher limit of inclusion in sigma (standard deviation) units</param>
        /// <returns></returns>
        private static bool SigmaClipping(double value, double median, double standardDeviation, double sValueMin, double sValueMax)
        {
            if ((median - value) / standardDeviation > sValueMin)
            {
                return true;
            }
            else if ((value - median) / standardDeviation > sValueMax)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

    }
}