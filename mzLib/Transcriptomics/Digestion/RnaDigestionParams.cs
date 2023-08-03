using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;

namespace Transcriptomics
{
    public class RnaDigestionParams : DigestionParametersBase
    {
        public Rnase Rnase
        {
            get => (Rnase)Enzyme;
            private set => Enzyme = value;
        }
        public RnaDigestionParams(string rnase = "top-down", int maxMissedCleavages = 0, int minLength = 1, 
            int maxLength = int.MaxValue, int maxModificationIsoforms = 1024, int maxMods = 2) 
            : base(maxMissedCleavages, minLength, maxLength, maxModificationIsoforms, maxMods)
        {
            Rnase = RnaseDictionary.Dictionary[rnase];
        }
    }
}
