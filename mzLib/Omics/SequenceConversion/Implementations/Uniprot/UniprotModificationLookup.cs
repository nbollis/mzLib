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

        if (targetResidue.HasValue)
        {
            foreach (var variant in GenerateResidueSpecificVariants(normalized, targetResidue.Value))
            {
                yield return variant;
            }
        }
    }

    private IEnumerable<string> GenerateResidueSpecificVariants(string modificationName, char targetResidue)
    {
        var residueName = targetResidue.ToString();
        if (ResidueNameMap.TryGetValue(char.ToUpperInvariant(targetResidue), out var fullResidueName))
        {
            residueName = fullResidueName;
        }
        var lowerResidue = residueName.ToLowerInvariant();
        var upperResidue = char.ToUpperInvariant(targetResidue);

        foreach (var suffix in new[] { "ylation", "rylation", "ation", "lation" })
        {
            var root = TryRemoveSuffix(modificationName, suffix);
            if (!string.IsNullOrEmpty(root) && root.Length < modificationName.Length)
            {
                yield return $"{root}{lowerResidue}";
                yield return $"{root}{lowerResidue} on {upperResidue}";
                
                var initialUpper = char.ToUpperInvariant(root[0]) + root[1..];
                yield return $"{initialUpper}{lowerResidue}";
                yield return $"{initialUpper}{lowerResidue} on {upperResidue}";
            }
        }
    }

    private static string TryRemoveSuffix(string name, string suffix)
    {
        if (name.Length <= suffix.Length)
        {
            return null;
        }

        int onIndex = name.IndexOf(" on ", StringComparison.Ordinal);
        if (onIndex > 0)
            name = name.Substring(0, onIndex).Trim();

        var ending = name.AsSpan(name.Length - suffix.Length);
        if (ending.Equals(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return name[..^suffix.Length];
        }

        return null;
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

    protected override Modification? ScoreCandidates(
        IList<Modification> candidates,
        char? targetResidue,
        string? originalRepresentation,
        Chemistry.ChemicalFormula? chemicalFormula)
    {
        if (candidates.Count <= 1 || string.IsNullOrWhiteSpace(originalRepresentation))
        {
            return null;
        }

        var normalizedOriginal = NormalizeRepresentation(originalRepresentation);
        var targetStr = targetResidue?.ToString();

        var scored = candidates.Select(mod =>
        {
            int score = 0;

            if (chemicalFormula != null && mod.ChemicalFormula != null)
            {
                if (chemicalFormula.Equals(mod.ChemicalFormula))
                {
                    score += 1000;
                }
                else
                {
                    score -= 1000;
                }
            }

            var modTarget = mod.Target?.Motif;
            if (!string.IsNullOrEmpty(modTarget))
            {
                if (modTarget == targetStr)
                {
                    score += 500;
                }
                else if (modTarget == "X")
                {
                    score += 250;
                }
            }

            var overlapScore = CalculateOverlapScore(mod, normalizedOriginal);
            score += overlapScore;

            var nameLengthPenalty = (mod.OriginalId?.Length ?? 0) * 1;
            score -= nameLengthPenalty;

            return (mod, score);
        }).ToList();

        var bestScore = scored.Max(s => s.score);
        var bestCandidates = scored.Where(s => s.score == bestScore).Select(s => s.mod).ToList();

        return bestCandidates.Count == 1 ? bestCandidates[0] : null;
    }

    private int CalculateOverlapScore(Modification mod, string normalizedOriginal)
    {
        var idWithMotif = mod.IdWithMotif ?? mod.OriginalId ?? "";

        int bestOverlap = 0;
        for (int i = 0; i < idWithMotif.Length; i++)
        {
            for (int j = 0; j < normalizedOriginal.Length; j++)
            {
                int k = 0;
                while (i + k < idWithMotif.Length && j + k < normalizedOriginal.Length && 
                       char.ToLowerInvariant(idWithMotif[i + k]) == char.ToLowerInvariant(normalizedOriginal[j + k]))
                {
                    k++;
                }
                bestOverlap = Math.Max(bestOverlap, k);
            }
        }

        return bestOverlap * 10;
    }
}
