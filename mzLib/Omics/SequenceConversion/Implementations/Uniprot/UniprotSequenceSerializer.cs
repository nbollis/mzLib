using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Omics.SequenceConversion;

/// <summary>
/// Serializes canonical sequences to the UniProt modification notation (e.g., [UniProt:Phosphoserine on S]).
/// UniProt has special serialization rules:
/// - "Carbamidomethyl" (fixed modifications) are REMOVED entirely from output
/// </summary>
public class UniProtSequenceSerializer : SequenceSerializerBase
{
    public static UniProtSequenceSerializer Instance { get; } = new();

    /// <summary>
    /// Modification types that should be completely removed from output (e.g., fixed modifications).
    /// </summary>
    private static readonly HashSet<string> SuppressedModTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Common Fixed"
    };

    /// <summary>
    /// Specific modification names that should be suppressed (regardless of type).
    /// </summary>
    private static readonly HashSet<string> SuppressedModNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Carbamidomethyl",
        "S-carbamoylmethylcysteine"
    };

    public UniProtSequenceSerializer(SequenceFormatSchema? schema = null, IModificationLookup? lookup = null)
        : base(lookup ?? UniProtModificationLookup.Instance)
    {
        Schema = schema ?? UniProtSequenceSchema.Instance;
    }

    public override string FormatName => Schema.FormatName;

    public override SequenceFormatSchema Schema { get; }

    public override SequenceConversionHandlingMode HandlingMode => SequenceConversionHandlingMode.RemoveIncompatibleElements;

    public override bool CanSerialize(CanonicalSequence sequence) => !string.IsNullOrEmpty(sequence.BaseSequence);

    /// <summary>
    /// Override to set the target residue for N-terminal modifications based on the sequence's first character.
    /// This enables lookup of residue-specific N-terminal mods like "N-acetylproline".
    /// </summary>
    protected override CanonicalSequence EnrichModificationsIfNeeded(CanonicalSequence sequence)
    {
        if (!sequence.HasModifications || string.IsNullOrEmpty(sequence.BaseSequence))
        {
            return base.EnrichModificationsIfNeeded(sequence);
        }

        // Check if we have an N-terminal mod that needs its target residue set
        // TargetResidue may be null OR 'X' (generic N-terminal target) - both need updating
        var nTermMod = sequence.NTerminalModification;
        if (nTermMod.HasValue && 
            (nTermMod.Value.TargetResidue == 'X' || !nTermMod.Value.TargetResidue.HasValue) &&
            sequence.BaseSequence.Length > 0)
        {
            // Set the target residue to the first character of the sequence
            var firstResidue = sequence.BaseSequence[0];
            var updatedNTermMod = nTermMod.Value with { TargetResidue = firstResidue };
            
            // Create a new sequence with the updated N-terminal mod
            var allMods = sequence.Modifications.ToArray();
            for (int i = 0; i < allMods.Length; i++)
            {
                if (allMods[i].PositionType == ModificationPositionType.NTerminus)
                {
                    allMods[i] = updatedNTermMod;
                    break;
                }
            }
            sequence = sequence.WithModifications(allMods);
        }

        // Now call the base implementation to do the actual resolution
        return base.EnrichModificationsIfNeeded(sequence);
    }

    public override bool ShouldResolveMod(CanonicalModification mod)
    {
        var resolved = mod.MzLibModification;
        if (resolved != null)
        {
            // Already resolved - need to re-resolve only if it's NOT already a UniProt mod
            return !string.Equals(resolved.ModificationType, "UniProt", StringComparison.OrdinalIgnoreCase);
        }

        // Not yet resolved - need to resolve via lookup UNLESS it already has inline UniProt representation
        // (i.e., the OriginalRepresentation already contains "UniProt:" which can be used directly)
        return mod.OriginalRepresentation?.IndexOf("UniProt", StringComparison.OrdinalIgnoreCase) < 0;
    }

    protected override string? GetModificationString(CanonicalModification mod, ConversionWarnings warnings, SequenceConversionHandlingMode mode)
    {
        if (ShouldSuppressMod(mod))
        {
            return null;
        }

        bool writeType = Schema is UniProtSequenceSchema { WriteModType: true };
        bool writeMotif = Schema is UniProtSequenceSchema { WriteMotifs: true };
        var resolved = mod.MzLibModification;
        if (resolved != null &&
            !string.IsNullOrWhiteSpace(resolved.IdWithMotif) &&
            string.Equals(resolved.ModificationType, "UniProt", StringComparison.OrdinalIgnoreCase))
        {
            return writeType switch
            {
                false when !writeMotif => resolved.OriginalId,
                false when writeMotif => resolved.IdWithMotif,
                true when !writeMotif => resolved.ModificationType + ":" + resolved.OriginalId,
                true when writeMotif => resolved.ModificationType + ":" + resolved.IdWithMotif
            };
        }

        var inline = TryGetInlineUniProtRepresentation(mod, writeMotif, writeType);
        if (inline != null)
        {
            return inline;
        }

        warnings.AddIncompatibleItem(mod.ToString());

        if (mode == SequenceConversionHandlingMode.RemoveIncompatibleElements)
        {
            warnings.AddWarning($"Removing incompatible modification without UniProt mapping: {mod}");
            return null;
        }

        if (mode == SequenceConversionHandlingMode.ThrowException)
        {
            throw new SequenceConversionException(
                $"Cannot serialize modification in UniProt format - mapping unavailable: {mod}",
                ConversionFailureReason.IncompatibleModifications,
                new[] { mod.ToString() });
        }

        return null;
    }

    private string? TryGetInlineUniProtRepresentation(CanonicalModification mod, bool writeMotif, bool writeType)
    {
        if (string.IsNullOrWhiteSpace(mod.OriginalRepresentation))
        {
            return null;
        }

        var representation = mod.OriginalRepresentation.Trim().Trim('[', ']');
        if (representation.IndexOf("UniProt", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return null;
        }

        var colonIndex = representation.IndexOf(':');
        var payload = colonIndex >= 0
            ? representation[(colonIndex + 1)..].Trim()
            : representation;

        var motifIndex = payload.IndexOf("on", StringComparison.Ordinal);
        if (motifIndex >= 0)
        {
            var motifPart = payload[(motifIndex + 2)..].Trim();
            payload = payload[..motifIndex].Trim();
            if (writeMotif && !string.IsNullOrEmpty(motifPart))
            {
                payload += $" on {motifPart}";
            }
        }

        if (writeType && colonIndex >= 0)
        {
            var typePart = representation[..colonIndex].Trim();
            if (!string.IsNullOrEmpty(typePart))
            {
                payload = $"{typePart}:{payload}";
            }
        }
        return payload;
    }

    /// <summary>
    /// Determines if a modification should be completely suppressed from output.
    /// </summary>
    private static bool ShouldSuppressMod(CanonicalModification mod)
    {
        var resolved = mod.MzLibModification;

        // Check by modification type in original representation FIRST (e.g., "Common Fixed:Carbamidomethyl")
        var orig = mod.OriginalRepresentation;
        if (!string.IsNullOrEmpty(orig))
        {
            foreach (var suppressedType in SuppressedModTypes)
            {
                if (orig.Contains(suppressedType, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // Check original representation for suppressed mod names
            foreach (var suppressed in SuppressedModNames)
            {
                if (orig.Contains(suppressed, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        // Also check resolved mod's type (for completeness)
        if (resolved?.ModificationType != null && SuppressedModTypes.Contains(resolved.ModificationType))
        {
            return true;
        }

        // Check resolved mod's name
        var resolvedName = resolved?.OriginalId ?? resolved?.IdWithMotif;
        if (!string.IsNullOrEmpty(resolvedName))
        {
            foreach (var suppressed in SuppressedModNames)
            {
                if (resolvedName.Contains(suppressed, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
