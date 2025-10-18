using CommandLine;

namespace CasanovoMapper;

public class CommandLineOptions
{
    [Option('d', "databases", Required = true, Separator = ',', HelpText = "Comma-separated list of FASTA database file paths.")]
    public IEnumerable<string> DatabasePaths { get; set; } = null!;

    [Option('f', "casanovo-files", Separator = ',', SetName = "files", HelpText = "Comma-separated list of Casanovo mzTab file paths.")]
    public IEnumerable<string>? CasanovoFiles { get; set; }

    [Option('i', "casanovo-directory", SetName = "directory", HelpText = "Directory containing Casanovo mzTab files (*.mztab).")]
    public string? CasanovoDirectory { get; set; }

    [Option('o', "output", Required = true, HelpText = "Output directory for mapped results.")]
    public string OutputDirectory { get; set; } = null!;

    [Option('p', "protease", Default = "trypsin", HelpText = "Protease used for digestion (e.g., trypsin, lysc, argc).")]
    public string Protease { get; set; } = "trypsin";

    [Option('m', "missed-cleavages", Default = 2, HelpText = "Maximum number of missed cleavages allowed.")]
    public int MissedCleavages { get; set; } = 2;

    [Option("min-length", Default = 7, HelpText = "Minimum peptide length for digestion.")]
    public int MinPeptideLength { get; set; } = 7;

    [Option("max-length", Default = int.MaxValue, HelpText = "Maximum peptide length for digestion.")]
    public int MaxPeptideLength { get; set; } = int.MaxValue;

    [Option("replace-i-with-l", Default = true, HelpText = "Replace isoleucine (I) with leucine (L) in FASTA sequences (Casanovo convention).")]
    public bool ReplaceIWithL { get; set; } = true;

    [Option("chunk-size", Default = 1000, HelpText = "Number of proteins to process per chunk (for memory efficiency).")]
    public int ChunkSize { get; set; } = 1000;

    [Option("workers", Default = 10, HelpText = "Number of parallel worker threads.")]
    public int Workers { get; set; } = 10;

    [Option("write-filtered-fasta", Default = true, HelpText = "Write a filtered FASTA file containing only proteins with mapped peptides.")]
    public bool WriteFilteredFasta { get; set; } = true;

    [Option("generate-decoys", Default = false, HelpText = "Generate decoy proteins and include them in mapping and output FASTA files.")]
    public bool GenerateDecoys { get; set; } = false;

    [Option("decoy-prefix", Default = "DECOY", HelpText = "Prefix for decoy protein accessions (default: DECOY).")]
    public string DecoyPrefix { get; set; } = "DECOY";
}
