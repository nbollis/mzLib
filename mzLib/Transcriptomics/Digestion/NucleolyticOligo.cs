using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Chemistry;
using MassSpectrometry;

namespace Transcriptomics
{
    /// <summary>
    /// The most basic form of a digested oligo, this class does not care about mass or formula, just base sequence
    /// </summary>
    public class NucleolyticOligo
    {
        internal NucleolyticOligo(NucleicAcid nucleicAcid, int oneBaseStartResidue,
            int oneBasedEndResidue, int missedCleavages, CleavageSpecificity cleavageSpecificity,
            IHasChemicalFormula? fivePrimeTerminus, IHasChemicalFormula? threePrimeTerminus)
        {
            NucleicAcid = nucleicAcid;
            OneBasedStartResidue = oneBaseStartResidue;
            OneBasedEndResidue = oneBasedEndResidue;
            MissedCleavages = missedCleavages;
            CleavageSpecificityForFdrCategory = cleavageSpecificity;
            _fivePrimeTerminus = fivePrimeTerminus ?? NucleicAcid.DefaultFivePrimeTerminus;
            _threePrimeTerminus = threePrimeTerminus ?? NucleicAcid.DefaultThreePrimeTerminus;
        }

        protected IHasChemicalFormula _fivePrimeTerminus;
        protected IHasChemicalFormula _threePrimeTerminus;

        [NonSerialized] private NucleicAcid _nucleicAcid;
        /// <summary>
        /// Nucleic acid this oligo was digested from
        /// </summary>
        public NucleicAcid NucleicAcid
        {
            get => _nucleicAcid;
            protected set => _nucleicAcid = value;
        }

        public int Length => BaseSequence.Length;

        private string _baseSequence;

        public string BaseSequence
        {
            get
            {
                return _baseSequence ??= NucleicAcid.BaseSequence.Substring(
                    OneBasedStartResidue - 1,
                    OneBasedEndResidue - OneBasedStartResidue + 1);
            }
        }

        /// <summary>
        /// Residue number at which the oligo begins 
        /// </summary>
        public int OneBasedStartResidue { get; init; }

        /// <summary>
        /// Residue number at which the oligo ends
        /// </summary>
        public int OneBasedEndResidue { get; init; }

        /// <summary>
        /// The number of missed cleavages this oligo has with respect to the digesting Rnase
        /// </summary>
        public int MissedCleavages { get; init; }

        /// <summary>
        /// Structured explanation of the source
        /// </summary>
        public CleavageSpecificity CleavageSpecificityForFdrCategory { get; set; }

        /// <summary>
        /// The one letter symbol of the nucleotide which precedes this fragment on the original Nucleic Acid
        /// </summary>
        public virtual char PreviousResidue => OneBasedStartResidue > 1
            ? NucleicAcid[OneBasedStartResidue - 2]
            : '-';

        /// <summary>
        /// The one letter symbol of the nucleotide which comes after this fragment on the original Nucleic Acid
        /// </summary>
        public virtual char NextResidue => OneBasedEndResidue < NucleicAcid.Length
            ? NucleicAcid[OneBasedEndResidue]
            : '-';

        public override string ToString()
        {
            return BaseSequence;
        }

        internal IEnumerable<OligoWithSetMods> GetModifiedOligos(IEnumerable<Modification> allKnownFixedMods,
            RnaDigestionParams digestionParams, List<Modification> variableModifications)
        {
            // TODO: Mods

            yield return new OligoWithSetMods(NucleicAcid, digestionParams, OneBasedStartResidue,
                OneBasedEndResidue, MissedCleavages, CleavageSpecificityForFdrCategory,
                new Dictionary<int, Modification>(), 0, _fivePrimeTerminus,
                _threePrimeTerminus);
        }
    }
}
