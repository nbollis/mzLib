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
    public class NucleolyticOligo : LysisProduct
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
            // TODO: Deal with alternative termini
            int oligoLength = OneBasedEndResidue - OneBasedStartResidue + 1;
            int maximumVariableModificationIsoforms = digestionParams.MaxModificationIsoforms;
            int maxModsForOligo = digestionParams.MaxMods;
            var twoBasedPossibleVariableAndLocalizeableModifications = new Dictionary<int, List<Modification>>(oligoLength + 4);

            var pepNTermVariableMods = new List<Modification>();
            twoBasedPossibleVariableAndLocalizeableModifications.Add(1, pepNTermVariableMods);

            var pepCTermVariableMods = new List<Modification>();
            twoBasedPossibleVariableAndLocalizeableModifications.Add(oligoLength + 2, pepCTermVariableMods);

            foreach (Modification variableModification in variableModifications)
            {
                // Check if can be a n-term mod
                if (CanBeFivePrime(variableModification, oligoLength)/* && !ModificationLocalization.UniprotModExists(NucleicAcid, 1, variableModification)*/)
                {
                    pepNTermVariableMods.Add(variableModification);
                }

                for (int r = 0; r < oligoLength; r++)
                {
                    if (ModFits(variableModification, NucleicAcid.BaseSequence, r + 1, oligoLength, OneBasedStartResidue + r)
                        && variableModification.LocationRestriction == "Anywhere." /*&& !ModificationLocalization.UniprotModExists(NucleicAcid, r + 1, variableModification)*/)
                    {
                        if (!twoBasedPossibleVariableAndLocalizeableModifications.TryGetValue(r + 2, out List<Modification> residueVariableMods))
                        {
                            residueVariableMods = new List<Modification> { variableModification };
                            twoBasedPossibleVariableAndLocalizeableModifications.Add(r + 2, residueVariableMods);
                        }
                        else
                        {
                            residueVariableMods.Add(variableModification);
                        }
                    }
                }
                // Check if can be a c-term mod
                if (CanBeThreePrime(variableModification, oligoLength) /*&& !ModificationLocalization.UniprotModExists(NucleicAcid, oligoLength, variableModification)*/)
                {
                    pepCTermVariableMods.Add(variableModification);
                }
            }

            // LOCALIZED MODS
            foreach (var kvp in NucleicAcid.OneBasedPossibleLocalizedModifications)
            {
                bool inBounds = kvp.Key >= OneBasedStartResidue && kvp.Key <= OneBasedEndResidue;
                if (!inBounds)
                {
                    continue;
                }

                int locInPeptide = kvp.Key - OneBasedStartResidue + 1;
                foreach (Modification modWithMass in kvp.Value)
                {
                    if (modWithMass is Modification variableModification)
                    {
                        // Check if can be a n-term mod
                        if (locInPeptide == 1 && CanBeFivePrime(variableModification, oligoLength) && !NucleicAcid.IsDecoy)
                        {
                            pepNTermVariableMods.Add(variableModification);
                        }

                        int r = locInPeptide - 1;
                        if (r >= 0 && r < oligoLength
                            && (NucleicAcid.IsDecoy ||
                            (ModFits(variableModification, NucleicAcid.BaseSequence, r + 1, oligoLength, OneBasedStartResidue + r)
                                && variableModification.LocationRestriction == "Anywhere.")))
                        {
                            if (!twoBasedPossibleVariableAndLocalizeableModifications.TryGetValue(r + 2, out List<Modification> residueVariableMods))
                            {
                                residueVariableMods = new List<Modification> { variableModification };
                                twoBasedPossibleVariableAndLocalizeableModifications.Add(r + 2, residueVariableMods);
                            }
                            else
                            {
                                residueVariableMods.Add(variableModification);
                            }
                        }

                        // Check if can be a c-term mod
                        if (locInPeptide == oligoLength && CanBeThreePrime(variableModification, oligoLength) && !NucleicAcid.IsDecoy)
                        {
                            pepCTermVariableMods.Add(variableModification);
                        }
                    }
                }
            }

            int variable_modification_isoforms = 0;

            foreach (Dictionary<int, Modification> kvp in GetVariableModificationPatterns(twoBasedPossibleVariableAndLocalizeableModifications, maxModsForOligo, oligoLength))
            {
                int numFixedMods = 0;
                foreach (var ok in GetFixedModsOneIsFivePrime(oligoLength, allKnownFixedMods))
                {
                    if (!kvp.ContainsKey(ok.Key))
                    {
                        numFixedMods++;
                        kvp.Add(ok.Key, ok.Value);
                    }
                }
                yield return new OligoWithSetMods(NucleicAcid, digestionParams, OneBasedStartResidue, OneBasedEndResidue, MissedCleavages,
                    CleavageSpecificityForFdrCategory, kvp, numFixedMods, _fivePrimeTerminus, _threePrimeTerminus);
                variable_modification_isoforms++;
                if (variable_modification_isoforms == maximumVariableModificationIsoforms)
                {
                    yield break;
                }
            }

            //yield return new OligoWithSetMods(NucleicAcid, digestionParams, OneBasedStartResidue,
            //    OneBasedEndResidue, MissedCleavages, CleavageSpecificityForFdrCategory,
            //    new Dictionary<int, Modification>(), 0, _fivePrimeTerminus,
            //    _threePrimeTerminus);
        }

        #region Digestion

        internal static bool ModFits(Modification attemptToLocalize, string nucleicAcidSequence, int oligoOneBasedIndex,
            int oligoLength, int nucleicAcidOneBasedIndex)
        {
            // First find the capital letter...
            var motif = attemptToLocalize.Target;
            var motifStartLocation = motif.ToString().IndexOf(motif.ToString().First(b => char.IsUpper(b)));

            // Look up starting at and including the capital letter
            var proteinToMotifOffset = oligoOneBasedIndex - motifStartLocation - 1;
            var indexUp = 0;
            while (indexUp < motif.ToString().Length)
            {
                if (indexUp + proteinToMotifOffset < 0 || indexUp + proteinToMotifOffset >= nucleicAcidSequence.Length
                                                       || !MotifMatches(motif.ToString()[indexUp], nucleicAcidSequence[indexUp + proteinToMotifOffset]))
                {
                    return false;
                }
                indexUp++;
            }

            switch (attemptToLocalize.LocationRestriction)
            {
                case "FivePrime." when nucleicAcidOneBasedIndex > 2:
                case "OligoFivePrime." when oligoOneBasedIndex > 1:
                case "ThreePrime." when nucleicAcidOneBasedIndex < nucleicAcidSequence.Length:
                case "OligoThreePrime." when oligoOneBasedIndex < oligoLength:
                    return false;
                default:
                    // I guess Anywhere. and Unassigned. are true since how do you localize anywhere or unassigned.

                    return true;
            }
        }

        private static bool MotifMatches(char motifChar, char sequenceChar)
        {
            char upperMotifChar = char.ToUpper(motifChar);
            return upperMotifChar.Equals('X')
                   || upperMotifChar.Equals(sequenceChar);
        }

        private bool CanBeFivePrime(Modification variableModification, int peptideLength)
        {
            return ModFits(variableModification, NucleicAcid.BaseSequence, 1, peptideLength, OneBasedStartResidue)
                   && (variableModification.LocationRestriction == "FivePrime." || variableModification.LocationRestriction == "OligoFivePrime.");
        }

        private bool CanBeThreePrime(Modification variableModification, int peptideLength)
        {
            return ModFits(variableModification, NucleicAcid.BaseSequence, peptideLength, peptideLength, OneBasedStartResidue + peptideLength - 1)
                   && (variableModification.LocationRestriction == "ThreePrime." || variableModification.LocationRestriction == "OligoThreePrime.");
        }

        private Dictionary<int, Modification> GetFixedModsOneIsFivePrime(int oligoLength,
           IEnumerable<Modification> allKnownFixedModifications)
        {
            var fixedModsOneIsNterminus = new Dictionary<int, Modification>(oligoLength + 3);
            foreach (Modification mod in allKnownFixedModifications)
            {
                switch (mod.LocationRestriction)
                {
                    case "FivePrime.":
                    case "OligoFivePrime.":
                        //the modification is protease associated and is applied to the n-terminal cleaved residue, not at the beginign of the protein
                        if (mod.ModificationType == "Protease" && ModFits(mod, NucleicAcid.BaseSequence, 1, oligoLength, OneBasedStartResidue))
                        {
                            if (OneBasedStartResidue != 1)
                            {
                                fixedModsOneIsNterminus[2] = mod;
                            }
                        }
                        //Normal N-terminal peptide modification
                        else if (ModFits(mod, NucleicAcid.BaseSequence, 1, oligoLength, OneBasedStartResidue))
                        {
                            fixedModsOneIsNterminus[1] = mod;
                        }
                        break;

                    case "Anywhere.":
                        for (int i = 2; i <= oligoLength + 1; i++)
                        {
                            if (ModFits(mod, NucleicAcid.BaseSequence, i - 1, oligoLength, OneBasedStartResidue + i - 2))
                            {
                                fixedModsOneIsNterminus[i] = mod;
                            }
                        }
                        break;

                    case "ThreePrime.":
                    case "OligoThreePrime.":
                        //the modification is protease associated and is applied to the c-terminal cleaved residue, not if it is at the end of the protein
                        if (mod.ModificationType == "Protease" && ModFits(mod, NucleicAcid.BaseSequence, oligoLength, oligoLength, OneBasedStartResidue + oligoLength - 1))
                        {
                            if (OneBasedEndResidue != NucleicAcid.Length)
                            {
                                fixedModsOneIsNterminus[oligoLength + 1] = mod;
                            }

                        }
                        //Normal C-terminal peptide modification 
                        else if (ModFits(mod, NucleicAcid.BaseSequence, oligoLength, oligoLength, OneBasedStartResidue + oligoLength - 1))
                        {
                            fixedModsOneIsNterminus[oligoLength + 2] = mod;
                        }
                        break;

                    default:
                        throw new NotSupportedException("This terminus localization is not supported.");
                }
            }
            return fixedModsOneIsNterminus;
        }

        #endregion


    }
}
