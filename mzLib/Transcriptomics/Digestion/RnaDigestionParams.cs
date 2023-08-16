using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;

namespace Transcriptomics
{
    public class RnaDigestionParams : IDigestionParams
    {
       
        public RnaDigestionParams(string rnase = "top-down", int maxMissedCleavages = 0, int minLength = 1, 
            int maxLength = int.MaxValue, int maxModificationIsoforms = 1024, int maxMods = 2) 
        {
            Rnase = RnaseDictionary.Dictionary[rnase];
            MaxMissedCleavages = maxMissedCleavages;
            MinPeptideLength = minLength;
            MaxPeptideLength = maxLength;
            MaxModsForPeptide = maxModificationIsoforms;
            MaxModificationIsoforms = maxModificationIsoforms;
        }

        public int MaxMissedCleavages { get; set; }
        public int MinPeptideLength { get; set; }
        public int MaxPeptideLength { get; set; }
        public int MaxModificationIsoforms { get; set; }
        public int MaxModsForPeptide { get; set; }
        public DigestionAgent Enzyme => Rnase;
        public Rnase Rnase { get; private set; }
    }
}
