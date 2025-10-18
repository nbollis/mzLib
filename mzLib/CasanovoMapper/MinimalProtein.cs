using Omics.Digestion;
using Proteomics.ProteolyticDigestion;

namespace CasanovoMapper;
public class MinimalProtein
{
    public string Accession { get; set; } 
    public string Header { get; set; }
    public string Sequence { get; set; }

    public MinimalProtein(string accession, string header, string sequence)
    {
        Accession = accession;
        Header = header;
        Sequence = sequence;
    }

    public IEnumerable<string> Digest(IDigestionParams digestionParams, string? sequence = null)
    {
        sequence ??= Sequence;
        int missedCleavagesAllowed = digestionParams.MaxMissedCleavages;
        int minPeptideLength = digestionParams.MinLength;
        int maxPeptideLength = digestionParams.MaxLength;
        var initiatorMethionineBehavior = digestionParams is DigestionParams dig ? dig.InitiatorMethionineBehavior : InitiatorMethionineBehavior.Retain;
        List<int> oneBasedIndicesToCleaveAfter = digestionParams.DigestionAgent.GetDigestionSiteIndices(Sequence);

        char firstResidueInProtein = sequence[0];

        for (int missedCleavages = 0; missedCleavages <= missedCleavagesAllowed; missedCleavages++)
        {
            for (int i = 0; i < oneBasedIndicesToCleaveAfter.Count - missedCleavages - 1; i++)
            {
                if (Retain(i, initiatorMethionineBehavior, firstResidueInProtein)
                    && ValidLength(oneBasedIndicesToCleaveAfter[i + missedCleavages + 1] - oneBasedIndicesToCleaveAfter[i], minPeptideLength, maxPeptideLength))
                {
                    int zeroBasedStartResidue = oneBasedIndicesToCleaveAfter[i];
                    int zeroBasedEndResidue = oneBasedIndicesToCleaveAfter[i + missedCleavages + 1] - 1;


                    yield return sequence.Substring(zeroBasedStartResidue, zeroBasedEndResidue - zeroBasedStartResidue + 1);
                }
                if (Cleave(i, initiatorMethionineBehavior, firstResidueInProtein) && oneBasedIndicesToCleaveAfter[1] != 1 //prevent duplicates if that bond is cleaved by the protease
                    && ValidLength(oneBasedIndicesToCleaveAfter[i + missedCleavages + 1] - 1, minPeptideLength, maxPeptideLength))
                {
                    int zeroBasedStartResidue = 1; //skip the initiator methionine
                    int zeroBasedEndResidue = oneBasedIndicesToCleaveAfter[i + missedCleavages + 1] - 1;
                    yield return sequence.Substring(zeroBasedStartResidue, zeroBasedEndResidue - zeroBasedStartResidue + 1);
                }
            }
        }
    }

    public IEnumerable<string> GetDecoyPeptides(IDigestionParams digParams) => Digest(digParams, GetDecoySequence());

    public string GetDecoySequence()
    {
        // reverse sequence
        // Do not include the initiator methionine in reversal!!!
        char[] sequenceArray = Sequence.ToCharArray();
        bool startsWithM = Sequence.StartsWith("M", StringComparison.Ordinal);
        if (startsWithM)
        {
            Array.Reverse(sequenceArray, 1, Sequence.Length - 1);
        }
        else
        {
            Array.Reverse(sequenceArray);
        }
        string reversedSequence = new string(sequenceArray);
        return reversedSequence;
    }

    #region Digestion Helpers - Excised from Protease and Digestion Agent classes

    /// <summary>
    /// Retain N-terminal residue?
    /// </summary>
    /// <param name="oneBasedCleaveAfter"></param>
    /// <param name="initiatorMethionineBehavior"></param>
    /// <param name="nTerminus"></param>
    /// <returns></returns>
    static bool Retain(int oneBasedCleaveAfter, InitiatorMethionineBehavior initiatorMethionineBehavior, char nTerminus)
    {
        return oneBasedCleaveAfter != 0 // this only pertains to the n-terminus
               || initiatorMethionineBehavior != InitiatorMethionineBehavior.Cleave
               || nTerminus != 'M';
    }

    /// <summary>
    /// Cleave N-terminal residue?
    /// </summary>
    /// <param name="oneBasedCleaveAfter"></param>
    /// <param name="initiatorMethionineBehavior"></param>
    /// <param name="nTerminus"></param>
    /// <returns></returns>
    static bool Cleave(int oneBasedCleaveAfter, InitiatorMethionineBehavior initiatorMethionineBehavior, char nTerminus)
    {
        return oneBasedCleaveAfter == 0 // this only pertains to the n-terminus
               && initiatorMethionineBehavior != InitiatorMethionineBehavior.Retain
               && nTerminus == 'M';
    }

    /// <summary>
    /// Is length of given peptide okay, given minimum and maximum?
    /// </summary>
    /// <param name="length"></param>
    /// <param name="minLength"></param>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    static bool ValidLength(int length, int minLength, int maxLength)
    {
        return ValidMinLength(length, minLength) && ValidMaxLength(length, maxLength);
    }

    /// <summary>
    /// Is length of given peptide okay, given minimum?
    /// </summary>
    /// <param name="length"></param>
    /// <param name="minLength"></param>
    /// <returns></returns>
    static bool ValidMinLength(int length, int minLength)
    {
        return length >= minLength;
    }

    /// <summary>
    /// Is length of given peptide okay, given maximum?
    /// </summary>
    /// <param name="length"></param>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    static bool ValidMaxLength(int? length, int maxLength)
    {
        return !length.HasValue || length <= maxLength;
    }

    #endregion
}
