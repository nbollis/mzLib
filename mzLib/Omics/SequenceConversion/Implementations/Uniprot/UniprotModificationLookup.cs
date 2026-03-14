using System;
using System.Collections.Generic;
using System.Linq;
using Omics.Modifications;

namespace Omics.SequenceConversion;

/// <summary>
/// Resolves modifications to their UniProt representations by searching the UniProt PTM database.
/// Uses fuzzy string matching with overlap scoring to find the best match.
/// </summary>
public class UniProtModificationLookup : ModificationLookupBase
{
    public static UniProtModificationLookup Instance { get; } = new();

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

    protected override IEnumerable<Modification> FilterByName(IEnumerable<Modification> source, string name, char? targetResidue)
    {
        var candidates = base.FilterByName(source, name, targetResidue).ToList();
        if (candidates.Count > 0)
        {
            return candidates;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return [];
        }

        var normalized = NormalizeRepresentation(name);
        var trimmed = normalized.Split(new[] { "-L-" }, StringSplitOptions.None)[0];
        
        var potentialNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { normalized };
        if (trimmed != normalized)
        {
            potentialNames.Add(trimmed);
        }

        if (normalized.Contains("ation", StringComparison.OrdinalIgnoreCase))
        {
            potentialNames.Add(ReplaceCaseInsensitive(normalized, "ation", string.Empty));
            potentialNames.Add(ReplaceCaseInsensitive(normalized, "ation", "yl"));
        }

        if (targetResidue.HasValue)
        {
            foreach (var potentialName in potentialNames.ToList())
            {
                potentialNames.Add($"{potentialName} on {targetResidue.Value}");
                potentialNames.Add($"{potentialName} on X");
            }
        }

        source ??= CandidateSet;
        return source.Where(mod =>
        {
            foreach (var potentialName in potentialNames)
            {
                if (MatchesIdentifier(mod, potentialName))
                {
                    return true;
                }
            }
            return false;
        }).Distinct();
    }

    protected IEnumerable<Modification> ScoreAndSelectBest(
        IList<Modification> candidates, 
        string originalName, 
        char? targetResidue,
        Chemistry.ChemicalFormula? chemicalFormula)
    {
        if (candidates.Count == 0)
        {
            yield break;
        }

        if (candidates.Count == 1)
        {
            yield return candidates[0];
            yield break;
        }

        var scored = candidates
            .Select(c => (mod: c, score: CalculateMatchScore(c, originalName, targetResidue, chemicalFormula)))
            .OrderByDescending(x => x.score)
            .ToList();

        var bestScore = scored[0].score;
        
        foreach (var (mod, score) in scored)
        {
            if (score == bestScore)
            {
                yield return mod;
            }
            else
            {
                yield break;
            }
        }
    }

    private int CalculateMatchScore(Modification mod, string originalName, char? targetResidue, Chemistry.ChemicalFormula? formula)
    {
        int score = 0;

        var nameVariants = GetNameVariants(mod).ToList();
        var normalizedOriginal = NormalizeRepresentation(originalName);

        int bestOverlap = 0;
        foreach (var variant in nameVariants)
        {
            var overlap = GetOverlapScore(variant, normalizedOriginal);
            if (overlap > bestOverlap)
            {
                bestOverlap = overlap;
            }
        }
        score += bestOverlap * 10;

        if (targetResidue.HasValue)
        {
            var modTarget = mod.Target?.Motif;
            if (!string.IsNullOrEmpty(modTarget))
            {
                if (modTarget == targetResidue.Value.ToString())
                {
                    score += 100;
                }
                else if (modTarget == "X")
                {
                    score += 50;
                }
            }
        }

        if (formula != null && mod.ChemicalFormula != null && mod.ChemicalFormula.Equals(formula))
        {
            score += 500;
        }

        var nameScore = GetNameSpecificityScore(mod);
        score -= nameScore;

        return score;
    }

    private static IEnumerable<string> GetNameVariants(Modification mod)
    {
        if (!string.IsNullOrEmpty(mod.OriginalId))
        {
            yield return mod.OriginalId;
        }

        if (!string.IsNullOrEmpty(mod.IdWithMotif))
        {
            yield return mod.IdWithMotif;
            
            if (mod.IdWithMotif.Contains(" on ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = mod.IdWithMotif.Split(new[] { " on " }, StringSplitOptions.None);
                if (parts.Length > 0)
                {
                    yield return parts[0];
                }
            }
        }

        if (!string.IsNullOrEmpty(mod.ModificationType) && !string.IsNullOrEmpty(mod.IdWithMotif))
        {
            yield return $"{mod.ModificationType}:{mod.IdWithMotif}";
        }
    }

    private static int GetNameSpecificityScore(Modification mod)
    {
        if (string.IsNullOrEmpty(mod.OriginalId))
        {
            return 0;
        }

        int score = 0;

        if (mod.OriginalId.Contains('(', StringComparison.OrdinalIgnoreCase) ||
            mod.OriginalId.Contains(')', StringComparison.OrdinalIgnoreCase))
        {
            score += 5;
        }

        if (mod.OriginalId.Contains(',', StringComparison.OrdinalIgnoreCase))
        {
            score += 3;
        }

        score += mod.OriginalId.Length / 10;

        return score;
    }
}