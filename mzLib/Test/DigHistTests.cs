using mzPlot;
using NUnit.Framework;
using Omics;
using Omics.Digestion;
using Omics.Fragmentation;
using Plotly.NET;
using Plotly.NET.CSharp;
using Proteomics;
using Proteomics.ProteolyticDigestion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Transcriptomics.Digestion;
using UsefulProteomicsDatabases;
using UsefulProteomicsDatabases.Transcriptomics;

namespace Test;

[TestFixture]
public class DigHistTests
{
    [Test]
    public static void DigHistComputesProteinDigestionHistogram()
    {
        List<IBioPolymer> proteins =
        [
            new Protein("PEPTIDE", "P1", databaseFilePath: "protein-db.fasta"),
            new Protein("TEST", "P2", databaseFilePath: "protein-db.fasta"),
        ];

        IDigestionParams digestionParams = new DigestionParams(
            protease: "trypsin",
            maxMissedCleavages: 0,
            minPeptideLength: 1,
            initiatorMethionineBehavior: InitiatorMethionineBehavior.Retain,
            fragmentationTerminus: FragmentationTerminus.Both);

        DigHistResult result = new DigHist(digestionParams, proteins).Run("human-proteome", DigestionPolymerType.Protein);

        Assert.Multiple(() =>
        {
            Assert.That(result.SourceId, Is.EqualTo("human-proteome"));
            Assert.That(result.PolymerType, Is.EqualTo(DigestionPolymerType.Protein));
            Assert.That(result.DigestionAgentName, Is.EqualTo("trypsin"));
            Assert.That(result.BioPolymersCount, Is.EqualTo(2));
            Assert.That(result.DigestedProductsCount, Is.EqualTo(2));
            Assert.That(result.UniqueProductsCount, Is.EqualTo(2));
            Assert.That(result.DigestionLengthHistogram.Count, Is.EqualTo(2));
            Assert.That(result.DigestionLengthHistogram[4], Is.EqualTo(1));
            Assert.That(result.DigestionLengthHistogram[7], Is.EqualTo(1));
        });
    }

