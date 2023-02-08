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
        [Test]
        public static void CompareMRSToGausianHistogramFit()
        {
            var standardsDirectory = @"R:\Nic\Chimera Validation\SingleStandards";
            var files = Directory.GetFiles(standardsDirectory).Where(p => p.Contains(".raw")).ToList();
            files.Add(@"D:\DataFiles\JurkatTopDown\FXN7_tr1_032017.raw");
            files.Add(@"D:\DataFiles\Hela_1\20100611_Velos1_TaGe_SA_Hela_3.raw");

            Dictionary<string, List<MzSpectrum>> fileDict = new();
            foreach (var file in files)
            {
                fileDict.Add(Path.GetFileNameWithoutExtension(file),
                    SpectraFileHandler.LoadAllScansFromFile(file).Where(p => p.MsnOrder == 1)
                        .Select(p => p.MassSpectrum).ToList());
            }

            foreach (var file in fileDict)
            {
                var normType = NormalizationType.RelativeToTics;
                file.Value.NormalizeSpectra(normType);
                IntensityHistogram hist = new(file.Value, 5000, 50);
                hist.OutputWithPlotly($"{file.Key} - {normType}");
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
