using MassSpectrometry;
using MzLibUtil;
using SpectralAveraging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nett.TomlObjectFactory;

namespace Test.AveragingTests
{
    public class MzMatcher
    {
        private static int minChargeState = 1;
        private static int maxChargeState = 50;
        private static PpmTolerance tolerance = new(20);
        private static double snrCutoff = 3;

        private int percentageOfPeaksToMakeHistogram;
        private List<SpectralAveragingParameters> parameters;
        private List<MsDataScan> originalMs1Scans;
        private Dictionary<int, double> chargeStateAndMz;
        public List<AveragingMatcherResults> Results { get; set; }

        public MzMatcher(List<SpectralAveragingParameters> parameters, List<MsDataScan> originalScans, Dictionary<int, double> chargeAndMz, int percentageOfPeaksToMakeHistogram)
        {
            this.parameters = parameters;
            originalMs1Scans = originalScans.Where(p => p.MsnOrder == 1).ToList();
            chargeStateAndMz = chargeAndMz;
            this.percentageOfPeaksToMakeHistogram = percentageOfPeaksToMakeHistogram;

            Results = new List<AveragingMatcherResults>();
            UnaveragedMatcherResults.OriginalScanCount = originalMs1Scans.Count;
            UnaveragedMatcherResults.NoiseEstimations =
                new NoiseEstimationMethodComparison(originalScans.Select(p => p.MassSpectrum).ToList(), percentageOfPeaksToMakeHistogram);
            UnaveragedMatcherResults.OriginalScansScore = ScoreChargeStates(originalScans,
                UnaveragedMatcherResults.NoiseEstimations.MrsNoiseEstimation);
        }

        public void ScoreAllAveragingParameters()
        {
            foreach (var averagingParameter in parameters)
            {
                Stopwatch sw = Stopwatch.StartNew();
                var averagedScans = SpectraFileAveraging.AverageSpectraFile(originalMs1Scans, averagingParameter);
                sw.Stop();
                var noise = new NoiseEstimationMethodComparison(averagedScans.Select(p => p.MassSpectrum).ToList(), percentageOfPeaksToMakeHistogram);
                var score = ScoreChargeStates(averagedScans, noise.MrsNoiseEstimation);

                Results.Add(new AveragingMatcherResults(averagingParameter, averagedScans.Length, score, sw.ElapsedMilliseconds, noise));
            }
        }

        private Dictionary<int, double> ScoreChargeStates(IReadOnlyCollection<MsDataScan> scans, double noiseLevel)
        {
            Dictionary<int, double> chargeStateScores = chargeStateAndMz.ToDictionary
                (p => p.Key, p => 0.0);
            int scanCount = scans.Count(p => p.MsnOrder == 1);

            foreach (var spectra in scans.Where(p => p.MsnOrder == 1).Select(p => p.MassSpectrum))
            {
                var yCutoff = noiseLevel * snrCutoff;
                foreach (var chargeState in chargeStateAndMz)
                {
                    var peaksWithinTolerance = spectra.XArray.Where(p =>
                        tolerance.Within(p, chargeState.Value)).ToList();

                    var peaksWithinToleranceAboveCutoff =
                        peaksWithinTolerance.Where(p => spectra.YArray[Array.IndexOf(spectra.XArray, p)] >= yCutoff).ToList();

                    if (peaksWithinToleranceAboveCutoff.Any())
                        chargeStateScores[chargeState.Key] += 1;
                }
            }

            // normalize counts to number of spectra
            for (int i = minChargeState; i < maxChargeState; i++)
            {
                chargeStateScores[i] /= scanCount;
            }

            return chargeStateScores;
        }
    }
}
