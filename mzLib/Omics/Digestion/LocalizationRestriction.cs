namespace Omics.Digestion
{
    /// <summary>
    /// The localization restriction of a modification.
    /// <remarks>
    /// PeptideNTerm and PeptideTermC are for peptide modifications, used on the terminus of ALL digestion products.
    /// </remarks>
    /// </summary>
    public enum LocalizationRestriction
    {
        Anywhere,
        Unassigned,

        // Used for ALL digestion products
        PeptideNTerminal, OligoFivePrimeTerminal,
        PeptideCTerminal, OligoThreePrimeTerminal,

        // Used only on termini from the intact BioPolymer (pre digestion). 
        NTerminal, FivePrimeTerminal,
        CTerminal, ThreePrimeTerminal,
    }

    public static class LocalizationRestrictionExtensions
    {

        /// <summary>
        /// Converts mod text string to LocalizationRestriction enum
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static LocalizationRestriction ToLocalizationRestriction(this string? input)
        {
            return input switch
            {
                "N-terminal." => LocalizationRestriction.NTerminal,
                "C-terminal." => LocalizationRestriction.CTerminal,
                "Peptide N-terminal." => LocalizationRestriction.PeptideNTerminal,
                "Peptide C-terminal." => LocalizationRestriction.PeptideCTerminal,
                "Anywhere." => LocalizationRestriction.Anywhere,
                "3'-terminal." => LocalizationRestriction.ThreePrimeTerminal,
                "5'-terminal." => LocalizationRestriction.FivePrimeTerminal,
                "Oligo 3'-terminal." => LocalizationRestriction.OligoThreePrimeTerminal,
                "Oligo 5'-terminal." => LocalizationRestriction.OligoFivePrimeTerminal,
                _ => LocalizationRestriction.Unassigned,
            };
        }

        /// <summary>
        /// Converts LocalizationRestriction enum to mod text string
        /// </summary>
        /// <param name="restriction"></param>
        /// <returns></returns>
        public static string ToModTextString(this LocalizationRestriction restriction)
        {
            return restriction switch
            {
                LocalizationRestriction.NTerminal => "N-terminal.",
                LocalizationRestriction.CTerminal => "C-terminal.",
                LocalizationRestriction.PeptideNTerminal => "Peptide N-terminal.",
                LocalizationRestriction.PeptideCTerminal => "Peptide C-terminal.",
                LocalizationRestriction.Anywhere => "Anywhere.",
                LocalizationRestriction.ThreePrimeTerminal => "3'-terminal.",
                LocalizationRestriction.FivePrimeTerminal => "5'-terminal.",
                LocalizationRestriction.OligoThreePrimeTerminal => "Oligo 3'-terminal.",
                LocalizationRestriction.OligoFivePrimeTerminal => "Oligo 5'-terminal.",
                _ => "Unassigned.",
            };
        }

        public static bool IsStartTerminus(this LocalizationRestriction restriction)
        {
            return restriction is LocalizationRestriction.PeptideNTerminal
                or LocalizationRestriction.OligoFivePrimeTerminal
                or LocalizationRestriction.NTerminal
                or LocalizationRestriction.FivePrimeTerminal;
        }

        public static bool IsEndTerminus(this LocalizationRestriction restriction)
        {
            return restriction is LocalizationRestriction.PeptideCTerminal
                or LocalizationRestriction.OligoThreePrimeTerminal
                or LocalizationRestriction.CTerminal
                or LocalizationRestriction.ThreePrimeTerminal;
        }
    }
}
