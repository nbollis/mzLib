using Omics.Digestion;
using Omics.Fragmentation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace mzPlot;

public enum DigestionPolymerType
{
    Protein,
    Rna,
}

public sealed class DigHistResult
{
    public DigHistResult(
        string sourceId,
        DigestionPolymerType polymerType,
        string digestionAgentName,
        int maxMissedCleavages,
        int minLength,
        int maxLength,
        FragmentationTerminus fragmentationTerminus,
        CleavageSpecificity searchModeType,
        int bioPolymersCount,
        int digestedProductsCount,
        int uniqueProductsCount,
        IReadOnlyDictionary<int, int> digestionLengthHistogram)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(digestionAgentName);
        ArgumentNullException.ThrowIfNull(digestionLengthHistogram);

        SourceId = sourceId;
        PolymerType = polymerType;
        DigestionAgentName = digestionAgentName;
        MaxMissedCleavages = maxMissedCleavages;
        MinLength = minLength;
        MaxLength = maxLength;
        FragmentationTerminus = fragmentationTerminus;
        SearchModeType = searchModeType;
        BioPolymersCount = bioPolymersCount;
        DigestedProductsCount = digestedProductsCount;
        UniqueProductsCount = uniqueProductsCount;
        DigestionLengthHistogram = digestionLengthHistogram
            .OrderBy(pair => pair.Key)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    public string SourceId { get; }
    public DigestionPolymerType PolymerType { get; }
    public string DigestionAgentName { get; }
    public int MaxMissedCleavages { get; }
    public int MinLength { get; }
    public int MaxLength { get; }
    public FragmentationTerminus FragmentationTerminus { get; }
    public CleavageSpecificity SearchModeType { get; }
    public int BioPolymersCount { get; }
    public int DigestedProductsCount { get; }
    public int UniqueProductsCount { get; }
    public IReadOnlyDictionary<int, int> DigestionLengthHistogram { get; }

    public string ConditionLabel => $"{DigestionAgentName}, MC={MaxMissedCleavages}";

    public string SeriesLabel => $"{SourceId}: {ConditionLabel}";
}
