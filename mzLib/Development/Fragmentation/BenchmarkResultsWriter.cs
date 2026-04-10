using System.Diagnostics;
using System.Text;

namespace Development
{
    internal sealed record BenchmarkRecord
    {
        public string Timestamp              { get; init; } = "";
        public string Label                  { get; init; } = "";
        public string Branch                 { get; init; } = "";
        public int    Proteins               { get; init; }
        public int    Peptides               { get; init; }
        public double LoadElapsed_s          { get; init; }
        public double DigestionElapsed_s     { get; init; }
        public double DigestionPps           { get; init; }
        public double AvgPeptideLength       { get; init; }
        public double PeptidesWithMods_pct   { get; init; }
        public long   SerialFragments        { get; init; }
        public double AvgFragsPerPeptide     { get; init; }
        public double SerialElapsed_s        { get; init; }
        public double SerialPps              { get; init; }
        public double ParallelElapsed_s      { get; init; }
        public double ParallelPps            { get; init; }
        public int    LogicalProcessors      { get; init; }
        public double Speedup                { get; init; }
        public double ParallelEfficiency_pct { get; init; }

        // Column order must match Serialize / Deserialize exactly.
        internal static readonly string[] Columns =
        [
            "Timestamp", "Label", "Branch",
            "Proteins", "Peptides",
            "LoadElapsed_s", "DigestionElapsed_s", "DigestionPps",
            "AvgPeptideLength", "PeptidesWithMods_pct",
            "SerialFragments", "AvgFragsPerPeptide",
            "SerialElapsed_s", "SerialPps",
            "ParallelElapsed_s", "ParallelPps",
            "LogicalProcessors", "Speedup", "ParallelEfficiency_pct"
        ];

        internal string Serialize() => string.Join('\t',
            Timestamp, Label, Branch,
            Proteins, Peptides,
            F(LoadElapsed_s), F(DigestionElapsed_s), F(DigestionPps),
            F(AvgPeptideLength), F(PeptidesWithMods_pct),
            SerialFragments, F(AvgFragsPerPeptide),
            F(SerialElapsed_s), F(SerialPps),
            F(ParallelElapsed_s), F(ParallelPps),
            LogicalProcessors, F(Speedup), F(ParallelEfficiency_pct));

        internal static BenchmarkRecord? Deserialize(string line)
        {
            var f = line.Split('\t');
            if (f.Length != Columns.Length) return null;
            try
            {
                return new BenchmarkRecord
                {
                    Timestamp              = f[0],
                    Label                  = f[1],
                    Branch                 = f[2],
                    Proteins               = int.Parse(f[3]),
                    Peptides               = int.Parse(f[4]),
                    LoadElapsed_s          = double.Parse(f[5]),
                    DigestionElapsed_s     = double.Parse(f[6]),
                    DigestionPps           = double.Parse(f[7]),
                    AvgPeptideLength       = double.Parse(f[8]),
                    PeptidesWithMods_pct   = double.Parse(f[9]),
                    SerialFragments        = long.Parse(f[10]),
                    AvgFragsPerPeptide     = double.Parse(f[11]),
                    SerialElapsed_s        = double.Parse(f[12]),
                    SerialPps              = double.Parse(f[13]),
                    ParallelElapsed_s      = double.Parse(f[14]),
                    ParallelPps            = double.Parse(f[15]),
                    LogicalProcessors      = int.Parse(f[16]),
                    Speedup                = double.Parse(f[17]),
                    ParallelEfficiency_pct = double.Parse(f[18])
                };
            }
            catch { return null; }
        }

        private static string F(double v) => v.ToString("F3");
    }

    internal static class BenchmarkResultsWriter
    {
        private const string ResultsFileName = "BenchmarkResults.tsv";

        // Walks up from the binary output directory until it finds a directory
        // named "mzLib" that contains a "Development" subdirectory, then resolves
        // to Development/Fragmentation/BenchmarkResults.tsv inside that root.
        // Falls back to the binary directory if the source tree can't be located.
        internal static string DefaultPath
        {
            get
            {
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

                while (dir is not null)
                {
                    if (dir.Name.Equals("mzLib", StringComparison.OrdinalIgnoreCase)
                        && Directory.Exists(Path.Combine(dir.FullName, "Development")))
                    {
                        return Path.Combine(
                            dir.FullName, "Development", "Fragmentation", ResultsFileName);
                    }
                    dir = dir.Parent;
                }

                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ResultsFileName);
            }
        }

