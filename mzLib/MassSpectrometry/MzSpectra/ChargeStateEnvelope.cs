using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassSpectrometry
{
    public class ChargeStateEnvelope
    {
        public double AverageMonoIsotopicMass { get; set; }
        public double AverageMostAbundantMass { get; set; }
        public List<IsotopicEnvelope> IsotopicEnvelopes { get; set; }

        public ChargeStateEnvelope(IEnumerable<IsotopicEnvelope> envelopes)
        {
            var isotopicEnvelopes = envelopes.ToList();
            IsotopicEnvelopes = isotopicEnvelopes;
            AverageMostAbundantMass = isotopicEnvelopes.Select(p => p.MostAbundantObservedIsotopicMass).Average();
            AverageMonoIsotopicMass = isotopicEnvelopes.Select(p => p.MonoisotopicMass).Average();
        }
    }
}
