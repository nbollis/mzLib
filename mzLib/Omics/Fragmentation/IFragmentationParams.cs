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
    /// 
    /// <remarks>
    /// Currently used only in diagnost ion label generation. 
    /// </remarks>
    /// </summary>
    Polarity Polarity { get; set; }
}