﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using IO.MzML;
using MassSpectrometry;
using MzLibUtil;
using NUnit.Framework;
using SpectralAveraging;

namespace Test.AveragingTests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class TestAveragingSpectraWriteFile
    {
        public static string OutputDirectory;
        public static string SpectraPath;
        public static SpectralAveragingParameters Parameters;
        public static List<MsDataScan> Scans;
        public static MsDataScan[] DdaCompositeSpectra;

        [OneTimeSetUp]
        public static void OneTimeSetup()
        {
            Parameters = new SpectralAveragingParameters();
            OutputDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, @"AveragingTestData");
            SpectraPath = Path.Combine(OutputDirectory, "TDYeastFractionMS1.mzML");
            Scans = SpectraFileHandler.LoadAllScansFromFile(SpectraPath).Take(50).ToList();

            Parameters.SpectraFileAveragingType = SpectraFileAveragingType.AverageDdaScansWithOverlap;
            DdaCompositeSpectra = SpectraFileAveraging.AverageSpectraFile(Scans, Parameters);
            Assert.That(DdaCompositeSpectra.Length > 1);

        }

        [OneTimeTearDown]
        public static void OneTimeTearDown()
        {
            if (Directory.Exists(Path.Combine(OutputDirectory, "AveragedSpectra")))
                Directory.Delete(Path.Combine(OutputDirectory, "AveragedSpectra"), true);
            string[] filesToDelete =
                Directory.GetFiles(OutputDirectory).Where(p => p.Contains("Averaged")).ToArray();
            foreach (var file in filesToDelete)
            {
                File.Delete(file);
            }
            Assert.That(Directory.GetFiles(OutputDirectory).Length == 2);
        }

        [Test]
        public static void OutputAveragedSpectraAsMzMLTest()
        {
            // test that it outputs correctly
            Assert.That(Parameters.OutputType == OutputType.MzML);
            AveragedSpectraWriter.WriteAveragedScans(DdaCompositeSpectra, Parameters, SpectraPath);
            string averagedSpectraPath = Path.Combine(OutputDirectory,
                "Averaged_" + Path.GetFileNameWithoutExtension(SpectraPath) + ".mzML");
            Assert.That(File.Exists(averagedSpectraPath));

            var temp = Mzml.LoadAllStaticData(averagedSpectraPath);
            MsDataScan[] loadedScans = temp.GetAllScansList().ToArray();
            for (var i = 0; i < loadedScans.Length; i++)
            {
                for (int j = 0; j < loadedScans[i].MassSpectrum.YArray.Length; j++)
                {
                    Assert.That(Math.Abs(loadedScans[i].MassSpectrum.XArray[j] - DdaCompositeSpectra[i].MassSpectrum.XArray[j]) < 0.0001);
                    Assert.That(Math.Abs(loadedScans[i].MassSpectrum.YArray[j] - DdaCompositeSpectra[i].MassSpectrum.YArray[j]) < 0.0001);
                }
            }

            // test errors
            var exception = Assert.Throws<MzLibException>(() =>
            {
                AveragedSpectraWriter.WriteAveragedScans(DdaCompositeSpectra, Parameters, "");
            });
            Assert.That(exception.Message == "Cannot Access Spectra Directory");
        }

        [Test]
        public static void OutputAveragedSpectraAsTxt()
        {
            Parameters.OutputType = OutputType.Text;
            Assert.That(Parameters.OutputType == OutputType.Text);

            Parameters.SpectraFileAveragingType = SpectraFileAveragingType.AverageDdaScansWithOverlap;
            AveragedSpectraWriter.WriteAveragedScans(DdaCompositeSpectra, Parameters, SpectraPath);
            Assert.That(Directory.Exists(Path.Combine(OutputDirectory, "AveragedSpectra")));
            string[] txtFiles = Directory.GetFiles(Path.Combine(OutputDirectory, "AveragedSpectra"))
                .OrderBy(p => double.Parse(p.Split('_').Last().Replace(".txt", ""))).ToArray();
            Assert.That(txtFiles.Length == DdaCompositeSpectra.Length);
            for (var i = 0; i < txtFiles.Length; i++)
            {

                double[] xArray = new double[DdaCompositeSpectra[i].MassSpectrum.XArray.Length];
                double[] yArray = new double[DdaCompositeSpectra[i].MassSpectrum.YArray.Length];
                string[] mzAndInts = File.ReadAllLines(txtFiles[i]);
                for (var j = 0; j < mzAndInts.Length; j++)
                {
                    xArray[j] = double.Parse(mzAndInts[j].Split(',')[0]);
                    yArray[j] = double.Parse(mzAndInts[j].Split(',')[1]);
                }

                MzSpectrum loadedSpectra = new(xArray, yArray, false);
                Assert.That(loadedSpectra.Equals(DdaCompositeSpectra[i].MassSpectrum));
            }

            // test errors
            var exception = Assert.Throws<MzLibException>(() => 
            { 
                AveragedSpectraWriter.WriteAveragedScans(DdaCompositeSpectra, Parameters, "");
            } );
            Assert.That(exception.Message == "Cannot Access Spectra Directory");

        }
    }
}
