using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using NUnit.Framework;
using Readers;

namespace Test.FileReadingTests
{
    [TestFixture]
    public class TestMsAlign
    {

        private const string lvs22averaged =
            @"B:\Users\Nic\ScanAveraging\AveragedDataBulkJurkat\TopFD\CalibAveraged\id_02-17-20_jurkat_td_rep2_fract2-calib-averaged-centroided_file\id_02-17-20_jurkat_td_rep2_fract2-calib-averaged-centroided_ms1.msalign";
        private const string lvs22calib =
            @"B:\Users\Nic\ScanAveraging\AveragedDataBulkJurkat\TopFD\Calib\id_02-17-20_jurkat_td_rep2_fract2-calib-centroided_file\id_02-17-20_jurkat_td_rep2_fract2-calib-centroided_ms1.msalign";



        private const string FlashDeconAveragedDirectory =
            @"B:\Users\Nic\ScanAveraging\AveragedDataBulkJurkat\FLASHDeconNoCentroid\CalibAveraged";
        private const string FlashDeconCalibDirectory =
            @"B:\Users\Nic\ScanAveraging\AveragedDataBulkJurkat\FLASHDeconNoCentroid\Calib";

        private const string TopFDAveragedDirectory =
            @"B:\Users\Nic\ScanAveraging\AveragedDataBulkJurkat\TopFD\CalibAveraged";
        private const string TopFDCalibDirectory =
            @"B:\Users\Nic\ScanAveraging\AveragedDataBulkJurkat\TopFD\Calib";


        [Test]
        [TestCase(@"DataFiles/LVS_jurkat_td_rep2_fract2-calib-averaged-centroided_ms1.msalign")]
        public void FirstTestMsAlign(string filePath)
        {
            string spectraPath = Path.Combine(TestContext.CurrentContext.TestDirectory, filePath);
            MsDataFile datafile = MsDataFileReader.GetDataFile(spectraPath);
            var t = datafile.LoadAllStaticData();
        }

        public enum DeconType
        {
            FLASHDecon,
            TopFD,
        }

        [Test]
        [TestCase(FlashDeconCalibDirectory, FlashDeconAveragedDirectory, DeconType.FLASHDecon)]
        [TestCase(TopFDCalibDirectory, TopFDAveragedDirectory, DeconType.TopFD)]
        public void CompareFlashDeconvMs1Align(string calibDirectory, string averagedDirectory, DeconType type)
        {
            var calibFiles = GetMs1AlignFiles(calibDirectory, type);
            var averagedFiles = GetMs1AlignFiles(averagedDirectory, type);

            List<MsAlignResults> results = new();

            for (var i = 0; i < calibFiles.Length; i++)
            {
                var calibFile = calibFiles[i];
                var averagedFile = averagedFiles[i];

                var withoutExtension = Path.GetFileNameWithoutExtension(averagedFile);
                var longName = withoutExtension.Substring(withoutExtension.IndexOf('_') + 1,
                    withoutExtension.Length - withoutExtension.IndexOf('_') - 1);
                var fileName = longName.Remove(longName.IndexOf("-calib"));
                if (type == DeconType.TopFD)
                    fileName = fileName.Substring(fileName.IndexOf('_') + 1, fileName.Length - fileName.IndexOf('_') - 1);

                var calibPeakCount = MsDataFileReader.GetDataFile(calibFile)
                    .GetMS1Scans()
                    .Sum(p => p.MassSpectrum.XArray.Length);
                var averagedPeakCount = MsDataFileReader.GetDataFile(averagedFile)
                    .GetMS1Scans()
                    .Sum(p => p.MassSpectrum.XArray.Length);

                results.Add(new MsAlignResults(type, fileName, calibPeakCount, averagedPeakCount));
            }

            string outDirectory =
                @"C:\Users\Nic\OneDrive - UW-Madison\AUSTIN V CARR - AUSTIN V CARR's files\SpectralAveragingPaper\ResultsData\Deconvolution";
            string outPath = Path.Combine(outDirectory, $"{type}_ms1Align_PeakCounting.csv");
            using (var sw = new StreamWriter(outPath))
            {
                sw.WriteLine("Software,File,Calib,CalibAveraged");
                foreach (var result in results)
                {
                    sw.WriteLine($"{result.Type},{result.FileName},{result.Calib},{result.Averaged}");
                }
            }
        }

        public record struct MsAlignResults(DeconType Type, string FileName, double Calib, double Averaged);


        private string[] GetMs1AlignFiles(string directoryPath, DeconType type)
        {
            return type switch
            {
                DeconType.FLASHDecon => 
                    Directory.GetFiles(directoryPath)
                        .Where(p => p.EndsWith("ms1.msalign"))
                        .OrderBy(p => p)
                        .ToArray(),
                DeconType.TopFD => 
                    Directory.GetDirectories(directoryPath)
                        .Where(p => p.EndsWith("_file"))
                        .SelectMany(p => Directory.GetFiles(p).Where(m => m.EndsWith("ms1.msalign")))
                        .OrderBy(p => p)
                        .ToArray()
            };
        }
    }
}
