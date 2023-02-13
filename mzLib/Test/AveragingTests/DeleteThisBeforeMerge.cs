using IO.ThermoRawFileReader;
using MassSpectrometry;
using MzLibUtil;
using NUnit.Framework;
using SpectralAveraging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easy.Common.Extensions;
using MathNet.Numerics.Statistics;
using MzLibUtil.MrsNoiseEstimation;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.LayoutObjects;
using TopDownProteomics;
using Plotly.NET;
using Plotly.NET.CSharp;
using Plotly.NET.LayoutObjects;
using Chart = Plotly.NET.CSharp.Chart;

namespace Test.AveragingTests
{
    [TestFixture]
    public static class DeleteThisBeforeMerge
    {

        private const string JurkatPath = @"D:\DataFiles\JurkatTopDown\FXN7_tr1_032017.raw";
        private const string JurkatPathNoRejection = @"D:\Projects\SpectralAveraging\Comparing Noise Level\AveragedFiles\Jurkat-FXN7-averaged-NoRejection.mzML";
        private const string JurkatPathSigma = @"D:\Projects\SpectralAveraging\Comparing Noise Level\AveragedFiles\Jurkat-FXN7-averaged-Sigma1.5.mzML";
        private const string JurkatPathWinsorized = @"D:\Projects\SpectralAveraging\Comparing Noise Level\AveragedFiles\";
        private const string JurkatPathAvgSigma = @"D:\Projects\SpectralAveraging\Comparing Noise Level\AveragedFiles\Jurkat-FXN7-averaged-AveragedSigma1.5.mzML";

        private const string HelaPath = @"D:\DataFiles\Hela_1\20100611_Velos1_TaGe_SA_Hela_3.raw";
        private const string HelaPathNoRejection = @"D:\Projects\SpectralAveraging\Comparing Noise Level\AveragedFiles\Hela3-averaged-NoRejection.mzML";
        private const string HelaPathSigma = @"D:\Projects\SpectralAveraging\Comparing Noise Level\AveragedFiles\Hela3-averaged-Sigma1.5.mzML";
        private const string HelaPathWinsorized = @"D:\Projects\SpectralAveraging\Comparing Noise Level\AveragedFiles\Hela3-averaged-Winsorized.mzML";
        private const string HelatPathAvgSigma = @"D:\Projects\SpectralAveraging\Comparing Noise Level\AveragedFiles\Hela3-averaged-AveragedSigma1.5.mzML";
        
        private const string UbqPath = @"R:\Nic\Chimera Validation\SingleStandards\221110_UbiqOnly_50IW.raw";
        private const string UbqPathNoRejection = @"D:\Projects\SpectralAveraging\Comparing Noise Level\AveragedFiles\UbiqOnly-averaged-NoRejection.mzML";
        private const string UbqPathSigma = @"D:\Projects\SpectralAveraging\Comparing Noise Level\AveragedFiles\UbiqOnly-averaged-Sigma1.5.mzML";
        private const string UbqPathWinsorized = @"D:\Projects\SpectralAveraging\Comparing Noise Level\AveragedFiles\";
        private const string UbqPathAvgSigma = @"D:\Projects\SpectralAveraging\Comparing Noise Level\AveragedFiles\UbiqOnly-averaged-AveragedSigma1.5.mzML";

        [Test]
        public static void CompareNoiseInAveragedShit()
        {
            var ms1Scans = SpectraFileHandler.LoadAllScansFromFile(UbqPathAvgSigma)
                .Where(p => p.MsnOrder == 1 && p.OneBasedScanNumber >= 1004);

            foreach (var scan in ms1Scans)
            {
                var noise = new NoiseEstimationMethodComparison(scan, 500, 90);
                noise.ShowCompositePlot($"Ubiq TD Averaged Sigma {scan.OneBasedScanNumber}");
            }
            
        }

        [Test]
        public static void TestAveragingWithWinsorized()
        {
            SpectralAveragingParameters parameters = new SpectralAveragingParameters()
            {
                SpectraFileAveragingType = SpectraFileAveragingType.AverageDdaScansWithOverlap,
                OutlierRejectionType = OutlierRejectionType.SigmaClipping,
                SpectralWeightingType = SpectraWeightingType.TicValue,
                NormalizationType = NormalizationType.RelativeToTics,
                BinSize = 0.01,
                NumberOfScansToAverage = 5,
                ScanOverlap = 4,
                MinSigmaValue = 1.5,
                MaxSigmaValue = 1.5,
            };

            var jurkat = ThermoRawFileReader.LoadAllStaticData(UbqPath).GetAllScansList();
            var averaged = SpectraFileAveraging.AverageSpectraFile(jurkat, parameters);
            AveragedSpectraWriter.WriteAveragedScans(averaged, parameters, UbqPath);
        }

