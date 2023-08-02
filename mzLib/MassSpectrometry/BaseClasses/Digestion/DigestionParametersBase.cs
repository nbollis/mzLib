using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassSpectrometry
{
    public abstract class DigestionParametersBase
    {
        protected DigestionParametersBase(int maxMissedCleavages, int minLength, int maxLength, int maxModificationIsoforms, int maxMods, CleavageSpecificity searchModeType)
        {
            MaxMissedCleavages = maxMissedCleavages;
            MinLength = minLength;
            MaxLength = maxLength;
            MaxModificationIsoforms = maxModificationIsoforms;
            MaxMods = maxMods;
            SearchModeType = searchModeType;
        }


        public int MaxMissedCleavages { get; private set; }
        public int MinLength { get; private set; }
        public int MaxLength { get; private set; }
        public int MaxModificationIsoforms { get; private set; }
        public int MaxMods { get; private set; }
        public CleavageSpecificity SearchModeType { get; private set; }
        
    }
}
