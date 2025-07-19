using System.Text;
using Chemistry;
using Omics.BioPolymer;
using Omics.Digestion;
using Omics.Modifications;

namespace Omics;

public static class BioPolymerWithSetModsExtensions
{
    /// <summary>
    /// This method returns the full sequence with mass shifts INSTEAD OF PTMs in brackets []
    /// Some external tools cannot parse PTMs, instead requiring a numerical input indicating the mass of a PTM in brackets
    /// after the position of that modification
    /// N-terminal mas shifts are in brackets prior to the first amino acid and apparently missing the + sign
    /// </summary>
    /// <returns></returns>
    public static string FullSequenceWithMassShift(this IBioPolymerWithSetMods withSetMods)
    {
        var subsequence = new StringBuilder();

        // modification on peptide N-terminus
        if (withSetMods.AllModsOneIsNterminus.TryGetValue(1, out Modification? mod))
        {
            subsequence.Append($"[{mod.MonoisotopicMass.RoundedDouble(6)}]");
        }

        for (int r = 0; r < withSetMods.Length; r++)
        {
            subsequence.Append(withSetMods[r]);

            // modification on this residue
            if (withSetMods.AllModsOneIsNterminus.TryGetValue(r + 2, out mod))
            {
                if (mod.MonoisotopicMass > 0)
                {
                    subsequence.Append($"[+{mod.MonoisotopicMass.RoundedDouble(6)}]");
                }
                else
                {
                    subsequence.Append($"[{mod.MonoisotopicMass.RoundedDouble(6)}]");
                }
            }
        }

        // modification on peptide C-terminus
        if (withSetMods.AllModsOneIsNterminus.TryGetValue(withSetMods.Length + 2, out mod))
        {
            if (mod.MonoisotopicMass > 0)
            {
                subsequence.Append($"[+{mod.MonoisotopicMass.RoundedDouble(6)}]");
            }
            else
            {
                subsequence.Append($"[{mod.MonoisotopicMass.RoundedDouble(6)}]");
            }
        }
        return subsequence.ToString();
    }

    /// <summary>
    /// This method returns the full sequence only with the specified modifications in the modstoWritePruned dictionary
    /// </summary>
    /// <param name="withSetMods"></param>
    /// <param name="modstoWritePruned"></param>
    /// <returns></returns>
    public static string EssentialSequence(this IBioPolymerWithSetMods withSetMods,
        IReadOnlyDictionary<string, int> modstoWritePruned)
    {
        string essentialSequence = withSetMods.BaseSequence;
        if (modstoWritePruned != null)
        {
            var sbsequence = new StringBuilder(withSetMods.FullSequence.Length);

            // variable modification on peptide N-terminus
            if (withSetMods.AllModsOneIsNterminus.TryGetValue(1, out Modification pep_n_term_variable_mod))
            {
                if (modstoWritePruned.ContainsKey(pep_n_term_variable_mod.ModificationType))
                {
                    sbsequence.Append(
                        $"[{pep_n_term_variable_mod.ModificationType}:{pep_n_term_variable_mod.IdWithMotif}]");
                }
            }
            for (int r = 0; r < withSetMods.Length; r++)
            {
                sbsequence.Append(withSetMods[r]);
                // variable modification on this residue
                if (withSetMods.AllModsOneIsNterminus.TryGetValue(r + 2, out Modification residue_variable_mod))
                {
                    if (modstoWritePruned.ContainsKey(residue_variable_mod.ModificationType))
                    {
                        sbsequence.Append(
                            $"[{residue_variable_mod.ModificationType}:{residue_variable_mod.IdWithMotif}]");
                    }
                }
            }

            // variable modification on peptide C-terminus
            if (withSetMods.AllModsOneIsNterminus.TryGetValue(withSetMods.Length + 2, out Modification pep_c_term_variable_mod))
            {
                if (modstoWritePruned.ContainsKey(pep_c_term_variable_mod.ModificationType))
                {
                    sbsequence.Append(
                        $"[{pep_c_term_variable_mod.ModificationType}:{pep_c_term_variable_mod.IdWithMotif}]");
                }
            }

            essentialSequence = sbsequence.ToString();
        }
        return essentialSequence;
    }

    public static string DetermineFullSequence(this IBioPolymerWithSetMods withSetMods) => IBioPolymerWithSetMods
        .DetermineFullSequence(withSetMods.BaseSequence, withSetMods.AllModsOneIsNterminus);

