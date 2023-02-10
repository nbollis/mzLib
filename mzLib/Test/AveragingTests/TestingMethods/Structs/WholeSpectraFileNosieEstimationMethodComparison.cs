using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;

namespace Test.AveragingTests
{
    public record struct WholeSpectraFileNoiseEstimationMethodComparison : ITsv
    {
        public string Name { get; init; }
        public List<NoiseEstimationMethodComparison> IndividualComparisons { get; init; }
        public double MrsNoiseEstimation => IndividualComparisons.Average(p => p.MrsNoiseEstimation);
        public double AverageOfMostAbundantHistogramBin => IndividualComparisons.Average(p => p.AverageOfLastHistogramNoiseBin);
        public double AverageOfLastHistogramNoiseBin => IndividualComparisons.Average(p => p.AverageOfLastHistogramNoiseBin);
        public double MaxSignalOverMrsNoise => IndividualComparisons.Average(p => p.MaxSignalOverMrsNoise);
        public double MaxSignalOverMaxHistogramNoise => IndividualComparisons.Average(p => p.MaxSignalOverMaxHistogramNoise);
        public double HistogramNoiseOverSignalIntegration => IndividualComparisons.Average(p => p.HistogramNoiseOverSignalIntegration);
        public double AverageOverStDevOfPeaks => IndividualComparisons.Average(p => p.AverageOverStDevOfPeaks);

        public WholeSpectraFileNoiseEstimationMethodComparison(string name, List<MsDataScan> scans, int numberOfBins,
            int percentToKeep)
        {
            Name = name;
            IndividualComparisons = new();
            foreach (var scan in scans) 
                IndividualComparisons.Add(new (scan, numberOfBins, percentToKeep));
        }

        public string TabSeparatedHeader
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append("Name\t");
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
            sb.Append($"{Name}\t");
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
