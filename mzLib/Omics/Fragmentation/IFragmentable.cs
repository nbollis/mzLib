using Chemistry;
using MassSpectrometry;
using Omics.Modifications;
using System;
using System.Collections.Generic;
using System.Text;

namespace Omics.Fragmentation;

public interface IFragmentable : IHasMass, IHasChemicalFormula
{
    public Product DefaultMIon => new CustomMProduct(string.Empty, MonoisotopicMass);

    /// <summary>
    /// Generates theoretical fragments for given dissociation type for this peptide. 
    /// The "products" parameter is filled with these fragments.
    /// </summary>
    public void Fragment(DissociationType dissociationType, FragmentationTerminus fragmentationTerminus, List<Product> products, IFragmentationParams? fragmentationParams = null);

    /// <summary>
    /// Generates theoretical internal fragments for given dissociation type for this peptide. 
    /// <param name="dissociationType">The dissociation type to use for fragmentation</param>
    /// <param name="minLengthOfFragments">The minimum number of amino acids for an internal fragment to be included</param>
    /// <param name="products">The list to be filled with the generated fragments</param>
    /// <param name="fragmentationParams">Optional fragmentation parameters</param>
    /// </summary>
    public void FragmentInternally(DissociationType dissociationType, int minLengthOfFragments, List<Product> products, IFragmentationParams? fragmentationParams = null);

    /// <summary>
    /// Generates M ions with neutral losses encoded in the fragmentation parameters.
    /// </summary>
    public virtual IEnumerable<Product> GetMIonsWithNeturalLosses(IFragmentationParams fragmentationParams)
    {
        // Molecular ion with neutral losses
        foreach (var ionLoss in fragmentationParams.MIonLosses)
        {
            yield return new CustomMProduct(ionLoss.Annotation, MonoisotopicMass - ionLoss.MonoisotopicMass);
        }
    }

    /// <summary>
    /// Generates M ions with neutral losses encoded in the modifications where the mass is M- neutral loss.
    /// </summary>
    public virtual IEnumerable<Product> GetMIonsFromModifications(IEnumerable<Modification> modsToCheck, DissociationType dissociationType)
    {
        foreach (var mod in modsToCheck.Where(p => p.NeutralLosses != null))
        {
            // molecular ion minus neutral losses
            if (mod.NeutralLosses.TryGetValue(dissociationType, out var losses))
            {
                foreach (double neutralLoss in losses.Where(p => p != 0))
                {
                    if (neutralLoss != 0)
                    {
                        yield return new Product(ProductType.M, FragmentationTerminus.Both, MonoisotopicMass - neutralLoss, 0, 0, neutralLoss);
                    }
                }
            }

            if (mod.NeutralLosses.TryGetValue(DissociationType.AnyActivationType, out losses))
            {
                foreach (double neutralLoss in losses.Where(p => p != 0))
                {
                    if (neutralLoss != 0)
                    {
                        yield return new Product(ProductType.M, FragmentationTerminus.Both, MonoisotopicMass - neutralLoss, 0, 0, neutralLoss);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns any diagnostic ions annotated in the Modifications. 
    /// </summary>
    /// <remarks>
    /// Code is extracted verbatim from PeptideWithSetMods for omics generalization.
    /// </remarks>
    public virtual IEnumerable<Product> GetDiagnosticIonsFromModifications(IEnumerable<Modification> modsToCheck, DissociationType dissociationType, IFragmentationParams fragmentationParams)
    {
        // TODO: this code is memory-efficient but sort of CPU inefficient; it can be further optimized.
        // however, diagnostic ions are fairly rare so it's probably OK for now
        foreach (double diagnosticIon in modsToCheck
            .Where(p => p.DiagnosticIons != null)
            .SelectMany(p => p.DiagnosticIons.Where(v => v.Key == dissociationType || v.Key == DissociationType.AnyActivationType))
            .SelectMany(p => p.Value)
            .Distinct())
        {
            int diagnosticIonLabel = (int)Math.Round(diagnosticIon.ToMz(1, fragmentationParams.Polarity), 0);

            // the diagnostic ion is assumed to be annotated in the mod info as the *neutral mass* of the diagnostic ion, not the ionized species
            yield return new Product(ProductType.D, FragmentationTerminus.Both, diagnosticIon, diagnosticIonLabel, 0, 0);
        }
    }
}