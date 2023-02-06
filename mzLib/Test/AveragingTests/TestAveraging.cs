using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using IO.MzML;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using MzLibUtil;
using MzLibUtil.MrsNoiseEstimation;
using NUnit.Framework;
using SpectralAveraging;


namespace Test.AveragingTests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public static class TestAveraging
    {
        public static List<MzSpectrum> DummyMzSpectra { get; set; }
        public static List<MsDataScan> ActualScans { get; set; }

        public static List<MzSpectrum> DummyMzCopy
        {
            get
            {
                List<MzSpectrum> newList = new();
                foreach (var spec in DummyMzSpectra)
                {
                    newList.Add(new MzSpectrum(spec.XArray, spec.YArray, true));
                }
                return newList;
            }
        }

        private static double[][] xArrays;
        private static double[][] yArrays;

        [OneTimeSetUp]
        public static void OneTimeSetup()
        {
            ActualScans = Mzml.LoadAllStaticData(Path.Combine(TestContext.CurrentContext.TestDirectory,
                @"AveragingTestData\TDYeastFractionMS1.mzML")).GetAllScansList();
            double[] xArray = new double[] { 100.1453781, 200, 300, 400, 500, 600, 700, 800, 900.4123745 };
            double[] yArray1 = new double[] { 0, 5, 0, 0, 0, 0, 0, 10, 0, 0 };
            double[] yArray2 = new double[] { 0, 5, 0, 0, 0, 0, 0, 10, 0, 0 };
            double[] yArray3 = new double[] { 0, 5, 0, 0, 0, 0, 0, 10, 0, 0 };
            double[] yArray4 = new double[] { 0, 5, 0, 0, 0, 0, 0, 10, 0, 0 };
            double[] yArray5 = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            List<MzSpectrum> mzSpectra = new();
            mzSpectra.Add(new(xArray, yArray1, true));
            mzSpectra.Add(new(xArray, yArray2, true));
            mzSpectra.Add(new(xArray, yArray3, true));
            mzSpectra.Add(new(xArray, yArray4, true));
            mzSpectra.Add(new(xArray, yArray5, true));
            mzSpectra.Add(new(xArray, yArray1, true));
            mzSpectra.Add(new(xArray, yArray2, true));
            mzSpectra.Add(new(xArray, yArray3, true));
            mzSpectra.Add(new(xArray, yArray4, true));
            mzSpectra.Add(new(xArray, yArray5, true));

            DummyMzSpectra = mzSpectra;
            xArrays = new[]
            {
                new double[] { 0, 1, 2, 3, 3.49, 4 },
                new double[] { 0, 1, 2, 3, 4 },
                new double[] { 0.1, 1.1, 2.1, 3.1, 4.1}
            };
            yArrays = new[]
            {
                new double[] { 10, 11, 12, 12, 13, 14 },
                new double[] { 11, 12, 13, 14, 15 },
                new double[] { 20, 25, 30, 35, 40 }
            };
        }



        [Test]
        public static void filteringTest()
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
                    new FilteringParams(numberOfPeaksToKeepPerWindow: 200, minimumAllowedIntensityRatioToBasePeak: 0.01, applyTrimmingToMs1:true))
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

        [Test]
        public static void TestMzBinning()
        {
            SpectralAveragingParameters parameters = new();
            MzSpectrum[] mzSpectras = new MzSpectrum[DummyMzSpectra.Count];
            DummyMzCopy.CopyTo(mzSpectras);
            var compositeSpectra = mzSpectras.AverageSpectra(parameters);

            double[] expected = new[] { 3.2, 6.4};
            Assert.That(compositeSpectra.XArray.Length == compositeSpectra.YArray.Length);
            Assert.That(expected.SequenceEqual(compositeSpectra.YArray));

            parameters.NormalizationType = NormalizationType.NoNormalization;
            DummyMzCopy.CopyTo(mzSpectras);
            compositeSpectra = mzSpectras.AverageSpectra(parameters);
            expected = new[] { 4.0, 8.0};
            Assert.That(compositeSpectra.XArray.Length == compositeSpectra.YArray.Length);
            Assert.That(expected.SequenceEqual(compositeSpectra.YArray));
        }

        [Test]
        public static void TestWhenAllValuesGetRejected()
        {
            SpectralAveragingParameters parameters = new()
            {
                SpectraFileAveragingType = SpectraFileAveragingType.AverageAll,
                OutlierRejectionType = OutlierRejectionType.MinMaxClipping,
                SpectralWeightingType = SpectraWeightingType.WeightEvenly,
                NormalizationType = NormalizationType.NoNormalization,
                BinSize = 1,
            };

            List<MzSpectrum> spectra = new()
            {
                new MzSpectrum(new[] { 2.0, 3.0, 3.1, 4.0, 4.1 }, new[] { 1.0, 1.1, 1.2, 1.5, 1.6 }, false),
                new MzSpectrum(new[] { 2.0, 3.0, 3.1, 4.0, 4.1 }, new[] { 1.0, 1.3, 1.4, 1.7, 1.8 }, false),
            };

            var averagedSpectra = spectra.AverageSpectra(parameters);
            Assert.That(averagedSpectra.XArray.SequenceEqual(new [] {3.05, 4.05}));
            Assert.That(averagedSpectra.YArray.SequenceEqual(new[]{1.25, 1.65}));
        }

        [Test]
        public static void TestBinningMethod()
        {
            SpectralAveragingParameters parameters = new() { BinSize = 1 };

            var methodInfo = typeof(SpectraAveraging).GetMethod("GetBins", BindingFlags.NonPublic | BindingFlags.Static);
            var bins = (Dictionary<int, List<BinnedPeak>>)methodInfo.Invoke(null, new object?[] { xArrays, yArrays, parameters.BinSize });
            Assert.That(bins != null);
            Assert.That(bins.Count == 5);

            var bin = bins[0];
            Assert.That(bin.Select(p => p.Mz).SequenceEqual(new [] {0, 0, 0.1}));
            Assert.That(bin.Select(p => p.Intensity).SequenceEqual(new [] {10.0, 11, 20}));

            bin = bins[1];
            Assert.That(bin.Select(p => p.Mz).SequenceEqual(new[] { 1, 1, 1.1 }));
            Assert.That(bin.Select(p => p.Intensity).SequenceEqual(new[] { 11.0, 12, 25 }));

            bin = bins[2];
            Assert.That(bin.Select(p => p.Mz).SequenceEqual(new[] { 2, 2, 2.1 }));
            Assert.That(bin.Select(p => p.Intensity).SequenceEqual(new[] { 12, 13, 30.0 }));

            bin = bins[3];
            Assert.That(bin.Select(p => p.Mz).SequenceEqual(new[] { 3, 3.49, 3, 3.1 }));
            Assert.That(bin.Select(p => p.Intensity).SequenceEqual(new[] { 12.0, 13, 14, 35 }));

            bin = bins[4];
            Assert.That(bin.Select(p => p.Mz).SequenceEqual(new[] { 4, 4, 4.1 }));
            Assert.That(bin.Select(p => p.Intensity).SequenceEqual(new[] { 14.0, 15, 40 }));
        }

        [Test]
        public static void TestAverageSpectraError()
        {
            SpectralAveragingParameters parameters = new SpectralAveragingParameters();
            parameters.SpectralAveragingType = (SpectralAveragingType)(-1);

            var exception = Assert.Throws<MzLibException>(() =>
            {
                DummyMzSpectra.AverageSpectra(parameters);
            });
            Assert.That(exception.Message == "Spectrum Averaging Type Not Yet Implemented");
        }

    }
}