        // ?? Public API ???????????????????????????????????????????????????????

        /// <summary>Appends one row to the TSV, writing the header first if the file is new.</summary>
        internal static void Append(BenchmarkRecord record, string? path = null)
        {
            path ??= DefaultPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            bool needsHeader = !File.Exists(path);
            using var writer = new StreamWriter(path, append: true);
            if (needsHeader)
                writer.WriteLine(string.Join('\t', BenchmarkRecord.Columns));
            writer.WriteLine(record.Serialize());
        }

        /// <summary>Returns the last <paramref name="n"/> successfully parsed rows.</summary>
        internal static List<BenchmarkRecord> ReadLast(int n, string? path = null)
        {
            path ??= DefaultPath;
            if (!File.Exists(path)) return [];

            return File.ReadAllLines(path)
                       .Skip(1)                                          // skip header
                       .Where(l => !string.IsNullOrWhiteSpace(l))
                       .TakeLast(n)
                       .Select(BenchmarkRecord.Deserialize)
                       .Where(r => r is not null)
                       .ToList()!;
        }

        /// <summary>
        /// Formats a compact aligned comparison table from a list of records.
        /// Columns: Label | Branch | Timestamp | Digest_s | DigestPps | Serial_s | SerialPps | Parallel_s | ParallelPps | Speedup | Eff%
        /// </summary>
        internal static string FormatComparisonTable(IReadOnlyList<BenchmarkRecord> rows)
        {
            if (rows.Count == 0)
                return "  (no previous runs found)";

            // Dynamic widths so the table stays tight regardless of label/branch length
            int lw = Math.Max("Label".Length,  rows.Max(r => r.Label.Length));
            int bw = Math.Max("Branch".Length, rows.Max(r => r.Branch.Length));
            int tw = "yyyy-MM-dd HH:mm".Length;

            string Sep() => new string('-',
                lw + bw + tw + 9 * 10 + 10 * 2 + 4); // approx total width

            var sb = new StringBuilder();

            // Header
            sb.AppendLine($"  {"Label".PadRight(lw)}  {"Branch".PadRight(bw)}  {"Timestamp".PadRight(tw)}" +
                          $"  {"Digest_s",8}  {"DigestPps",10}" +
                          $"  {"Serial_s",8}  {"SerialPps",10}" +
                          $"  {"Para_s",8}  {"ParaPps",10}" +
                          $"  {"Speedup",7}  {"Eff%",5}");
            sb.AppendLine("  " + Sep());

            foreach (var r in rows)
            {
                sb.AppendLine(
                    $"  {r.Label.PadRight(lw)}  {r.Branch.PadRight(bw)}  {r.Timestamp.PadRight(tw)}" +
                    $"  {r.DigestionElapsed_s,8:F3}  {r.DigestionPps,10:N0}" +
                    $"  {r.SerialElapsed_s,8:F3}  {r.SerialPps,10:N0}" +
                    $"  {r.ParallelElapsed_s,8:F3}  {r.ParallelPps,10:N0}" +
                    $"  {r.Speedup,6:F2}x  {r.ParallelEfficiency_pct,4:F1}%");
            }

            sb.AppendLine("  " + Sep());
            return sb.ToString();
        }

        // ?? Helpers ??????????????????????????????????????????????????????????

        /// <summary>Returns the current git branch name, or "unknown" if it cannot be determined.</summary>
        internal static string GetGitBranch()
        {
            try
            {
                var psi = new ProcessStartInfo("git", "rev-parse --abbrev-ref HEAD")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };
                using var proc = Process.Start(psi)!;
                var branch = proc.StandardOutput.ReadToEnd().Trim();
                proc.WaitForExit();
                return string.IsNullOrEmpty(branch) ? "unknown" : branch;
            }
            catch { return "unknown"; }
        }
    }
}
