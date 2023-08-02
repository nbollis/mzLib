using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;

namespace Transcriptomics
{
    public class RnaDigestionParameters : DigestionParametersBase
    {
        public Rnase Enzyme { get; set; }
        public RnaDigestionParameters(Rnase rnase, int maxMissedCleavages, int minLength, int maxLength, int maxModificationIsoforms, int maxMods) 
            : base(maxMissedCleavages, minLength, maxLength, maxModificationIsoforms, maxMods, rnase.CleavageSpecificity)
        {
            Enzyme = rnase;
        }
    }
}