    #region Variants 
    /// <summary>
    /// Takes an individual peptideWithSetModifications and determines if applied variations from the protein are found within its length
    /// </summary>
    public static bool IsVariant(this IBioPolymerWithSetMods withSetMods)
    {
        bool identifiedVariant = false;
        if (withSetMods.Parent.AppliedSequenceVariations.Any())
        {
            foreach (var variant in withSetMods.Parent.AppliedSequenceVariations)
            {
                if (withSetMods.IntersectsAndIdentifiesVariation(variant).identifies)
                {
                    identifiedVariant = true;
                    break;
                }
            }
        }
        return identifiedVariant;
    }

    /// <summary>
    /// Checks if sequence variant and peptide intersect, also checks if the seuqence variatn can be identified whether they intersect
    /// or not (ie if the variant causes a cleavage site generating the peptide). Returns a tuple with item 1 being a bool value
    /// representing if the varaint intersects the peptide and item 2 beign abool that represents if the variatn is identified.
    /// </summary>
    public static (bool intersects, bool identifies) IntersectsAndIdentifiesVariation(this IBioPolymerWithSetMods withSetMods, SequenceVariation appliedVariation)
    {
        // Determine possible locations for variant start site
        bool variantStartsBeforePeptide = appliedVariation.OneBasedBeginPosition < withSetMods.OneBasedStartResidue;
        bool variantStartsAtPeptideStart = appliedVariation.OneBasedBeginPosition == withSetMods.OneBasedStartResidue;
        bool variantStartsInsidePeptide = appliedVariation.OneBasedBeginPosition >= withSetMods.OneBasedStartResidue && appliedVariation.OneBasedBeginPosition < withSetMods.OneBasedEndResidue;
        bool variantStartsAtPeptideEnd = appliedVariation.OneBasedBeginPosition == withSetMods.OneBasedEndResidue;
        // Determine possible locations for variant end site
        bool variantEndsAtPeptideStart = appliedVariation.OneBasedEndPosition == withSetMods.OneBasedStartResidue;
        bool variantEndsInsidePeptide = appliedVariation.OneBasedEndPosition > withSetMods.OneBasedStartResidue && appliedVariation.OneBasedEndPosition <= withSetMods.OneBasedEndResidue;
        bool variantEndsAtPeptideEnd = appliedVariation.OneBasedEndPosition == withSetMods.OneBasedEndResidue;
        bool variantEndsAfterPeptide = appliedVariation.OneBasedEndPosition > withSetMods.OneBasedEndResidue;

        bool intersects = false;
        bool identifies = false;
        // Start and end combinations that lead to variants being intersected by the peptide sequence
        if (variantStartsBeforePeptide || variantStartsAtPeptideStart)
        {
            if (variantEndsAtPeptideStart || variantEndsInsidePeptide || variantEndsAtPeptideEnd || variantEndsAfterPeptide)
            {
                intersects = true;
            }
        }
        else if (variantStartsInsidePeptide)
        {
            if (variantEndsInsidePeptide || variantEndsAfterPeptide || variantEndsAtPeptideEnd)
            {
                intersects = true;
            }
        }
        else if (variantStartsAtPeptideEnd)
        {
            if (variantEndsAfterPeptide || variantEndsAtPeptideEnd)
            {
                intersects = true;
            }
        }

        if (intersects)
        {
            int lengthDifference = appliedVariation.VariantSequence.Length - appliedVariation.OriginalSequence.Length;
            int intersectOneBasedStart = Math.Max(withSetMods.OneBasedStartResidue, appliedVariation.OneBasedBeginPosition);
            int intersectOneBasedEnd = Math.Min(withSetMods.OneBasedEndResidue, appliedVariation.OneBasedEndPosition + lengthDifference);
            int intersectSize = intersectOneBasedEnd - intersectOneBasedStart + 1;

            // If the original sequence within the peptide is shorter or longer than the variant sequence within the peptide, there is a sequence change
            int variantZeroBasedStartInPeptide = intersectOneBasedStart - appliedVariation.OneBasedBeginPosition;
            bool originalSequenceIsShort = appliedVariation.OriginalSequence.Length - variantZeroBasedStartInPeptide < intersectSize;
            bool originalSequenceIsLong = appliedVariation.OriginalSequence.Length > intersectSize && withSetMods.OneBasedEndResidue > intersectOneBasedEnd;
            if (originalSequenceIsShort || originalSequenceIsLong)
            {
                identifies = true;
            }
            else
            {
                // Crosses the entire variant sequence (needed to identify truncations and certain deletions, like KAAAAAAAAA -> K, but also catches synonymous variations A -> A)
                bool crossesEntireVariant = intersectSize == appliedVariation.VariantSequence.Length;

                if (crossesEntireVariant)
                {
                    // Is the variant sequence intersecting the peptide different than the original sequence?
                    string originalAtIntersect = appliedVariation.OriginalSequence.Substring(intersectOneBasedStart - appliedVariation.OneBasedBeginPosition, intersectSize);
                    string variantAtIntersect = appliedVariation.VariantSequence.Substring(intersectOneBasedStart - appliedVariation.OneBasedBeginPosition, intersectSize);
                    identifies = originalAtIntersect != variantAtIntersect;
                }
            }
        }
        // Checks to see if the variant causes a cleavage event creating the peptide. This is how a variant can be identified without intersecting with the peptide itself
        else
        {
            // Account for any variants that occur in the protein prior to the variant in question.
            // This information is used to calculate a scaling factor to calculate the AA that precedes the peptide sequence in the original (variant free) withSetMods.Parent
            List<SequenceVariation> variantsThatAffectPreviousResiduePosition = withSetMods.Parent.AppliedSequenceVariations
                .Where(v => v.OneBasedEndPosition <= withSetMods.OneBasedStartResidue).ToList();
            int totalLengthDifference = 0;
            foreach (var variant in variantsThatAffectPreviousResiduePosition)
            {
                totalLengthDifference += variant.VariantSequence.Length - variant.OriginalSequence.Length;
            }

            // Determine what the cleavage sites are for the protease used (will allow us to determine if new cleavage sites were made by variant)
            List<DigestionMotif> proteaseCleavageSites = withSetMods.DigestionParams.DigestionAgent.DigestionMotifs;
            // If the variant ends the AA before the peptide starts then it may have caused C-terminal cleavage
            // See if the protease used for digestion has C-terminal cleavage sites
            List<string> cTerminalResidues = proteaseCleavageSites.Where(dm => dm.CutIndex == 1).Select(d => d.InducingCleavage).ToList();

            if (appliedVariation.OneBasedEndPosition == (withSetMods.OneBasedStartResidue - 1))
            {
                if (cTerminalResidues.Count > 0)
                {
                    // Get the AA that precedes the peptide from the variant withSetMods.Parent (AKA the last AA in the variant)
                    var previousResidueVariant = new DigestionProduct(withSetMods.Parent, withSetMods.OneBasedStartResidue - 1, withSetMods.OneBasedStartResidue - 1, 0, CleavageSpecificity.Full, "full");

                    // Get the AA that precedes the peptide sequence in the original withSetMods.Parent (without any applied variants)
                    var previousResidueOriginal = new DigestionProduct(withSetMods.Parent.ConsensusVariant, (withSetMods.OneBasedStartResidue - 1) - totalLengthDifference, (withSetMods.OneBasedStartResidue - 1) - totalLengthDifference, 0, CleavageSpecificity.Full, "full");

                    bool newSite = cTerminalResidues.Contains(previousResidueVariant.BaseSequence);
                    bool oldSite = cTerminalResidues.Contains(previousResidueOriginal.BaseSequence);
                    // If the new AA causes a cleavage event, and that cleavage event would not have occurred without the variant then it is identified
                    if (newSite && !oldSite)
                    {
                        identifies = true;
                    }
                }
            }
            // If the variant begins the AA after the peptide ends then it may have caused N-terminal cleavage
            else if (appliedVariation.OneBasedBeginPosition == (withSetMods.OneBasedEndResidue + 1))
            {
                // See if the protease used for digestion has N-terminal cleavage sites
                List<string> nTerminalResidues = proteaseCleavageSites.Where(dm => dm.CutIndex == 0).Select(d => d.InducingCleavage).ToList();
                // Stop gain variation can create a peptide; this checks for this with C-terminal cleavage proteases
                if (cTerminalResidues.Count > 0)
                {
                    if (appliedVariation.VariantSequence == "*")
                    {
                        var lastResidueOfPeptide = new DigestionProduct(withSetMods.Parent, withSetMods.OneBasedEndResidue, withSetMods.OneBasedEndResidue, 0, CleavageSpecificity.Full, "full");

                        bool oldSite = cTerminalResidues.Contains(lastResidueOfPeptide.BaseSequence);
                        if (!oldSite)
                        {
                            identifies = true;
                        }
                    }
                }

                if (nTerminalResidues.Count > 0)
                {
                    if (withSetMods.Parent.Length >= withSetMods.OneBasedEndResidue + 1)
                    {
                        // Get the AA that follows the peptide sequence from the variant withSetMods.Parent (AKA the first AA of the variant)
                        var nextResidueVariant = new DigestionProduct(withSetMods.Parent, withSetMods.OneBasedEndResidue + 1, withSetMods.OneBasedEndResidue + 1, 0, CleavageSpecificity.Full, "full");

                        // Checks to make sure the original withSetMods.Parent has an amino acid following the peptide (an issue with stop loss variants or variants that add AA after the previous stop residue)
                        // No else statement because if the peptide end residue was the previous withSetMods.Parent stop site, there is no way to truly identify the variant. 
                        // If the peptide were to extend into the stop loss region then the peptide would intersect the variant and this code block would not be triggered.
                        if (withSetMods.Parent.ConsensusVariant.Length >= withSetMods.OneBasedEndResidue + 1)
                        {
                            // Get the AA that follows the peptide sequence in the original withSetMods.Parent (without any applied variants)
                            var nextResidueOriginal = new DigestionProduct(withSetMods.Parent.ConsensusVariant, (withSetMods.OneBasedEndResidue + 1) - totalLengthDifference, (withSetMods.OneBasedEndResidue + 1) - totalLengthDifference, 0, CleavageSpecificity.Full, "full");

                            bool newSite = nTerminalResidues.Contains(nextResidueVariant.BaseSequence);
                            bool oldSite = nTerminalResidues.Contains(nextResidueOriginal.BaseSequence);
                            // If the new AA causes a cleavage event, and that cleavage event would not have occurred without the variant then it is identified
                            if (newSite && !oldSite)
                            {
                                identifies = true;
                            }
                        }
                    }
                    // For stop gain variations that cause peptide
                    else
                    {
                        // Get the AA that follows the peptide sequence in the original withSetMods.Parent (without any applied variants)
                        var nextResidueOriginal = new DigestionProduct(withSetMods.Parent.ConsensusVariant, (withSetMods.OneBasedEndResidue + 1) - totalLengthDifference, (withSetMods.OneBasedEndResidue + 1) - totalLengthDifference, 0, CleavageSpecificity.Full, "full");

                        bool oldSite = nTerminalResidues.Contains(nextResidueOriginal.BaseSequence);
                        // If the new AA causes a cleavage event, and that cleavage event would not have occurred without the variant then it is identified
                        if (!oldSite)
                        {
                            identifies = true;
                        }
                    }
                }
            }
        }

        return (intersects, identifies);
    }