        [Test]
        public static void TestAveragingMethodsWithNoiseComparison()
        {
            //var standardsDirectory = @"R:\Nic\Chimera Validation\SingleStandards";
            List<string> files = new(); /*Directory.GetFiles(standardsDirectory).Where(p => p.Contains(".raw")).ToList();*/
            //files.Add(@"D:\DataFiles\JurkatTopDown\FXN7_tr1_032017.raw");
            //files.Add(@"D:\DataFiles\Hela_1\20100611_Velos1_TaGe_SA_Hela_3.raw");
            files.Add(JurkatPathAvgSigma);
            files.Add(JurkatPathNoRejection);
            files.Add(JurkatPath);

            int numberOfBins = 500;
            int percentToKeep = 90;
            int ouputtedBins = 150;
            List<WholeSpectraFileNoiseEstimationMethodComparison> noiseComaprisons = new();
            foreach (var file in files)
            {
                var scans = SpectraFileHandler.LoadAllScansFromFile(file)
                    .Where(p => p.MsnOrder == 1)
                    .ToList();

                var original =
                    new WholeSpectraFileNoiseEstimationMethodComparison(Path.GetFileNameWithoutExtension(file), scans,
                        numberOfBins, percentToKeep);
                noiseComaprisons.Add(original);
            }

            string outPath = @"D:\Projects\SpectralAveraging\Comparing Noise Level\NoiseAcrossAllScans.tsv";
            noiseComaprisons.Select(p => (ITsv)p).ExportAsTsv(outPath);
          

        }


        [Test]
        public static void CompareMRSToGausianHistogramFit()
        {
            var standardsDirectory = @"R:\Nic\Chimera Validation\SingleStandards";
            var files = Directory.GetFiles(standardsDirectory).Where(p => p.Contains(".raw")).ToList();
            files.Add(@"D:\DataFiles\JurkatTopDown\FXN7_tr1_032017.raw");
            files.Add(@"D:\DataFiles\Hela_1\20100611_Velos1_TaGe_SA_Hela_3.raw");


            int numberOfBins = 500;
            int percentToKeep = 90;
            int ouputtedBins = 150;
            string fileType = "Jurkat";
            foreach (var scan in ThermoRawFileReader.LoadAllStaticData(files.First(p => p.Contains(fileType))).GetMS1Scans())
            {
                string title =
                    $"{fileType} {scan.OneBasedScanNumber}, {numberOfBins} Bins, {percentToKeep}% peaks kept, {ouputtedBins} Bins Outputted";
                var noise = new NoiseEstimationMethodComparison(scan, numberOfBins, percentToKeep);
                noise.ShowCompositePlot(title);
            }
        }


        [Test]
        public static void FilteringTestShortreedSideQuest()
        {
            string jurkatPath = @"D:\DataFiles\JurkatTopDown\FXN7_tr1_032017.raw";
            var filteringParams = new FilteringParams(numberOfPeaksToKeepPerWindow: 200,
                minimumAllowedIntensityRatioToBasePeak: 0.01, applyTrimmingToMsMs: true);
            var filteredScans = ThermoRawFileReader.LoadAllStaticData(jurkatPath, filteringParams).GetAllScansList()
                .Where(p => p.MsnOrder == 2 && p.OneBasedPrecursorScanNumber >= 1004 &&
                            p.OneBasedPrecursorScanNumber <= 1200).ToList();
            var unfilteredScans = ThermoRawFileReader.LoadAllStaticData(jurkatPath).GetAllScansList().Where(p =>
                    p.MsnOrder == 2 && p.OneBasedPrecursorScanNumber >= 1004 && p.OneBasedPrecursorScanNumber <= 1200)
                .ToList();

            List<(MsDataScan, MsDataScan)> scans = new List<(MsDataScan, MsDataScan)>();
            for (int i = 0; i < filteredScans.Count(); i++)
            {
                scans.Add((filteredScans[i], unfilteredScans[i]));
            }

            using (StreamWriter sw =
                   new StreamWriter(
                       @"D:\Projects\SpectralAveraging\Comparing Noise Level\noiseExplorationJurkatMS2.csv"))
            {
                var filtered = scans.SelectMany(p => p.Item1.MassSpectrum.YArray).ToList();
                var unfiltered = scans.SelectMany(p => p.Item2.MassSpectrum.YArray).ToList();

                sw.WriteLine("Unfiltered,Filtered");
                for (int i = 0; i < unfiltered.Count; i++)
                {
                    if (i > filtered.Count - 1)
                        sw.WriteLine(unfiltered[i]);
                    else
                        sw.WriteLine(unfiltered[i] + "," + filtered[i]);
                }

            }
        }


