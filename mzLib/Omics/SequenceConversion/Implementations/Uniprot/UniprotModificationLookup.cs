using System;
using System.Collections.Generic;
using System.Linq;
using Chemistry;
using Omics.Modifications;

namespace Omics.SequenceConversion;

/// <summary>
/// Resolves modifications to their UniProt representations by searching the UniProt PTM database.
/// Uses fuzzy string matching with overlap scoring to find the best match.
/// </summary>
public class UniProtModificationLookup : ModificationLookupBase
{
    public static UniProtModificationLookup Instance { get; } = new();

    private static readonly Dictionary<char, string> ResidueNameMap = new()
    {
        ['A'] = "Alanine",
        ['R'] = "Arginine",
        ['N'] = "Asparagine",
        ['D'] = "Aspartic acid",
        ['C'] = "Cysteine",
        ['E'] = "Glutamic acid",
        ['Q'] = "Glutamine",
        ['G'] = "Glycine",
        ['H'] = "Histidine",
        ['I'] = "Isoleucine",
        ['L'] = "Leucine",
        ['K'] = "Lysine",
        ['M'] = "Methionine",
        ['F'] = "Phenylalanine",
        ['P'] = "Proline",
        ['S'] = "Serine",
        ['T'] = "Threonine",
        ['W'] = "Tryptophan",
        ['Y'] = "Tyrosine",
        ['V'] = "Valine",
        ['U'] = "Selenocysteine",
        ['O'] = "Pyrrolysine"
    };
    public UniProtModificationLookup(IEnumerable<Modification>? candidateSet = null, double massTolerance = 0.001)
        : base(candidateSet ?? Mods.UniprotModifications, massTolerance)
    {
    }

    public override string Name => "UniProt";

    protected override string NormalizeRepresentation(string representation)
    {
        var normalized = base.NormalizeRepresentation(representation);
        if (string.IsNullOrEmpty(normalized))
        {
            return normalized;
        }

        var colonIndex = normalized.IndexOf(':');
        if (colonIndex >= 0)
        {
            var prefix = normalized[..colonIndex].Trim();
            var remainder = normalized[(colonIndex + 1)..].Trim();

            if (prefix.Equals("UniProt", StringComparison.OrdinalIgnoreCase) || prefix.Contains(' '))
            {
                return string.IsNullOrEmpty(remainder) ? normalized : remainder;
            }
        }

        return normalized;
    }

    protected override IEnumerable<string> ExpandNameCandidates(string normalizedRepresentation, char? targetResidue)
    {
        if (string.IsNullOrWhiteSpace(normalizedRepresentation))
        {
            yield break;
        }

        var normalized = NormalizeRepresentation(normalizedRepresentation);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            yield break;
        }

        yield return normalized;

        if (targetResidue.HasValue)
        {
            yield return $"{normalized} on {targetResidue.Value}";
            yield return $"{normalized} on X";
        }

        if (normalized.Contains("on N-terminus", StringComparison.OrdinalIgnoreCase))
        {
            yield return normalized.Replace("on N-terminus", "on X", StringComparison.OrdinalIgnoreCase);
            if (targetResidue.HasValue)
            {
                yield return normalized.Replace("on N-terminus", $"on {targetResidue.Value}", StringComparison.OrdinalIgnoreCase);
            }
        }

        if (normalized.IndexOf("ation", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            var withoutAtion = ReplaceCaseInsensitive(normalized, "ation", string.Empty);
            yield return withoutAtion;
            yield return $"{withoutAtion} on {targetResidue ?? 'X'}";
            
            var withYl = ReplaceCaseInsensitive(normalized, "ation", "yl");
            yield return withYl;
            yield return $"{withYl} on {targetResidue ?? 'X'}";
        }

        var trimmed = normalized.Split(new[] { "-L-" }, StringSplitOptions.None)[0];
        if (trimmed != normalized)
        {
            yield return trimmed;
            if (targetResidue.HasValue)
            {
                yield return $"{trimmed} on {targetResidue.Value}";
            }
        }
    }

    private static string ReplaceCaseInsensitive(string input, string search, string replacement)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(search))
        {
            return input;
        }

        var start = input.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return input;
        }

        return string.Concat(input.AsSpan(0, start), replacement, input.AsSpan(start + search.Length));
    }
}