    [Test]
    public static void DigHistCacheRoundTripsCsvResults()
    {
        string cacheDirectory = Path.Combine(Path.GetTempPath(), $"DigHistCache_{Guid.NewGuid():N}");

        try
        {
            List<IBioPolymer> proteins =
            [
                new Protein("AKQK", "P1", databaseFilePath: "protein-db.fasta"),
                new Protein("QWERTK", "P2", databaseFilePath: "protein-db.fasta"),
            ];

            IDigestionParams digestionParams = new DigestionParams(
                protease: "trypsin",
                maxMissedCleavages: 1,
                minPeptideLength: 1,
                initiatorMethionineBehavior: InitiatorMethionineBehavior.Retain,
                fragmentationTerminus: FragmentationTerminus.Both);

            DigHistCache cache = new(cacheDirectory);

            DigHistResult first = cache.GetOrCreate("human-proteome", DigestionPolymerType.Protein, digestionParams, proteins);
            string cacheFilePath = cache.GetCacheFilePath("human-proteome", DigestionPolymerType.Protein, digestionParams, proteins);
            DigHistResult second = cache.GetOrCreate("human-proteome", DigestionPolymerType.Protein, digestionParams, proteins);

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(cacheFilePath), Is.True);
                Assert.That(second.SourceId, Is.EqualTo(first.SourceId));
                Assert.That(second.PolymerType, Is.EqualTo(first.PolymerType));
                Assert.That(second.DigestionAgentName, Is.EqualTo(first.DigestionAgentName));
                Assert.That(second.MaxMissedCleavages, Is.EqualTo(first.MaxMissedCleavages));
                Assert.That(second.DigestedProductsCount, Is.EqualTo(first.DigestedProductsCount));
                Assert.That(second.UniqueProductsCount, Is.EqualTo(first.UniqueProductsCount));
                Assert.That(second.DigestionLengthHistogram, Is.EquivalentTo(first.DigestionLengthHistogram));
            });
        }
        finally
        {
            if (Directory.Exists(cacheDirectory))
            {
                Directory.Delete(cacheDirectory, true);
            }
        }
    }

    [Test]
    public static void DigHistPlotBuilderCreatesProteinAndRnaCharts()
    {
        DigHistPlotBuilder builder = new();

        DigHistResult proteinResult = new(
            "human-proteome",
            DigestionPolymerType.Protein,
            "trypsin",
            1,
            1,
            int.MaxValue,
            FragmentationTerminus.Both,
            CleavageSpecificity.Full,
            2,
            3,
            3,
            new Dictionary<int, int> { [2] = 2, [6] = 1 });

        DigHistResult rnaResult = new(
            "human-lncRNA",
            DigestionPolymerType.Rna,
            "RNase T1",
            0,
            3,
            int.MaxValue,
            FragmentationTerminus.Both,
            CleavageSpecificity.Full,
            2,
            4,
            4,
            new Dictionary<int, int> { [3] = 1, [4] = 3 });

        Assert.Multiple(() =>
        {
            Assert.That(builder.CreateProteinChart([proteinResult]), Is.Not.Null);
            Assert.That(builder.CreateRnaChart([rnaResult]), Is.Not.Null);
        });
    }

    [Test]
    [Explicit("Manual local helper. Replace the placeholder file paths before running.")]
    public static void DigHistManualLocalAnalysis()
    {
        string proteinFastaPath = @"D:\Databases\Human_uniprotkb_proteome_UP000005640_AND_revi_2023_09_29.fasta";
        string rnaFastaPath = @"E:\Users\Nic\Downloads\gencode.v49.lncRNA_transcripts.fa\gencode.v49.lncRNA_transcripts.fa";
        string cacheDirectory = @"D:\Projects\SingleOligoPaperGit\results\DigestionHists";

        EnsureManualPathWasUpdated(proteinFastaPath, nameof(proteinFastaPath));
        EnsureManualPathWasUpdated(rnaFastaPath, nameof(rnaFastaPath));
        EnsureManualPathWasUpdated(cacheDirectory, nameof(cacheDirectory));

        List<Protein> proteins = ProteinDbLoader.LoadProteinFasta(
            proteinFastaPath,
            generateTargets: true,
            decoyType: DecoyType.None,
            isContaminant: false,
            out List<string> proteinErrors);

        List<global::Transcriptomics.RNA> rnas = RnaDbLoader.LoadRnaFasta(
            rnaFastaPath,
            generateTargets: true,
            decoyType: DecoyType.None,
            isContaminant: false,
            out List<string> rnaErrors);

        Assert.That(proteinErrors, Is.Empty, $"Protein FASTA loader reported errors: {string.Join(Environment.NewLine, proteinErrors)}");
        Assert.That(rnaErrors, Is.Empty, $"RNA FASTA loader reported errors: {string.Join(Environment.NewLine, rnaErrors)}");

        DigHistCache cache = new(cacheDirectory);

        List<DigHistResult> results =
        [
            cache.GetOrCreate(
                Path.GetFileNameWithoutExtension(proteinFastaPath),
                DigestionPolymerType.Protein,
                new DigestionParams(
                    protease: "trypsin",
                    maxMissedCleavages: 0,
                    minPeptideLength: 7,
                    initiatorMethionineBehavior: InitiatorMethionineBehavior.Retain,
                    fragmentationTerminus: FragmentationTerminus.Both),
                proteins.Cast<IBioPolymer>()),
            cache.GetOrCreate(
                Path.GetFileNameWithoutExtension(rnaFastaPath),
                DigestionPolymerType.Rna,
                new RnaDigestionParams(
                    rnase: "RNase T1",
                    maxMissedCleavages: 0,
                    minLength: 3,
                    fragmentationTerminus: FragmentationTerminus.Both),
                rnas.Cast<IBioPolymer>()),
        ];

        DigHistPlotBuilder builder = new();
        GenericChart proteinChart = builder.CreateProteinChart(results, "Manual Protein Digestion Analysis");
        GenericChart rnaChart = builder.CreateRnaChart(results, "Manual RNA Digestion Analysis");

        Plotly.NET.CSharp.GenericChartExtensions.Show(proteinChart);
        Plotly.NET.CSharp.GenericChartExtensions.Show(rnaChart);

        TestContext.WriteLine($"Protein entries loaded: {proteins.Count}");
        TestContext.WriteLine($"RNA entries loaded: {rnas.Count}");
        TestContext.WriteLine($"Cache directory: {cacheDirectory}");
    }

    private static void EnsureManualPathWasUpdated(string path, string parameterName)
    {
        if (path.Contains(@"C:\path\to\", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore($"Replace the placeholder value for {parameterName} before running this explicit helper test.");
        }
    }
}
