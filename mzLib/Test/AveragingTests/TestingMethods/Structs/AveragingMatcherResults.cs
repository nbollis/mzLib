using MzLibUtil;
using SpectralAveraging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easy.Common.Extensions;

namespace Test.AveragingTests
{
    public readonly record struct AveragingMatcherResults : ITsv
    {

        #region Scoring Metrics

        public double TimeToAverageInMilliseconds { get; init; }
        public int NumberOfScansAfterAveraging { get; init; }
        public int NumberOfChargeStatesObserved { get; init; }
        public static NoiseEstimationMethodComparison NoiseEstimations { get; private set; }
        public double SumOfScores { get; init; }
        public int FoundIn90PercentOfScans { get; init; }

        #endregion

        public SpectralAveragingParameters Parameters { get; init; }


        public AveragingMatcherResults(SpectralAveragingParameters parameters, int numOfScans,
            Dictionary<int, double> chargeStateScores, double timeToAverageInMilliseconds, NoiseEstimationMethodComparison noiseEstimation)
        {
            if (UnaveragedMatcherResults.OriginalScanCount.IsDefault())
                throw new MzLibException("Gotta initialize the original before averaging my guy");

            NoiseEstimations = noiseEstimation;
            Parameters = parameters;
            TimeToAverageInMilliseconds = timeToAverageInMilliseconds;
            NumberOfScansAfterAveraging = numOfScans;

            var observedChargeStates = chargeStateScores.Where(p => p.Value != 0).ToDictionary(p => p.Key, p => p.Value);
            NumberOfChargeStatesObserved = observedChargeStates.Count;
            FoundIn90PercentOfScans = observedChargeStates.Count(p => p.Value >= 0.9);

            SumOfScores = Math.Round(chargeStateScores.Values.Sum(), 2);
        }


        public string TabSeparatedHeader
        {
            get
            {
                var sb = new StringBuilder();
                // results
                sb.Append("Milliseconds To Average\t");
                sb.Append("Scan Count\t");
                sb.Append("Observed Charge States\t");
                sb.Append("Sum of Scores\t");
                sb.Append("90%\t");
                sb.Append($"{NoiseEstimations.TabSeparatedHeader}\t");

                // averaging stuff
                sb.Append("Bin Size\t");
                sb.Append("Scans To Average\t");
                sb.Append("Scan Overlap\t");
                sb.Append("Weighting Type\t");
                sb.Append("Normalization Type\t");
                sb.Append("Rejection Type\t");
                if (Parameters.OutlierRejectionType.ToString().Contains("Sigma"))
                {
                    sb.Append("Min Sigma\t");
                    sb.Append("Max Sigma\t");
                }
                else if (Parameters.OutlierRejectionType.ToString().Contains("Percentile"))
                    sb.Append("Percentile\t");

                var tsvString = sb.ToString().TrimEnd('\t');
                return tsvString;
            }
        }

        public string ToTsvString()
        {
            var sb = new StringBuilder();
            // results
            sb.Append($"{TimeToAverageInMilliseconds}\t");
            sb.Append($"{NumberOfScansAfterAveraging}\t");
            sb.Append($"{NumberOfChargeStatesObserved}\t");
            sb.Append($"{SumOfScores}\t");
            sb.Append($"{FoundIn90PercentOfScans}\t");
            sb.Append($"{NoiseEstimations.ToTsvString()}\t");

            // averaging stuff
            sb.Append($"{Parameters.BinSize}\t");
            sb.Append($"{Parameters.NumberOfScansToAverage}\t");
            sb.Append($"{Parameters.ScanOverlap}\t");
            sb.Append($"{Parameters.SpectralWeightingType}\t");
            sb.Append($"{Parameters.NormalizationType}\t");
            sb.Append($"{Parameters.OutlierRejectionType}\t");
            if (Parameters.OutlierRejectionType.ToString().Contains("Sigma"))
            {
                sb.Append($"{Parameters.MinSigmaValue}\t");
                sb.Append($"{Parameters.MaxSigmaValue}\t");
            }
            else if (Parameters.OutlierRejectionType.ToString().Contains("Percentile"))
                sb.Append($"{Parameters.Percentile}\t");
            var tsvString = sb.ToString().TrimEnd('\t');
            return tsvString;
        }
    }
}
