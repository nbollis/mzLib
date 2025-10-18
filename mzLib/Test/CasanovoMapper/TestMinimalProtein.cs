using System.Linq;
using CasanovoMapper;
using NUnit.Framework;
using Proteomics.ProteolyticDigestion;

namespace Test.CasanovoMapper;
[TestFixture]
public class TestMinimalProtein
{
    [Test]
    [TestCase("PEPTIDEKIAIDIS", new[] {"PEPTIDEK", "IAIDIS"}, 0, 4, 100)] // standard trypsin, no missed cleavages
    [TestCase("PEPTIDEKIAIDIS", new[] {"PEPTIDEK", "IAIDIS", "PEPTIDEKIAIDIS" }, 1, 4, 100)] // allow 1 missed cleavage, full peptide included
    [TestCase("PEPTIDEKIAIDIS", new[] {"PEPTIDEK", "PEPTIDEKIAIDIS" }, 1, 7, 100)] // min length excludes IAIDIS
    // Missed cleavages: 2, should allow PEPTIDEK, IAIDIS, PEPTIDEKIAIDIS, and PEPTIDEKIAID
    [TestCase("PEPTIDEKIAIRIS", new[] {"PEPTIDEK", "IAIRIS", "IAIR", "PEPTIDEKIAIRIS", "PEPTIDEKIAIR"}, 2, 4, 100)]
    // Min length: 8, only PEPTIDEKIAIDIS and PEPTIDEKIAID (length 13, 11)
    [TestCase("PEPTIDEKIAIRIS", new[] {"PEPTIDEKIAIRIS", "PEPTIDEKIAIR", "PEPTIDEK" }, 2, 8, 100)]
    // Max length: 8, only PEPTIDEK (8) and IAIDIS (6)
    [TestCase("PEPTIDEKIAIDIS", new[] {"PEPTIDEK", "IAIDIS"}, 2, 4, 8)]
    // Min=4, Max=8, only PEPTIDEK (8) and IAIDIS (6)
    [TestCase("PEPTIDEKIAIDIS", new[] {"PEPTIDEK", "IAIDIS"}, 2, 4, 8)]
    // Min=4, Max=7, only IAIDIS (6)
    [TestCase("PEPTIDEKIAIDIS", new[] {"IAIDIS"}, 2, 4, 7)]
    // Sequence with multiple K/R, missed cleavages 0
    [TestCase("AKBKCK", new[] {"AK", "BK", "CK"}, 0, 2, 10)]
    // Sequence with multiple K/R, missed cleavages 1, should allow AK, BK, CK, AKBK, BKCK
    [TestCase("AKBKCK", new[] {"AK", "BK", "CK", "AKBK", "BKCK"}, 1, 2, 10)]
    // Sequence with multiple K/R, missed cleavages 2, should allow AK, BK, CK, AKBK, BKCK, AKBKCK
    [TestCase("AKBKCK", new[] {"AK", "BK", "CK", "AKBK", "BKCK", "AKBKCK"}, 2, 2, 10)]
    public void TestDigestion(string inputSequence, string[] expectedPeptides, int missedCleavages, int min, int max)
    {
        var minProt = new MinimalProtein("", "", inputSequence);
        var digestionParams = new DigestionParams("trypsin", missedCleavages, min, max);
        var digestedPeptides = minProt.Digest(digestionParams).ToList();
        Assert.That(expectedPeptides.Length, Is.EqualTo(digestedPeptides.Count));
        foreach (var peptide in expectedPeptides)
        {
            Assert.That(digestedPeptides.Contains(peptide), Is.True, $"Expected peptide {peptide} not found in digested peptides.");
        }
    }
}
