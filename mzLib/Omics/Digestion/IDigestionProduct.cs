namespace Omics.Digestion
{
    public interface IDigestionProduct
    {
        /// <summary>
        /// BioPolymer that this lysis product is a digestion product of.
        /// </summary>
        public IBioPolymer Parent { get; }

        /// <summary>
        /// The primary sequence of the digestion product. 
        /// </summary>
        public string BaseSequence { get; }

        /// <summary>
        /// Unstructured explanation of source. 
        /// 
        /// Examples include: 
        /// Top-down truncation: full-length proteoform C-terminal digestion truncation
        /// Top-down truncation: DECOY full-length proteoform N-terminal digestion truncation
        /// Bottom-up search: full
        /// Bottom-up search: DECOY full
        /// Bottom-up search : chain(49-597) start
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The residue number at which the peptide begins (the first residue in a protein is 1).
        /// </summary>
        public int OneBasedStartResidue { get; }

        /// <summary>
        /// The residue number at which the peptide ends.
        /// </summary>
        public int OneBasedEndResidue { get; }

        /// <summary>
        /// The number of missed cleavages this peptide has with respect to the digesting protease.
        /// </summary>
        public int MissedCleavages { get; }

        /// <summary>
        /// How many residues long the peptide is.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// The residue immediately preceding the digestion product.
        /// </summary>
        public char PreviousResidue { get; }

        /// <summary>
        /// The residue immediately following the digestion product.
        /// </summary>
        public char NextResidue { get; }

        public char this[int zeroBasedIndex] => BaseSequence[zeroBasedIndex];
        public static string GetBaseSequence(IBioPolymer parent, int oneBasedStartResidue, int oneBasedEndResidue)
        {
            return parent.BaseSequence.Substring(oneBasedStartResidue - 1, oneBasedEndResidue - oneBasedStartResidue + 1);
        }
    }
}
