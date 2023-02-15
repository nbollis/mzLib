using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Easy.Common.Extensions;
using MzLibUtil;
using Nett;
using TopDownProteomics;

namespace Test.AveragingTests
{
    public interface ITsv
    {
        string TabSeparatedHeader { get; }
        string ToTsvString();
    }

    public static class TsvExtensions
    {
        public static string GetTsvString(this IEnumerable<ITsv> iTsvs)
        {
            var sb = new StringBuilder();

            sb.AppendLine(iTsvs.First().TabSeparatedHeader);
            foreach (var tsv in iTsvs)
            {
                sb.AppendLine(tsv.ToTsvString());
            }
            return sb.ToString();
        }

        public static void ExportAsTsv(this IEnumerable<ITsv> iTsvs, string outPath)
        {
            if (!outPath.EndsWith(".tsv"))
                outPath += ".tsv";

            List<string> headers = new();
            List<Dictionary<string, string>> tsvStringDictionary = new();
            // merge all headers into a single header
            foreach (var run in iTsvs)
            {
                var tsv = new Dictionary<string, string>();
                var headerSplits = run.TabSeparatedHeader.Split('\t');
                var stringSplits = run.ToTsvString().Split('\t');
                headers.AddRange(headerSplits);
                for (int i = 0; i < headerSplits.Length; i++)
                {
                    tsv.TryAdd(headerSplits[i], stringSplits[i]);
                }
                tsvStringDictionary.Add(tsv);
            }

            var distinctHeaders = headers.Distinct().ToList();
            var finalHeader = string.Join("\t", distinctHeaders);
            List<string> finalTsvStrings = new();
            // recreate each Tsv string, filling in blanks where one did not have the header
            foreach (var tsvResult in tsvStringDictionary)
            {
                string tsvString = "";
                foreach (var header in distinctHeaders)
                {
                    if (tsvResult.TryGetValue(header, out string field))
                    {
                        tsvString += field + '\t';
                    }
                    else
                    {
                        tsvString += '\t';
                    }
                }
                finalTsvStrings.Add(tsvString);
            }

            using (var sw = new StreamWriter(File.Create(outPath)))
            {
                sw.WriteLine(finalHeader);
                foreach (var tsv in finalTsvStrings)
                {
                    sw.WriteLine(tsv);
                }
            }
        }

        public static IEnumerable<T> ReadFromTsv<T>(string filePath) where T : ITsv
        {
            List<T> readObjects = new();

            

            var allLines = File.ReadAllLines(filePath);
            var headerSplits = allLines[0].Split('\t');
            Dictionary<string, List<string>> headerAllValues =
                headerSplits.ToDictionary(p => p, p => new List<string>());

            foreach (var line in allLines.SubSequence(1, allLines.Length))
            {
                var lineSplits = line.Split("\t");
                for (int i = 0; i < headerSplits.Length; i++)
                {
                    var key = headerSplits[i];
                    headerAllValues[key].Add(lineSplits[i]);
                }
            }

            switch (typeof(T).Name)
            {
                case nameof(WholeSpectraFileNoiseEstimationMethodComparison):
                    readObjects = (List<T>)ParseWholeSpectraFileNoiseEstimationMethodComparison(headerAllValues);
                    break;
                case nameof(BasicStats):
                    readObjects = (List<T>)ParseBasicStats(headerAllValues);
                    break;
                case nameof(NoiseEstimationMethodComparison):
                    readObjects = (List<T>)ParseNoiseEstimationMethodComparison(headerAllValues);
                    break;

                default:
                    throw new MzLibException("Specific ITsv reading not implemented");
            }


            return readObjects;
        }

        #region Individual ITsv Parsing

