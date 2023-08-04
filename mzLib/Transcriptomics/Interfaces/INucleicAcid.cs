using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;

namespace Transcriptomics
{
    public interface INucleicAcid : IHasChemicalFormula
    {
        /// <summary>
        /// The amino acid sequence
        /// </summary>
        string BaseSequence { get; }

        /// <summary>
        /// The length of the amino acid sequence
        /// </summary>
        int Length { get; }

        IHasChemicalFormula FivePrimeTerminus { get; set; }

        IHasChemicalFormula ThreePrimeTerminus { get; set; }
        
    }
}
