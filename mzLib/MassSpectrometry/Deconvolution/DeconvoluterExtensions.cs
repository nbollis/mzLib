using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;
using MzLibUtil;

namespace MassSpectrometry
{
    /// <summary>
    /// Class to provide extensions for other objects besides MsDataScan to be deconvoluted
    /// Methods can be generated for each deconvolution type, passing parameters that would otherwise be found in the MsDataScan
    /// </summary>
    public static class DeconvoluterExtensions
    {
        public static IEnumerable<IsotopicEnvelope> ClassicDeconvoluteMzSpectra(this Deconvoluter deconvoluter,
            MzSpectrum spectrum, MzRange range)
        {
            if (deconvoluter.DeconvolutionType != DeconvolutionTypes.ClassicDeconvolution)
            {
                throw new MzLibException("Deconvoluter is not of correct type for this extension method");
            }
            else
            {
                ((ClassicDeconvolutionParameters)deconvoluter.DeconvolutionParameters).Range = range;
                return deconvoluter.DeconvolutionAlgorithm.Deconvolute(spectrum);
            }
        }

        public static IEnumerable<ChargeStateEnvelope> ToChargeStateEnvelopes(
            this IEnumerable<IsotopicEnvelope> isoEnvelopes)
        {
            List<ChargeStateEnvelope> chargeStateEnvelopes = new();
            var groupedByMass = isoEnvelopes.GroupBy(p => p.MonoisotopicMass, new ToleranceEqualityComparer());

            foreach (var group in groupedByMass)
            {
                chargeStateEnvelopes.Add(new ChargeStateEnvelope(group.Select(p => p)));
            }

            //Dictionary<int, int> counts = new Dictionary<int, int>();
            //foreach (var group in groupedByMass)
            //{
            //    if (counts.TryAdd(group.Count(), 1))
            //    {

            //    }
            //    else
            //    {
            //        counts[group.Count()]++;
            //    }
            //}
            return chargeStateEnvelopes;
        }

        public static MzSpectrum ToDeconvolutedSpectrum(this IEnumerable<IsotopicEnvelope> envelopes)
        {
            var allPeaks = new List<MzPeak>();
            foreach (var envelope in envelopes)
            {
                foreach (var peak in envelope.Peaks)
                {
                    double mz = peak.mz * envelope.Charge - Constants.ProtonMass * envelope.Charge;
                    allPeaks.Add(new MzPeak(mz, peak.intensity));
                }
            }
            var orderedPeaks = allPeaks.OrderBy(p => p.Mz).ToList();
            return new MzSpectrum(orderedPeaks.Select(p => p.Mz).ToArray(), 
                orderedPeaks.Select(p => p.Intensity).ToArray(),
                true);
        }

        public static IEnumerable<IsotopicEnvelope> MergeLikeEnvelopesWithinTolerance(
            this IEnumerable<IsotopicEnvelope> envelopes, double daTolerance)
        {
            var groupedByMonoMass = envelopes
                .GroupBy(p => p.MonoisotopicMass, new ToleranceEqualityComparer(daTolerance)).ToList();
            var groupedByAbundantMass = envelopes.GroupBy(p => p.MostAbundantObservedIsotopicMass,
                new ToleranceEqualityComparer(daTolerance)).ToList();



            return new List<IsotopicEnvelope>();
        }
    }
}
