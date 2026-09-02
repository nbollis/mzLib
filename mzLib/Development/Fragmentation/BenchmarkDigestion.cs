using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using MassSpectrometry;
using Omics.Fragmentation;
using Omics.Modifications;
using Proteomics.ProteolyticDigestion;
using Transcriptomics;
using Transcriptomics.Digestion;
using UsefulProteomicsDatabases;
using UsefulProteomicsDatabases.Transcriptomics;

namespace Development.Fragmentation;

/// <summary>
/// Benchmarks the fragmentation-only cost of the IFragmentable/IFragmentationParams refactor.
/// All substrate construction (protein loading + digestion, RNA loading + digestion) happens in
/// GlobalSetup, off-clock; the timed methods only call Fragment(...).
/// </summary>
/// <remarks>
/// Uses a minimal exporter config so artifacts are just summary tables (Mean/StdDev) rather than
/// the default per-measurement CSVs and R plots.
/// </remarks>
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
[Config(typeof(MinimalArtifactsConfig))]
[HideColumns("Job", "RntmId", "WarmupCount", "LaunchCount", "TargetCount", "RatioSD")]
public class BenchmarkDigestion
{
    private class MinimalArtifactsConfig : ManualConfig
    {
        public MinimalArtifactsConfig()
        {
            // Summary-only exports: one row per method (Mean/StdDev/Allocated), no per-measurement files.
            AddExporter(CsvExporter.Default);
            AddExporter(MarkdownExporter.GitHub);
            AddLogger(ConsoleLogger.Default);
            AddColumnProvider(DefaultColumnProviders.Instance);
            Options |= ConfigOptions.DisableLogFile;
        }
    }

    // ── Inputs ────────────────────────────────────────────────────────────────────
    // Protein database path (local, human proteome with heavy variable mods).
    internal const string DefaultProteinDbPath = @"D:\Proteomes\uniprotkb_human_proteome_AND_reviewed_t_2024_03_22.xml";
    // RNA databases housed in the test project, copied next to the Development output.
    internal const string RnaTestDataDir = @"RnaTestData";
    internal const string RnaEnsembl = @"TestDatabase_Ensembl.GRCh38.ncrna.fa";
    internal const string RnaModomics = @"ModomicsUnmodifiedTrimmed.fasta";

    // Cap the RNA corpus so setup stays tractable; override via environment variable.
    internal const int DefaultRnaSequenceCap = 2000;
    internal static int RnaSequenceCap =>
        int.TryParse(Environment.GetEnvironmentVariable("MZLIB_BENCH_RNA_CAP"), out var cap) ? cap : DefaultRnaSequenceCap;

    // Cap the protein corpus the same way (the human proteome defaults to the full set).
    internal const int DefaultProteinCap = int.MaxValue;
    internal static int ProteinCap =>
        int.TryParse(Environment.GetEnvironmentVariable("MZLIB_BENCH_PROTEIN_CAP"), out var cap) ? cap : DefaultProteinCap;

    private IReadOnlyList<OligoWithSetMods> _rnaOligos;
    private IReadOnlyList<PeptideWithSetModifications> _peptides;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _peptides = BuildPeptideCorpus();
        _rnaOligos = BuildRnaCorpus().Take(RnaSequenceCap).ToList();

        // Warm up: trigger JIT and lazy mod/static initialization before timing.
        if (_peptides.Count == 0 || _rnaOligos.Count == 0)
            throw new InvalidOperationException("Benchmark corpus is empty; check data paths and caps.");

        var warmupProducts = new List<Product>();
        _peptides[0].Fragment(DissociationType.HCD, FragmentationTerminus.Both, warmupProducts);
        _rnaOligos[0].Fragment(DissociationType.CID, FragmentationTerminus.Both, warmupProducts);
    }

    private static IReadOnlyList<PeptideWithSetModifications> BuildPeptideCorpus()
    {
        var dbPath = Environment.GetEnvironmentVariable("MZLIB_BENCH_DB_FILE") ?? DefaultProteinDbPath;
        var proteins = ProteinDbLoader.LoadProteinXML(dbPath, generateTargets: true, DecoyType.None,
            Mods.AllKnownMods, false, null, out _, maxHeterozygousVariants: 0);
        var digestionParams = new DigestionParams();

        return proteins.Take(ProteinCap)
            .SelectMany(p => p.Digest(digestionParams, new List<Modification>(), new List<Modification>()))
            .ToList();
    }

    private static IEnumerable<OligoWithSetMods> BuildRnaCorpus()
    {
        var testDataRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, RnaTestDataDir));
        foreach (var fasta in new[] { RnaEnsembl, RnaModomics })
        {
            var path = Path.Combine(testDataRoot, fasta);
            if (!File.Exists(path))
                continue;

            var rnas = RnaDbLoader.LoadRnaFasta(path, generateTargets: true, DecoyType.None, false, out _);
            foreach (var rna in rnas)
            {
                foreach (var oligo in rna.Digest(new RnaDigestionParams(), new List<Modification>(), new List<Modification>()))
                {
                    yield return oligo;
                }
            }
        }
    }

    [Benchmark]
    public int FragmentProteins()
    {
        var products = new List<Product>();
        foreach (var peptide in _peptides)
        {
            peptide.Fragment(DissociationType.HCD, FragmentationTerminus.Both, products);
        }

        return products.Count;
    }

    [Benchmark]
    public int FragmentRna()
    {
        var products = new List<Product>();
        foreach (var oligo in _rnaOligos)
        {
            oligo.Fragment(DissociationType.CID, FragmentationTerminus.Both, products);
        }

        return products.Count;
    }
}