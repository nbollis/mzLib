using System;
using System.Collections.Generic;
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
using UsefulProteomicsDatabases;

namespace Development.Fragmentation;

/// <summary>
/// Benchmarks the fragmentation-only cost of the IFragmentable/IFragmentationParams refactor.
/// All substrate construction (protein loading + digestion) happens in GlobalSetup, off-clock;
/// the timed methods only call Fragment(...).
/// </summary>
/// <remarks>
/// Timing-only run: memory tracking is disabled and iterations are bounded so a 1000-protein
/// corpus (bottom-up digestion yields tens of thousands of peptides) stays tractable.
/// </remarks>
[SimpleJob(RuntimeMoniker.Net10_0, launchCount: 1, warmupCount: 4, iterationCount: 20)]
[Config(typeof(MinimalArtifactsConfig))]
[HideColumns("Job", "RntmId", "LaunchCount", "WarmupCount", "IterationCount", "Gen0", "Gen1", "Gen2", "Allocated", "RatioSD")]
public class BenchmarkDigestion
{
    private class MinimalArtifactsConfig : ManualConfig
    {
        public MinimalArtifactsConfig()
        {
            // Summary-only exports: one row per method (Mean/StdDev), no per-measurement files.
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

    // Cap the protein corpus so setup stays tractable; override via environment variable.
    internal const int DefaultProteinCap = int.MaxValue;
    internal static int ProteinCap =>
        int.TryParse(Environment.GetEnvironmentVariable("MZLIB_BENCH_PROTEIN_CAP"), out var cap) ? cap : DefaultProteinCap;

    private IReadOnlyList<PeptideWithSetModifications> _peptides;
    private IReadOnlyList<PeptideWithSetModifications> _proteins;

    // Fixed fragmentation parameters so the loops match production search settings and the
    // newest fragment-mass cutoffs (MaximumFragmentMassDa) are actually exercised.
    private static readonly FragmentationParams FragmentationParameters = new()
    {
        DissociationType = DissociationType.HCD,
        FragmentationTerminus = FragmentationTerminus.Both,
        MaximumFragmentMassDa = 30000,
    };

    [GlobalSetup]
    public void GlobalSetup()
    {
        _peptides = BuildPeptideCorpus();
        _proteins = BuildProteinCorpus();

        // Warm up: trigger JIT and lazy mod/static initialization before timing.
        if (_peptides.Count == 0 && _proteins.Count == 0)
            throw new InvalidOperationException("Benchmark corpus is empty; check data paths and caps.");

        var warmupProducts = new List<Product>();
        if (_peptides.Count > 0)
            _peptides[0].Fragment(FragmentationParameters, ref warmupProducts);
        if (_proteins.Count > 0)
            _proteins[0].Fragment(FragmentationParameters, ref warmupProducts);
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

    private static IReadOnlyList<PeptideWithSetModifications> BuildProteinCorpus()
    {
        var dbPath = Environment.GetEnvironmentVariable("MZLIB_BENCH_DB_FILE") ?? DefaultProteinDbPath;
        var proteins = ProteinDbLoader.LoadProteinXML(dbPath, generateTargets: true, DecoyType.None,
            Mods.AllKnownMods, false, null, out _, maxHeterozygousVariants: 0);
        var digestionParams = new DigestionParams("top-down");

        return proteins.Take(ProteinCap)
            .SelectMany(p => p.Digest(digestionParams, new List<Modification>(), new List<Modification>()))
            .ToList();
    }

    [Benchmark]
    public int FragmentPeptides()
    {
        var products = new List<Product>();
        foreach (var peptide in _peptides)
        {
            peptide.Fragment(FragmentationParameters, ref products);
        }

        return products.Count;
    }

    [Benchmark]
    public int FragmentProteins()
    {
        var products = new List<Product>();
        foreach (var peptide in _proteins)
        {
            peptide.Fragment(FragmentationParameters, ref products);
        }

        return products.Count;
    }
}