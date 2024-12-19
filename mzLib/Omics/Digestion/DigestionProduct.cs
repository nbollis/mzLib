﻿using MzLibUtil;
using Omics.Modifications;

namespace Omics.Digestion
{
    public abstract class DigestionProduct
    {
        protected string _baseSequence;
        protected static readonly DictionaryPool<int, Modification> ModDictionaryPool = new(32);
        protected static readonly DictionaryPool<int, List<Modification>> ModListDictionaryPool = new(32); 
        protected static readonly ListPool<Modification> ModListPool = new(4);

        protected DigestionProduct(IBioPolymer parent, int oneBasedStartResidue, int oneBasedEndResidue, int missedCleavages, 
            CleavageSpecificity cleavageSpecificityForFdrCategory, string? description = null, string? baseSequence = null)
        {
            Parent = parent;
            OneBasedStartResidue = oneBasedStartResidue;
            OneBasedEndResidue = oneBasedEndResidue;
            MissedCleavages = missedCleavages;
            CleavageSpecificityForFdrCategory = cleavageSpecificityForFdrCategory;
            Description = description;
            _baseSequence = baseSequence;
        }

        [field: NonSerialized] public IBioPolymer Parent { get; protected set; } // BioPolymer that this lysis product is a digestion product of
        public string Description { get; protected set; } //unstructured explanation of source
        public int OneBasedStartResidue { get; }// the residue number at which the peptide begins (the first residue in a protein is 1)
        public int OneBasedEndResidue { get; }// the residue number at which the peptide ends
        public int MissedCleavages { get; } // the number of missed cleavages this peptide has with respect to the digesting protease

        public virtual char PreviousResidue => Parent is null ? '-' : OneBasedStartResidue > 1 ? Parent[OneBasedStartResidue - 2] : '-';

        public virtual char NextResidue => Parent is null ? '-' : OneBasedEndResidue < Parent.Length ? Parent[OneBasedEndResidue] : '-';

        public string BaseSequence =>
            _baseSequence ??= Parent.BaseSequence.Substring(OneBasedStartResidue - 1,
                OneBasedEndResidue - OneBasedStartResidue + 1);
        public CleavageSpecificity CleavageSpecificityForFdrCategory { get; set; } //structured explanation of source
        public int Length => BaseSequence.Length; //how many residues long the peptide is
        public char this[int zeroBasedIndex] => BaseSequence[zeroBasedIndex];

        protected static IEnumerable<Dictionary<int, Modification>> GetVariableModificationPatterns(Dictionary<int, List<Modification>> possibleVariableModifications, int maxModsForPeptide, int peptideLength)
        {
            if (possibleVariableModifications.Count == 0)
            {
                yield return null;
            }
            else
            {
                int[] base_variable_modification_pattern = new int[peptideLength + 4];
                int totalAvailableMods = possibleVariableModifications.Values.Sum(mods => mods.Count);

                int maxVariableMods = Math.Min(totalAvailableMods, maxModsForPeptide);
                for (int variable_modifications = 0; variable_modifications <= maxVariableMods; variable_modifications++)
                {
                    foreach (int[] variable_modification_pattern in GetVariableModificationPatterns(possibleVariableModifications,
                                 possibleVariableModifications.Count - variable_modifications, base_variable_modification_pattern, 0))
                    {
                        yield return GetNewVariableModificationPattern(variable_modification_pattern, possibleVariableModifications);
                    }
                }
            }
        }

