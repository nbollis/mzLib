using Chemistry;
using MassSpectrometry;
using MzLibUtil;
using Omics.Modifications;

namespace Omics.Fragmentation;

public interface IFragmentable : IHasMass, IHasChemicalFormula
{
    public Product DefaultMIon => new CustomMProduct(string.Empty, MonoisotopicMass);
    public virtual IFragmentationParams DefaultFragmentationParams => FragmentationParams.Default;

    public void GetBackboneFragments(IFragmentationParams fragmentationParameters, ref List<Product> products);

    /// <summary>
    /// Generates theoretical fragments for given dissociation type for this peptide. 
    /// The "products" parameter is filled with these fragments.
    /// </summary>
    public virtual void Fragment(IFragmentationParams fragmentationParameters, ref List<Product> products)
    {
        products.Clear();

        GetBackboneFragments(fragmentationParameters, ref products);

        // Create M ions from neutral loss behavior encoded in the fragmentation parameters (represents unfragmented or barley fragmented ions)
        if (fragmentationParameters.GenerateMIon)
        {
            products.Add(DefaultMIon);

            foreach (var ionLoss in fragmentationParameters.MIonLosses)
            {
                products.Add(new CustomMProduct(ionLoss.Annotation, MonoisotopicMass - ionLoss.MonoisotopicMass));
            }
        }

        IBioPolymerWithSetMods? bpwsm = this as IBioPolymerWithSetMods;

        // Create M ions minus the neutral loss of any mod. Previously Protein Only (represents a labile mod M-ion such as M-Phospho). 
        if (bpwsm != null && fragmentationParameters is FragmentationParams)
        {
            foreach (var mod in bpwsm.AllModsOneIsNterminus.Values.Where(p => p.NeutralLosses != null))
            {
                // molecular ion minus neutral losses
                if (mod.NeutralLosses.TryGetValue(fragmentationParameters.DissociationType, out var losses))
                {
                    foreach (double neutralLoss in losses.Where(p => p != 0))
                    {
                        if (neutralLoss != 0)
                        {
                            products.Add(new Product(ProductType.M, FragmentationTerminus.Both, MonoisotopicMass - neutralLoss, 0, 0, neutralLoss));
                        }
                    }
                }

                if (mod.NeutralLosses.TryGetValue(DissociationType.AnyActivationType, out losses))
                {
                    foreach (double neutralLoss in losses.Where(p => p != 0))
                    {
                        if (neutralLoss != 0)
                        {
                            products.Add(new Product(ProductType.M, FragmentationTerminus.Both, MonoisotopicMass - neutralLoss, 0, 0, neutralLoss));
                        }
                    }
                }
            }
        }

        // generate diagnostic ions
        if (fragmentationParameters.GenerateDiagnosticIons && bpwsm != null)
        {        
            // TODO: this code is memory-efficient but sort of CPU inefficient; it can be further optimized.
            // however, diagnostic ions are fairly rare so it's probably OK for now
            foreach (double diagnosticIon in bpwsm.AllModsOneIsNterminus.Values
                .Where(p => p.DiagnosticIons != null)
                .SelectMany(p => p.DiagnosticIons.Where(v => v.Key == fragmentationParameters.DissociationType || v.Key == DissociationType.AnyActivationType))
                .SelectMany(p => p.Value)
                .Distinct())
            {
                int diagnosticIonLabel = (int)Math.Round(diagnosticIon.ToMz(1, fragmentationParameters.Polarity), 0);

                // the diagnostic ion is assumed to be annotated in the mod info as the *neutral mass* of the diagnostic ion, not the ionized species
                products.Add(new Product(ProductType.D, FragmentationTerminus.Both, diagnosticIon, diagnosticIonLabel, 0, 0));
            }
        }
    }


    /// <summary>
    /// Generates theoretical internal fragments for given dissociation type for this peptide. 
    /// <param name="dissociationType">The dissociation type to use for fragmentation</param>
    /// <param name="minLengthOfFragments">The minimum number of amino acids for an internal fragment to be included</param>
    /// <param name="products">The list to be filled with the generated fragments</param>
    /// <param name="fragmentationParams">Optional fragmentation parameters</param>
    /// </summary>
    public void FragmentInternally(DissociationType dissociationType, int minLengthOfFragments, List<Product> products, IFragmentationParams? fragmentationParams = null);

}

public static class IFragmentableExtensions
{
    public static void Fragment(this IFragmentable fragmentable, DissociationType dissociationType, FragmentationTerminus fragmentationTerminus, List<Product> products, IFragmentationParams? fragmentationParams = null)
    {
        fragmentationParams ??= fragmentable.DefaultFragmentationParams;
        fragmentationParams.DissociationType = dissociationType;
        fragmentationParams.FragmentationTerminus = fragmentationTerminus;

        fragmentable.Fragment(fragmentationParams, ref products);
    }

    public static void Fragment(this IFragmentable fragmentable, IFragmentationParams fragmentationParameters, ref List<Product> products)
    {
        fragmentable.Fragment(fragmentationParameters, ref products);
    }
}