        private static IEnumerable<WholeSpectraFileNoiseEstimationMethodComparison> ParseWholeSpectraFileNoiseEstimationMethodComparison(
            Dictionary<string, List<string>> parsedValues)
        {
            var ticStats = ParseBasicStats(parsedValues.Where(p => p.Key.Contains("Tic "))
                .ToDictionary(p => p.Key, p => p.Value)).ToList();
            var mrsNoiseEstimationStats = ParseBasicStats(parsedValues.Where(p => p.Key.StartsWith("Mrs ") && !p.Key.Contains("Noise"))
                .ToDictionary(p => p.Key, p => p.Value)).ToList();
            var averageOfMostAbundantHistogramBinStats = ParseBasicStats(parsedValues.Where(p => p.Key.Contains("Most Abundant Hist "))
                .ToDictionary(p => p.Key, p => p.Value)).ToList();
            var averageOfLastHistogramNoiseBinStats = ParseBasicStats(parsedValues.Where(p => p.Key.Contains("Last Noise Hist "))
                .ToDictionary(p => p.Key, p => p.Value)).ToList();
            var maxSignalOverMrsNoiseStats = ParseBasicStats(parsedValues.Where(p => p.Key.Contains("Max Signal / Mrs "))
                .ToDictionary(p => p.Key, p => p.Value)).ToList();
            var maxSignalOverMaxHistogramNoiseStats = ParseBasicStats(parsedValues.Where(p => p.Key.Contains("Tic "))
                .ToDictionary(p => p.Key, p => p.Value)).ToList();
            var histogramNoiseOverSignalIntegrationStats = ParseBasicStats(parsedValues.Where(p => p.Key.Contains("Hist Noise / Signal Integration "))
                .ToDictionary(p => p.Key, p => p.Value)).ToList();
            var averageOverStDevOfPeaksStats = ParseBasicStats(parsedValues.Where(p => p.Key.Contains("Avg / Stdev "))
                .ToDictionary(p => p.Key, p => p.Value)).ToList();

            List<WholeSpectraFileNoiseEstimationMethodComparison> readObjects = new();
            for (int i = 0; i < parsedValues.First().Value.Count; i++)
            {
                readObjects.Add(new WholeSpectraFileNoiseEstimationMethodComparison()
                {
                    Name = parsedValues["Name"][i],
                    IndividualComparisons = new List<NoiseEstimationMethodComparison>()
                    {
                        new NoiseEstimationMethodComparison()
                        {
                            Tic = double.Parse(parsedValues["Average Tic"][i]),
                            MrsNoiseEstimation = double.Parse(parsedValues["Mrs Noise"][i]),
                            AverageOfMostAbundantHistogramBin = double.Parse(parsedValues["Most Abundant Hist"][i]),
                            AverageOfLastHistogramNoiseBin = double.Parse(parsedValues["Last Noise Hist"][i]),
                            MaxSignalOverMrsNoise = double.Parse(parsedValues["Max Signal / Mrs"][i]),
                            MaxSignalOverMaxHistogramNoise = double.Parse(parsedValues["Max Signal / Last Hist"][i]),
                            HistSignalOverNoiseByHist = double.Parse(parsedValues["Hist Noise / Signal Integration"][i]),
                            AverageOverStDevOfPeaks = double.Parse(parsedValues["Avg / Stdev"][i]),
                        }
                    },
                    TicStats = ticStats[i],
                    MrsNoiseEstimationStats = mrsNoiseEstimationStats[i],
                    AverageOfMostAbundantHistogramBinStats = averageOfMostAbundantHistogramBinStats[i],
                    AverageOfLastHistogramNoiseBinStats = averageOfLastHistogramNoiseBinStats[i],
                    MaxSignalOverMrsNoiseStats = maxSignalOverMrsNoiseStats[i],
                    MaxSignalOverMaxHistogramNoiseStats = maxSignalOverMaxHistogramNoiseStats[i],
                    HistogramNoiseOverSignalIntegrationStats = histogramNoiseOverSignalIntegrationStats[i],
                    AverageOverStDevOfPeaksStats = averageOverStDevOfPeaksStats[i]
                });
            }

            return readObjects;
        }

        private static IEnumerable<BasicStats> ParseBasicStats(Dictionary<string, List<string>> parsedValues)
        {
            List<BasicStats> readObjects = new();
            for (int i = 0; i < parsedValues.First().Value.Count; i++)
            {
                string name = parsedValues.First(p => p.Key.Contains("Mean")).Key.Split("Mean")[0].Trim();
                readObjects.Add(new BasicStats()
                {
                    Name = name,
                    Mean = double.Parse(parsedValues[$"{name} Mean"][i]),
                    Median = double.Parse(parsedValues[$"{name} Median"][i]),
                    Minimum = double.Parse(parsedValues[$"{name} Minimum"][i]),
                    Maximum = double.Parse(parsedValues[$"{name} Maximum"][i]),
                    FirstQuartile = double.Parse(parsedValues[$"{name} FirstQuartile"][i]),
                    ThirdQuartile = double.Parse(parsedValues[$"{name} ThirdQuartile"][i]),
                });
            }
            return readObjects;
        }

        private static IEnumerable<NoiseEstimationMethodComparison> ParseNoiseEstimationMethodComparison(
            Dictionary<string, List<string>> parsedValues)
        {
            List<NoiseEstimationMethodComparison> readObjects = new();
            for (int i = 0; i < parsedValues.First().Value.Count; i++)
            {
                readObjects.Add(new NoiseEstimationMethodComparison()
                {
                    ScanNumber = double.Parse(parsedValues["Scan Number"][i]),
                    Tic = double.Parse(parsedValues["Tic"][i]),
                    MrsNoiseEstimation = double.Parse(parsedValues["Mrs Noise"][i]),
                    AverageOfMostAbundantHistogramBin = double.Parse(parsedValues["Most Abundant Hist"][i]),
                    AverageOfLastHistogramNoiseBin = double.Parse(parsedValues["Last Noise Hist"][i]),
                    MaxSignalOverMrsNoise = double.Parse(parsedValues["Max Signal / Mrs"][i]),
                    MaxSignalOverMaxHistogramNoise = double.Parse(parsedValues["Max Signal / Last Hist"][i]),
                    HistSignalOverNoiseByHist = double.Parse(parsedValues["Hist Signal / Noise Integration By Last His"][i]),
                    HistSignalOverNoiseByMrs = double.Parse(parsedValues["Hist Signal / Noise Integration By Mrs"][i]),
                    AverageOverStDevOfPeaks = double.Parse(parsedValues["Avg / Stdev"][i]),
                });
            }
            return readObjects;
        }

        #endregion
    }

}
