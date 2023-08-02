using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;

namespace Transcriptomics
{
    public class NucleolyticOligo
    {
        internal NucleolyticOligo(NucleicAcid nucleicAcid, int oneBaseStartResidueInNucleicAcid,
            int oneBasedEndResidueInNucleicAcid, int missedCleavages, CleavageSpecificity cleavageSpecificity)
        {
            NucleicAcid = nucleicAcid;
            OneBasedStartResidueInNucleicAcid = oneBaseStartResidueInNucleicAcid;
            OneBasedEndResidueInNucleicAcid = oneBasedEndResidueInNucleicAcid;
            MissedCleavages = missedCleavages;
            CleavesSpecificityForFdrCategory = cleavageSpecificity;
        }

        [NonSerialized] private NucleicAcid _nucleicAcid;
        /// <summary>
        /// Nucleic acid this oligo was digested from
        /// </summary>
        public NucleicAcid NucleicAcid
        {
            get => _nucleicAcid;
            protected set => _nucleicAcid = value;
        }

        private string _baseSequence;

        public string BaseSequence
        {
            get
            {
                return _baseSequence ??= NucleicAcid.BaseSequence.Substring(
                    OneBasedStartResidueInNucleicAcid - 1,
                    OneBasedEndResidueInNucleicAcid - OneBasedStartResidueInNucleicAcid + 1);
            }
        }


        /// <summary>
        /// Residue number at which the oligo begins 
        /// </summary>
        public int OneBasedStartResidueInNucleicAcid { get; init; }

        /// <summary>
        /// Residue number at which the oligo ends
        /// </summary>
        public int OneBasedEndResidueInNucleicAcid { get; init; }

        /// <summary>
        /// The number of missed cleavages this oligo has with respect to the digesting Rnase
        /// </summary>
        public int MissedCleavages { get; init; }

        /// <summary>
        /// Structured explanation of the source
        /// </summary>
        public CleavageSpecificity CleavesSpecificityForFdrCategory { get; init; }

        /// <summary>
        /// The one letter symbol of the nucleotide which precedes this fragment on the original Nucleic Acid
        /// </summary>
        public virtual char PreviousNucleicAcid => OneBasedStartResidueInNucleicAcid > 1
            ? NucleicAcid[OneBasedStartResidueInNucleicAcid - 2]
            : '-';

        /// <summary>
        /// The one letter symbol of the nucleotide which comes after this fragment on the original Nucleic Acid
        /// </summary>
        public virtual char NextNucleicAcid => OneBasedEndResidueInNucleicAcid < NucleicAcid.Length
            ? NucleicAcid[OneBasedEndResidueInNucleicAcid]
            : '-';

        public override string ToString()
        {
            return BaseSequence;
        }
    }
}
