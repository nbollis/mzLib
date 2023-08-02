using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;

namespace Transcriptomics
{
    public class OligoWithSetMods : NucleolyticOligo
    {
        internal OligoWithSetMods(NucleicAcid nucleicAcid, int oneBaseStartResidueInNucleicAcid,
            int oneBasedEndResidueInNucleicAcid, int missedCleavages, CleavageSpecificity cleavageSpecificity) : base(
            nucleicAcid, oneBaseStartResidueInNucleicAcid, oneBasedEndResidueInNucleicAcid, missedCleavages,
            cleavageSpecificity)
        {

        }
        
    }
}