        protected List<KeyValuePair<int, Modification>> GetFixedModsOneIsNorFivePrimeTerminus(int length,
            List<Modification> allKnownFixedModifications)
        {
            var fixedModsOneIsNterminus = ModDictionaryPool.Get();
            try
            {
                foreach (Modification mod in allKnownFixedModifications)
                {
                    switch (mod.LocationRestriction)
                    {
                        case "5'-terminal.":
                        case "Oligo 5'-terminal.":
                        case "N-terminal.":
                        case "Peptide N-terminal.":
                            //the modification is protease associated and is applied to the n-terminal cleaved residue, not at the beginning of the protein
                            if (ModificationLocalization.ModFits(mod, Parent.BaseSequence, 1, length, OneBasedStartResidue))
                            {
                                if (mod.ModificationType == "Protease")
                                {
                                    if (OneBasedStartResidue != 1)
                                        fixedModsOneIsNterminus[2] = mod;
                                }
                                else //Normal N-terminal peptide modification
                                    fixedModsOneIsNterminus[1] = mod;
                            }
                            break;

                        case "Anywhere.":
                            for (int i = 2; i <= length + 1; i++)
                            {
                                if (ModificationLocalization.ModFits(mod, Parent.BaseSequence, i - 1, length, OneBasedStartResidue + i - 2))
                                {
                                    fixedModsOneIsNterminus[i] = mod;
                                }
                            }
                            break;

                        case "3'-terminal.":
                        case "Oligo 3'-terminal.":
                        case "C-terminal.":
                        case "Peptide C-terminal.":
                            //the modification is protease associated and is applied to the c-terminal cleaved residue, not if it is at the end of the protein
                            if (ModificationLocalization.ModFits(mod, Parent.BaseSequence, length, length, OneBasedStartResidue + length - 1))
                            {
                                if (mod.ModificationType == "Protease")
                                {
                                    if (OneBasedEndResidue != Parent.Length)
                                        fixedModsOneIsNterminus[length + 1] = mod;
                                }
                                else //Normal C-terminal peptide modification 
                                    fixedModsOneIsNterminus[length + 2] = mod;
                            }
                            break;

                        default:
                            throw new NotSupportedException("This terminus localization is not supported.");
                    }
                }

                return fixedModsOneIsNterminus.ToList();
            }
            finally
            {
                ModDictionaryPool.Return(fixedModsOneIsNterminus);
            }
        }


        private static IEnumerable<int[]> GetVariableModificationPatterns(Dictionary<int, List<Modification>> possibleVariableModifications,
            int unmodifiedResiduesDesired, int[] variableModificationPattern, int index)
        {
            var keys = possibleVariableModifications.Keys.ToList();
            if (index < keys.Count - 1)
            {
                if (unmodifiedResiduesDesired > 0)
                {
                    variableModificationPattern[keys[index]] = 0;
                    foreach (int[] new_variable_modification_pattern in GetVariableModificationPatterns(possibleVariableModifications, unmodifiedResiduesDesired - 1, variableModificationPattern, index + 1))
                    {
                        yield return new_variable_modification_pattern;
                    }
                }
                if (unmodifiedResiduesDesired < keys.Count - index)
                {
                    for (int i = 1; i <= possibleVariableModifications[keys[index]].Count; i++)
                    {
                        variableModificationPattern[keys[index]] = i;
                        foreach (int[] new_variable_modification_pattern in GetVariableModificationPatterns(possibleVariableModifications, unmodifiedResiduesDesired, variableModificationPattern, index + 1))
                        {
                            yield return new_variable_modification_pattern;
                        }
                    }
                }
            }
            else
            {
                if (unmodifiedResiduesDesired > 0)
                {
                    variableModificationPattern[keys[index]] = 0;
                    yield return variableModificationPattern;
                }
                else
                {
                    for (int i = 1; i <= possibleVariableModifications[keys[index]].Count; i++)
                    {
                        variableModificationPattern[keys[index]] = i;
                        yield return variableModificationPattern;
                    }
                }
            }
        }

        private static Dictionary<int, Modification> GetNewVariableModificationPattern(int[] variableModificationArray,
            IEnumerable<KeyValuePair<int, List<Modification>>> possibleVariableModifications)
        {
            var modification_pattern = new Dictionary<int, Modification>();

            foreach (var kvp in possibleVariableModifications)
            {
                if (variableModificationArray[kvp.Key] > 0)
                {
                    modification_pattern.Add(kvp.Key, kvp.Value[variableModificationArray[kvp.Key] - 1]);
                }
            }

            return modification_pattern;
        }
    }
}