        [Test]
        public static void HistogramTest()
        {
            string jurkatPath = @"D:\DataFiles\JurkatTopDown\FXN7_tr1_032017.raw";
            StringBuilder sb = new StringBuilder();
            var scans = ThermoRawFileReader.LoadAllStaticData(jurkatPath,
                    new FilteringParams(numberOfPeaksToKeepPerWindow: 200, minimumAllowedIntensityRatioToBasePeak: 0.01, applyTrimmingToMs1: true))
                .GetMS1Scans()
                .Where(p => p.OneBasedScanNumber >= 1004).Take(100).ToList();

            var parameters = new SpectralAveragingParameters
            {
                SpectraFileAveragingType = SpectraFileAveragingType.AverageEverynScansWithOverlap,
                NormalizationType = NormalizationType.RelativeToTics,
                OutlierRejectionType = OutlierRejectionType.SigmaClipping,
                SpectralWeightingType = SpectraWeightingType.TicValue,
                MinSigmaValue = 1.5,
                MaxSigmaValue = 1.5,
                BinSize = 0.01,
                NumberOfScansToAverage = 5,
                ScanOverlap = 4,
            };

            var averagedScans = SpectraFileAveraging.AverageSpectraFile(scans, parameters);
            using (StreamWriter sw = new StreamWriter(@"D:\Projects\SpectralAveraging\Comparing Noise Level\noiseExploration100MS1TrimmedJurkatTDAveragingComparison.csv"))
            {
                List<double> unaveraged = new();
                for (int i = 0; i < scans.Count(); i++)
                {
                    var orderdyArray = scans[i].MassSpectrum.YArray.OrderBy(p => p).ToArray();
                    var trimmed = orderdyArray.SubArray(0, (int)(orderdyArray.Length * 0.7));
                    unaveraged.AddRange(trimmed);
                }

                List<double> averaged = new();
                for (int i = 0; i < averagedScans.Count(); i++)
                {
                    var orderdyArray = averagedScans[i].MassSpectrum.YArray.OrderBy(p => p).ToArray();
                    var trimmed = orderdyArray.SubArray(0, (int)(orderdyArray.Length * 0.7));
                    averaged.AddRange(trimmed);
                }

                sw.WriteLine("Unaveraged,Averaged");
                for (int i = 0; i < averaged.Count; i++)
                {
                    if (i > averaged.Count)
                        sw.WriteLine(unaveraged[i]);
                    if (i > unaveraged.Count)
                        sw.WriteLine("," + averaged[i]);
                    else
                        sw.WriteLine(unaveraged[i] + "," + averaged[i]);

                }
            }

            AveragedSpectraWriter.WriteAveragedScans(averagedScans, parameters, @"D:\DataFiles\JurkatTopDown\FXN7_tr1_032017.raw");

            //foreach (var scan in scans)
            //{
            //    bool converged = MRSNoiseEstimator.MRSNoiseEstimation(scan.MassSpectrum.YArray, 0.00001, out double mrsNoise, maxIterations:30, WaveletType.Db4);
            //    if(!converged) { Console.WriteLine("Did not converge");}
            //    var histogramNoiseLevel = scan.MassSpectrum.GetNoiseLevel();

            //    sb.AppendLine($"{mrsNoise},{histogramNoiseLevel}");
            //}

            //using (StreamWriter sw = new StreamWriter(@"D:\Projects\SpectralAveraging\Comparing Noise Level\Jurkat_5000Bins.csv"))
            //{
            //    sw.WriteLine("MrsNoise,Histogram");
            //    sw.Write(sb.ToString());
            //}
        }

        public readonly record struct Bin(double start, double end);
        public static double GetNoiseLevel(this MzSpectrum spectrum)
        {
            double numberOfBins = 5000.0;
            if (spectrum.FirstX == null || spectrum.LastX == null) return -10;

            Dictionary<Bin, List<MzPeak>> histogram = new();
            var peaks = spectrum.Extract(new DoubleRange(spectrum.FirstX.Value, spectrum.LastX.Value))
                .OrderBy(p => p.Mz).ToList();
            double maxIntensity = peaks.Max(p => p.Intensity);
            var binWidth = maxIntensity / numberOfBins;

            for (int i = 0; i < numberOfBins; i++)
            {
                Bin bin = new(binWidth * i, binWidth * (i + 1));
                List<MzPeak> peaksInBin =
                    peaks.Where(p => p.Intensity >= bin.start && p.Intensity < bin.end).ToList();
                histogram.Add(bin, peaksInBin);
            }
            double noiseLevel = histogram.MaxBy(p => p.Value.Count).Value.Average(p => p.Intensity);
            return noiseLevel;
        }

        public static Dictionary<(double start, double end), List<double>> GetHistogram(this MzSpectrum spectrum, int numberOfBins)
        {
            if (spectrum.FirstX == null || spectrum.LastX == null)
            {
                Debugger.Break();
                return null;
            }

            Dictionary<(double start, double end), List<double>> histogram = new();
            var peaks = spectrum.Extract(new DoubleRange(spectrum.FirstX.Value, spectrum.LastX.Value))
                .OrderBy(p => p.Mz).ToList();
            double maxIntensity = peaks.Max(p => p.Intensity);
            var binWidth = maxIntensity / numberOfBins;

            for (int i = 0; i < numberOfBins; i++)
            {
                (double start, double end) bin = new(binWidth * i, binWidth * (i + 1));
                List<double> peaksInBin = spectrum.YArray.Where(p => p >= bin.start && p < bin.end).ToList();
                histogram.Add(bin, peaksInBin);
            }

            return histogram;
        }
    }
}
