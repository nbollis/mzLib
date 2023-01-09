using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IO.ThermoRawFileReader;
using IO.MzML;
using MassSpectrometry;
using NUnit.Framework;

namespace Test
{
    [TestFixture]
    public static class DeconvolutionScorerTest
    {
        [Test]
        public static void DummyRunThroughTest()
        {
            string regularSpectraPath = @"R:\Nic\Chimera Validation\CaMyoUbiqCytCHgh\221110_CaMyoUbiqCytCHgh_130541641_5%_Sample28_25IW_.raw";
            //string regularSpectraPath = @"D:\Projects\Top Down MetaMorpheus\RawSpectra\FXN6_tr1_032017.raw";
            string avgCaliPath =
                @"D:\Projects\Top Down MetaMorpheus\ChimeraValidation\CaMyoUbiqCytCHgh\Sample28_Avg(20)CaliOpenModern\Task2-CalibrateTask\221110_CaMyoUbiqCytCHgh_130541641_5%_Sample28_25IW_-averaged-calib.mzML";
            string averagedPath =
                @"D:\Projects\Top Down MetaMorpheus\ChimeraValidation\CaMyoUbiqCytCHgh\Sample28_Avg(20)CaliOpenModern\Task1-AveragingTask\221110_CaMyoUbiqCytCHgh_130541641_5%_Sample28_25IW_-averaged.mzML";
            //string averagedPath = @"D:\Projects\Top Down MetaMorpheus\RawSpectra\AveragedSpectra\Task1-AveragingTask\FXN6_tr1_032017-averaged.mzML";


                var regularScans = ThermoRawFileReader.LoadAllStaticData(regularSpectraPath).GetMS1Scans();
            //var avgCaliScans = Mzml.LoadAllStaticData(avgCaliPath).GetAllScansList();
            var averagedScans = Mzml.LoadAllStaticData(averagedPath).GetMS1Scans();

            var regFirst = regularScans.First();
            //var avgCalFirst = avgCaliScans.First();
            var averagedFirst = averagedScans.First();

            Deconvoluter deconvoluter = new Deconvoluter(DeconvolutionTypes.ClassicDeconvolution,
                new ClassicDeconvolutionParameters(1, 60, 20, 3));

            var regEnvelopes = deconvoluter.Deconvolute(regFirst).OrderByDescending(p => p.MonoisotopicMass); // count = 1688
            //var avgCalEnvelopes = deconvoluter.Deconvolute(avgCalFirst); // count = 3822
            var averagedEnvelope = deconvoluter.Deconvolute(averagedFirst).OrderByDescending(p => p.MonoisotopicMass).Take(20).ToList();

            List<IsotopicEnvelope> regularEnvelopes = new List<IsotopicEnvelope>();
            foreach (var scan in regularScans)
            {
                regularEnvelopes.AddRange(deconvoluter.Deconvolute(scan));
            }
            //regularEnvelopes.ProcessEnvelopes("All TD Raw Spectra", regularScans.Count());

            List<IsotopicEnvelope> averagedEnvelopes = new List<IsotopicEnvelope>();
            foreach (var scan in averagedScans)
            {
                averagedEnvelopes.AddRange(deconvoluter.Deconvolute(scan));
            }
            //averagedEnvelopes.ProcessEnvelopes("All TD Averaged Spectra", averagedScans.Count());





            //var deconvolutedSpectra = regEnvelopes.ToDeconvolutedSpectrum();



            //string outPath = @"D:\Projects\Top Down MetaMorpheus\ChimeraValidation\DeconExploration";
            //Output(deconvolutedSpectra, Path.Combine(outPath, "decon.csv"));
            //Output(regFirst.MassSpectrum, Path.Combine(outPath, "regular.csv"));
        }

        private static void Output(MzSpectrum spec, string filepath)
        {
            using (StreamWriter sw = new(File.Create(filepath)))
            {
                for (int i = 0; i < spec.XArray.Length; i++)
                {
                    sw.WriteLine(spec.XArray[i] + "," + spec.YArray[i]);
                }
            }
        }

        private static void ProcessEnvelopes(this IEnumerable<IsotopicEnvelope> envelopes, string title, double scans)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(title);
            sb.AppendLine($"Envelopes:,{envelopes.Count()}");
            sb.AppendLine($"Scans:,{scans}");
            sb.AppendLine($"Average Score:,{envelopes.Select(p => p.Score).Average()}");

            var groupedByPeakCount = envelopes.GroupBy(p => p.Peaks.Count).OrderByDescending(p => p.Key);
            sb.AppendLine($"Number of Peaks,Count of Envelopes");
            foreach (var group in groupedByPeakCount)
            {
                sb.AppendLine($"{group.Key},{group.Count()}");
            }

            string filepath = @$"D:\Projects\Top Down MetaMorpheus\ChimeraValidation\EnvelopeExploration\envelopeCounting_{title}.csv";
            using (StreamWriter sw = new(File.Create(filepath)))
            {
                sw.Write(sb.ToString());
            }
        }
    }
}
