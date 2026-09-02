using MassSpectrometry;

namespace Omics.Fragmentation;

public interface IFragmentationParams : IEquatable<IFragmentationParams>
{
    /// <summary>
    /// Whether to generate M ions (the intact molecule with a charge state of 1)
    /// </summary>
    bool GenerateMIon { get; set; }

    /// <summary>
    /// Whether to generate diagnostic ions (e.g., from labile modifications or bases)
    /// </summary>
    bool GenerateDiagnosticIons { get; set; }

    /// <summary>
    /// The types of M ion losses to generate, if GenerateMIon is true
    /// </summary>
    List<MIonLoss> MIonLosses { get; set; }

    /// <summary>
    /// The polarity of the ions to generate (positive or negative). 
    /// </summary>
    /// <remarks>
    /// Currently used only in diagnostic ion label generation. 
    /// </remarks>
    Polarity Polarity { get; set; }

    /// <summary>
    /// The type of dissociation to use for fragmenting the molecule.
    /// </summary>
    DissociationType DissociationType { get; set; }

    /// <summary>
    /// The terminus of the molecule to fragment (N-terminal, C-terminal, or both).
    /// </summary>
    FragmentationTerminus FragmentationTerminus { get; set; }

    /// <summary>
    /// The minimum length of internal fragments to generate.
    /// </summary>
    int MinimumInternalFragmentLength { get; set; }

    /// <summary>
    /// The maximum mass of fragments to generate.
    /// </summary>
    double MaximumFragmentMassDa { get; set; }
}