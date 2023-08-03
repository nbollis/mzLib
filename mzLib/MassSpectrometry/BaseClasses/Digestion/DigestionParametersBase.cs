using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassSpectrometry
{
    public abstract class DigestionParametersBase
    {
        protected DigestionParametersBase(int maxMissedCleavages, int minLength, int maxLength, int maxModificationIsoforms, 
            int maxMods)
        {
            MaxMissedCleavages = maxMissedCleavages;
            MinPeptideLength = minLength;
            MaxPeptideLength = maxLength;
            MaxModificationIsoforms = maxModificationIsoforms;
            MaxModsForPeptide = maxMods;
        }


        public int MaxMissedCleavages { get; private set; }
        public int MinPeptideLength { get; private set; }
        public int MaxPeptideLength { get; private set; }
        public int MaxModificationIsoforms { get; private set; }
        public int MaxModsForPeptide { get; private set; }
        protected DigestionAgent Enzyme { get; set; }
        
    }
}
