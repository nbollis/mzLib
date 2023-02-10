using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;
using Easy.Common.Extensions;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using MzLibUtil;
using Proteomics.ProteolyticDigestion;

namespace Test.AveragingTests
{
    public class MzMatcher
    {
        private static int minChargeState = 1;
        private static int maxChargeState = 50;
        private static PpmTolerance tolerance = new(50);
        private static double relativeIntensityCutoff = 0.2;


        public string Accession { get; set; }
        public List<MsDataScan> Ms1Scans { get; set; }
        public Dictionary<int, double> ChargeStates { get; set; }
        public Dictionary<int, double> ScoredChargeStates { get; set; }

        public MzMatcher(string accession, List<MsDataScan> scans, Dictionary<int, double> chargeAndMz)
        {
            Accession = accession;
            Ms1Scans = scans.Where(p => p.MsnOrder == 1).ToList();
            ChargeStates = chargeAndMz;
        }

        public void ScoreScans()
        {
            Dictionary<int, double> chargeStateScores = ChargeStates.ToDictionary
                (p => p.Key, p => 0.0);
            int scanCount = Ms1Scans.Count;

            foreach (var spectra in Ms1Scans.Select(p => p.MassSpectrum))
            {
                var yCutoff = spectra.YArray.Max() * relativeIntensityCutoff;
                foreach (var chargeState in ChargeStates)
                {
                    // relative intensity
                    // TODO: change relative intensity to noise level
                    var peaksWithinTolerance = spectra.XArray.Where(p =>
                        tolerance.Within(p, chargeState.Value)).ToList();

                    var peaksWithinToleranceAboveCutoff =
                        peaksWithinTolerance.Where(p => spectra.YArray[spectra.XArray.IndexOf(p)] >= yCutoff).ToList();

                    if (peaksWithinToleranceAboveCutoff.Any())
                        chargeStateScores[chargeState.Key] += 1;
                }
            }

            // normalize counts to number of spectra
            for (int i = minChargeState; i < maxChargeState; i++)
            {
                chargeStateScores[i] /= scanCount;
            }

            ScoredChargeStates = chargeStateScores;
        }

    }
}
