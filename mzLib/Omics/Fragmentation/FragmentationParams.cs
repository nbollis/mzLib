using MassSpectrometry;

namespace Omics.Fragmentation;

public class FragmentationParams : IFragmentationParams, IEquatable<FragmentationParams>
{
    public static readonly FragmentationParams Default = new();

    public bool GenerateMIon { get; set; } = false;
    public bool GenerateDiagnosticIons { get; set; } = true;
    public List<MIonLoss> MIonLosses { get; set; } = new();
    public Polarity Polarity { get; set; } = Polarity.Positive;
    public DissociationType DissociationType { get; set; }
    public FragmentationTerminus FragmentationTerminus { get; set; }
    public double MaximumFragmentMassDa { get; set; } = double.MaxValue;
    public int MinimumInternalFragmentLength { get; set; } = 4;

    #region Equality

    public override bool Equals(object? obj)
        => obj is FragmentationParams fp && Equals(fp);

    bool IEquatable<IFragmentationParams>.Equals(IFragmentationParams? other)
        => other is FragmentationParams fp && Equals(fp);

    public bool Equals(FragmentationParams? other)
    {
        if (other is null) return false;
        return GenerateMIon == other.GenerateMIon
               && Polarity == other.Polarity
               && DissociationType == other.DissociationType
               && FragmentationTerminus == other.FragmentationTerminus
               && MaximumFragmentMassDa == other.MaximumFragmentMassDa
               && MinimumInternalFragmentLength == other.MinimumInternalFragmentLength
               && GenerateDiagnosticIons == other.GenerateDiagnosticIons
               && MIonListComparer.Instance.Equals(MIonLosses, other.MIonLosses);
    }

    public override int GetHashCode() => HashCode.Combine(
        GenerateMIon, GenerateDiagnosticIons, Polarity, FragmentationTerminus,
        DissociationType, MaximumFragmentMassDa, MinimumInternalFragmentLength,
        MIonListComparer.Instance.GetHashCode(MIonLosses));

    #endregion
}