    /// <summary>
    /// Makes the string representing a detected sequence variation, including any modifications on a variant amino acid.
    /// takes in the variant as well as the bool value of wheter the peptid eintersects the variant. (this allows for identified
    /// variants that cause the cleavage site for the peptide.
    /// </summary>
    public static string SequenceVariantString(this IBioPolymerWithSetMods withSetMods, SequenceVariation applied, bool intersects)
    {
        if (intersects == true)
        {
            bool startAtNTerm = applied.OneBasedBeginPosition == 1 && withSetMods.OneBasedStartResidue == 1;
            bool onlyPeptideStartAtNTerm = withSetMods.OneBasedStartResidue == 1 && applied.OneBasedBeginPosition != 1;

            int modResidueScale = startAtNTerm switch
            {
                true => 1,
                _ => onlyPeptideStartAtNTerm switch
                {
                    true => 2,
                    _ => 3
                }
            };

            var modsOnVariantOneIsNTerm = withSetMods.AllModsOneIsNterminus
                .Where(kv => kv.Key == 1 && applied.OneBasedBeginPosition == 1 || applied.OneBasedBeginPosition <= kv.Key - 2 + withSetMods.OneBasedStartResidue && kv.Key - 2 + withSetMods.OneBasedStartResidue <= applied.OneBasedEndPosition)
                .ToDictionary(kv => kv.Key - applied.OneBasedBeginPosition + (modResidueScale), kv => kv.Value);

            var startPosition = applied.OneBasedBeginPosition == 1 ? applied.OneBasedBeginPosition : applied.OneBasedBeginPosition - 1;
            
            var variantWithAnyMods = new DigestionProduct(withSetMods.Parent, startPosition, applied.OneBasedEndPosition, 0, withSetMods.CleavageSpecificityForFdrCategory, withSetMods.Description);
            var fullSequence = IBioPolymerWithSetMods.DetermineFullSequence(variantWithAnyMods.BaseSequence, modsOnVariantOneIsNTerm);
            return $"{applied.OriginalSequence}{applied.OneBasedBeginPosition}{fullSequence.Substring(applied.OneBasedBeginPosition == 1 ? 0 : 1)}";
        }
        //if the variant caused a cleavage site leading the the peptide sequence (variant does not intersect but is identified)
        else
        {
            return $"{applied.OriginalSequence}{applied.OneBasedBeginPosition}{applied.VariantSequence}";
        }
    }

    #endregion
}