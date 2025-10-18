using CommandLine;
using Proteomics;
using Proteomics.ProteolyticDigestion;
using Readers.ExternalResults.ResultFiles;
using Readers.ExternalResults.IndividualResultRecords;
using UsefulProteomicsDatabases;
using System.Text;

namespace CasanovoMapper;

class Program
{
    static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<CommandLineOptions>(args)
            .MapResult(
                options => RunMapping(options),
                errors => 1);
    }

    static int RunMapping(CommandLineOptions options)
    {
        // Ensure output directory exists early so log file can be created there
        if (!string.IsNullOrWhiteSpace(options.OutputDirectory))
            Directory.CreateDirectory(options.OutputDirectory);

        // Create unique log file in output directory
        var logPath = GetUniqueFilePath(Path.Combine(options.OutputDirectory ?? string.Empty, "CasanovoMapper.log"));
        var logWriter = new StreamWriter(logPath) { AutoFlush = true };

        // Capture original console streams
        var originalOut = Console.Out;
        var originalErr = Console.Error;

        // Replace Console.Out/Error with a writer that duplicates to console + file
        Console.SetOut(new MultiTextWriter(originalOut, logWriter));
        Console.SetError(new MultiTextWriter(originalErr, logWriter));

        try
        {
            Console.WriteLine("CasanovoMapper - Mapping Casanovo results to protein databases");
            Console.WriteLine("================================================================");
            Console.WriteLine();

            // Validate inputs
            if (!ValidateInputs(options))
                return 1;

            // Load Casanovo files
            Console.WriteLine("  Loading Casanovo mzTab files...");
            var casanovoFiles = LoadCasanovoFiles(options);
            Console.WriteLine($"  Loaded {casanovoFiles.Count} file(s) with {casanovoFiles.Sum(f => f.Results.Count)} total PSMs");
            Console.WriteLine();

            // Prepare sorted records for efficient lookup
            Console.WriteLine("  Preparing records for mapping...");
            var casanovoMelted = casanovoFiles
                .SelectMany(p => p.Results.Select((rec, ind) => (Path.GetFileNameWithoutExtension(p.FilePath), rec, ind)))
                .OrderBy(p => p.rec.BaseSequence, StringComparer.Ordinal)
                .ToList();
            Console.WriteLine($"  Sorted {casanovoMelted.Count} records");
            Console.WriteLine();

            // Build lookup ranges
            var firstCharRanges = BuildFirstCharRanges(casanovoMelted);

            // Create digestion parameters
            var digParams = new DigestionParams(
                protease: options.Protease,
                maxMissedCleavages: options.MissedCleavages,
                minPeptideLength: options.MinPeptideLength,
                maxPeptideLength: options.MaxPeptideLength);

            Console.WriteLine($"  Digestion parameters: {options.Protease}, {options.MissedCleavages} missed cleavages, length {options.MinPeptideLength}-{options.MaxPeptideLength}");
            if (options.GenerateDecoys)
                Console.WriteLine($"  Decoy generation with prefix '{options.DecoyPrefix}'");
            Console.WriteLine();

            // Prepare for parallel processing
            var recLocks = new object[casanovoMelted.Count];
            for (int i = 0; i < recLocks.Length; i++)
                recLocks[i] = new object();

            // Track matched proteins for filtered FASTA output
            // For each protein, track whether target and/or decoy had matches
            var matchedProteins = options.WriteFilteredFasta
                ? new Dictionary<string, Dictionary<string, (MinimalProtein Protein, bool TargetMatched, bool DecoyMatched)>>()
                : null;
            var proteinLocks = options.WriteFilteredFasta
                ? new Dictionary<string, object>()
                : null;

            if (options.WriteFilteredFasta)
            {
                foreach (var dbPath in options.DatabasePaths)
                {
                    var dbName = Path.GetFileNameWithoutExtension(dbPath);
                    matchedProteins![dbName] = new Dictionary<string, (MinimalProtein, bool, bool)>();
                    proteinLocks![dbName] = new object();
                }
            }

            // Process each database
            foreach (var fastaPath in options.DatabasePaths)
            {
                Console.WriteLine($"  Processing database: {Path.GetFileName(fastaPath)}");
                var dbName = Path.GetFileNameWithoutExtension(fastaPath);
                int proteinsProcessed = 0;
                int matchesFound = 0;
                int targetMatches = 0;
                int decoyMatches = 0;

                foreach (var chunk in FastaStreamReader.ReadFastaChunks(fastaPath, options.ChunkSize))
                {
                    int total = chunk.Count;
                    int per = (total + options.Workers - 1) / options.Workers;

                    Parallel.For(0, options.Workers, workerIndex =>
                    {
                        int start = workerIndex * per;
                        int end = Math.Min(start + per, total);

                        for (int ci = start; ci < end; ci++)
                        {
                            var (header, sequence) = chunk[ci];
                            string[]? splits = null;
                            string? acc = null;

                            // Apply I->L replacement if needed
                            var processedSequence = options.ReplaceIWithL 
                                ? sequence.Replace('I', 'L') 
                                : sequence;

                            // Create minimal protein for target
                            splits = header.Split('|');
                            acc = splits.Length > 1 ? splits[1] : header;
                            var targetProtein = new MinimalProtein(acc, header, processedSequence);
                            
                            bool targetMatched = false;
                            bool decoyMatched = false;

                            // Check target peptides
                            foreach (var pepSeq in targetProtein.Digest(digParams))
                            {
                                char pepFirst = pepSeq[0];

                                if (!firstCharRanges.TryGetValue(pepFirst, out var range))
                                    continue;

                                int startIdx = LowerBound(casanovoMelted, pepSeq, range.start, range.end);
                                if (startIdx < 0)
                                    continue;
                                int endIdx = UpperBound(casanovoMelted, pepSeq, startIdx, range.end);

                                for (int i = startIdx; i <= endIdx; i++)
                                {
                                    var recTuple = casanovoMelted[i];
                                    if (StringComparer.Ordinal.Equals(recTuple.rec.BaseSequence, pepSeq))
                                    {
                                        lock (recLocks[i])
                                        {
                                            recTuple.rec.Accession = Join(acc, recTuple.rec.Accession);
                                            recTuple.rec.Database = Join(dbName, recTuple.rec.Database);
                                        }

                                        targetMatched = true;
                                        Interlocked.Increment(ref matchesFound);
                                        Interlocked.Increment(ref targetMatches);
                                    }
                                }
                            }

                            // Check decoy peptides if decoy generation is enabled
                            if (options.GenerateDecoys)
                            {
                                var decoyAcc = $"{options.DecoyPrefix}_{acc}";
                                
                                foreach (var pepSeq in targetProtein.GetDecoyPeptides(digParams))
                                {
                                    char pepFirst = pepSeq[0];

                                    if (!firstCharRanges.TryGetValue(pepFirst, out var range))
                                        continue;

                                    int startIdx = LowerBound(casanovoMelted, pepSeq, range.start, range.end);
                                    if (startIdx < 0)
                                        continue;
                                    int endIdx = UpperBound(casanovoMelted, pepSeq, startIdx, range.end);

                                    for (int i = startIdx; i <= endIdx; i++)
                                    {
                                        var recTuple = casanovoMelted[i];
                                        if (StringComparer.Ordinal.Equals(recTuple.rec.BaseSequence, pepSeq))
                                        {
                                            lock (recLocks[i])
                                            {
                                                recTuple.rec.Accession = Join(decoyAcc, recTuple.rec.Accession);
                                                recTuple.rec.Database = Join(dbName, recTuple.rec.Database);
                                                recTuple.rec.IsDecoy = true;
                                            }

                                            decoyMatched = true;
                                            Interlocked.Increment(ref matchesFound);
                                            Interlocked.Increment(ref decoyMatches);
                                        }
                                    }
                                }
                            }

                            // Store matched protein for FASTA output
                            if (matchedProteins != null && (targetMatched || decoyMatched))
                            {
                                lock (proteinLocks![dbName])
                                {
                                    if (matchedProteins[dbName].TryGetValue(header, out var existing))
                                    {
                                        // Update existing entry
                                        matchedProteins[dbName][header] = (
                                            existing.Protein,
                                            existing.TargetMatched || targetMatched,
                                            existing.DecoyMatched || decoyMatched
                                        );
                                    }
                                    else
                                    {
                                        // Add new entry
                                        matchedProteins[dbName][header] = (targetProtein, targetMatched, decoyMatched);
                                    }
                                }
                            }
                        }
                    });

                    proteinsProcessed += total;
                    if (proteinsProcessed % 10000 == 0)
                        Console.Write($"\r    Processed {proteinsProcessed:N0} proteins...");
                }

                Console.Write($"\r    Processed {proteinsProcessed:N0} proteins...");
                Console.WriteLine();
                
                if (options.GenerateDecoys)
                {
                    Console.WriteLine($"    Completed. Found {matchesFound:N0} peptide matches ({targetMatches:N0} target, {decoyMatches:N0} decoy)");
                }
                else
                {
                    Console.WriteLine($"    Completed. Found {matchesFound:N0} peptide matches");
                }
                Console.WriteLine();
            }

            // Write results
            Console.WriteLine("  Writing mapped results...");
            Directory.CreateDirectory(options.OutputDirectory);

            int totalMappedPSMs = 0;
            int totalDecoyPSMs = 0;

            foreach (var group in casanovoMelted.GroupBy(p => p.Item1))
            {
                var original = casanovoFiles.First(p => p.FilePath.Contains(group.Key));
                original.Results = group.OrderBy(p => p.ind).Select(p => p.rec).ToList();

                var outPath = Path.Combine(options.OutputDirectory,
                    Path.GetFileNameWithoutExtension(original.FilePath) + "_Mapped");
                original.WriteResults(outPath);

                // Count mapped and decoy PSMs
                var mappedPSMs = original.Results.Count(r => !string.IsNullOrWhiteSpace(r.Accession));
                var decoyPSMs = original.Results.Count(r => r.IsDecoy && !string.IsNullOrWhiteSpace(r.Accession));
                
                totalMappedPSMs += mappedPSMs;
                totalDecoyPSMs += decoyPSMs;

                if (options.GenerateDecoys)
                {
                    Console.WriteLine($"    Wrote {Path.GetFileName(outPath)}.mztab ({mappedPSMs:N0} mapped PSMs, {decoyPSMs:N0} decoy matches)");
                }
                else
                {
                    Console.WriteLine($"    Wrote {Path.GetFileName(outPath)}.mztab ({mappedPSMs:N0} mapped PSMs)");
                }
            }

            if (options.GenerateDecoys && totalMappedPSMs > 0)
            {
                double decoyRate = (double)totalDecoyPSMs / totalMappedPSMs * 100;
                Console.WriteLine($"  Overall decoy rate: {decoyRate:F1}% ({totalDecoyPSMs:N0}/{totalMappedPSMs:N0})");
            }

            Console.WriteLine();

            // Write filtered FASTA files if requested
            if (options.WriteFilteredFasta && matchedProteins != null)
            {
                Console.WriteLine("  Writing filtered FASTA files...");
                var producedFilteredFastas = new List<string>();
                
                foreach (var fastaPath in options.DatabasePaths)
                {
                    var dbName = Path.GetFileNameWithoutExtension(fastaPath);
                    var proteins = matchedProteins[dbName];

                    if (proteins.Count > 0)
                    {
                        var filteredFastaPath = Path.Combine(options.OutputDirectory,
                            options.GenerateDecoys ? $"{dbName}_TargetDecoy_Matched.fasta" : $"{dbName}_Matched.fasta");

                        int proteinCount = WriteMatchedFasta(filteredFastaPath, proteins, options);
                        
                        producedFilteredFastas.Add(filteredFastaPath);
                        var suffix = options.GenerateDecoys ? " entries (targets+decoys)" : " proteins";
                        Console.WriteLine($"    Wrote {proteinCount:N0}{suffix} to {Path.GetFileName(filteredFastaPath)}");
                    }
                    else
                    {
                        Console.WriteLine($"    No matches found in {dbName}, skipping FASTA output");
                    }
                }
                
                Console.WriteLine();
                
                // Combine all per-database filtered FASTAs into a single merged FASTA
                if (producedFilteredFastas.Count > 0)
                {
                    var combinedPath = Path.Combine(options.OutputDirectory, 
                        options.GenerateDecoys ? "Combined_TargetDecoy_Matched.fasta" : "Combined_Matched.fasta");
                    int merged = FastaStreamReader.CombineFastas(producedFilteredFastas, combinedPath);
                    Console.WriteLine($"  Combined {merged:N0} unique entries into {Path.GetFileName(combinedPath)}");
                }
            }

            Console.WriteLine("  Mapping completed successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
        finally
        {
            // Restore console outputs and dispose log writer
            try { Console.SetOut(originalOut); } catch { }
            try { Console.SetError(originalErr); } catch { }
            try { logWriter.Dispose(); } catch { }
        }
    }

    static bool ValidateInputs(CommandLineOptions options)
    {
        // Validate databases
        foreach (var db in options.DatabasePaths)
        {
            if (!File.Exists(db))
            {
                Console.Error.WriteLine($"Database file not found: {db}");
                return false;
            }
        }

        // Validate Casanovo files/directory
        if (!string.IsNullOrEmpty(options.CasanovoDirectory))
        {
            if (!Directory.Exists(options.CasanovoDirectory))
            {
                Console.Error.WriteLine($"Casanovo directory not found: {options.CasanovoDirectory}");
                return false;
            }
        }
        else if (options.CasanovoFiles != null)
        {
            foreach (var file in options.CasanovoFiles)
            {
                if (!File.Exists(file))
                {
                    Console.Error.WriteLine($"Casanovo file not found: {file}");
                    return false;
                }
            }
        }
        else
        {
            Console.Error.WriteLine("Must specify either --casanovo-files or --casanovo-directory");
            return false;
        }
        return true;
    }

    static List<CasanovoMzTabFile> LoadCasanovoFiles(CommandLineOptions options)
    {
        IEnumerable<string> filePaths;

        if (!string.IsNullOrEmpty(options.CasanovoDirectory))
        {
            filePaths = Directory.GetFiles(options.CasanovoDirectory, "*.mztab");
        }
        else
        {
            filePaths = options.CasanovoFiles!;
        }

        return filePaths.Select(p => new CasanovoMzTabFile(p, loadModInformation: false)).ToList();
    }

    static string Join(string toAdd, string? existing, char delim = '|')
    {
        if (string.IsNullOrWhiteSpace(existing))
            return toAdd;
        if (ContainsToken(existing, toAdd, delim))
            return existing;
        return existing + delim + toAdd;
    }

    static bool ContainsToken(string existing, string token, char delim)
    {
        int start = 0;
        while (true)
        {
            int idx = existing.IndexOf(delim, start);
            if (idx < 0)
            {
                int len = existing.Length - start;
                return EqualsSegment(existing, start, len, token);
            }
            else
            {
                int len = idx - start;
                if (EqualsSegment(existing, start, len, token))
                    return true;
                start = idx + 1;
            }
        }
    }

    static bool EqualsSegment(string s, int start, int length, string token)
    {
        return length == token.Length &&
               string.Compare(s, start, token, 0, length, StringComparison.Ordinal) == 0;
    }

    static Dictionary<char, (int start, int end)> BuildFirstCharRanges(
        List<(string FileKey, CasanovoMzTabRecord rec, int index)> sorted)
    {
        var ranges = new Dictionary<char, (int, int)>();
        if (sorted.Count == 0) return ranges;

        int i = 0;
        while (i < sorted.Count)
        {
            char c = sorted[i].rec.BaseSequence[0];
            int start = i;
            int j = i + 1;
            while (j < sorted.Count && sorted[j].rec.BaseSequence[0] == c) j++;
            ranges[c] = (start, j - 1);
            i = j;
        }
        return ranges;
    }

    static int LowerBound(
        List<(string FileKey, CasanovoMzTabRecord rec, int index)> sorted,
        string target,
        int lo,
        int hi)
    {
        int left = lo;
        int right = hi;
        int found = -1;

        while (left <= right)
        {
            int mid = left + ((right - left) >> 1);
            int cmp = string.CompareOrdinal(sorted[mid].rec.BaseSequence, target);

            if (cmp < 0)
            {
                left = mid + 1;
            }
            else if (cmp > 0)
            {
                right = mid - 1;
            }
            else
            {
                found = mid;
                right = mid - 1;
            }
        }
        return found;
    }

    static int UpperBound(
        List<(string FileKey, CasanovoMzTabRecord rec, int index)> sorted,
        string target,
        int lo,
        int hi)
    {
        int left = lo;
        int right = hi;
        int found = lo - 1;

        while (left <= right)
        {
            int mid = left + ((right - left) >> 1);
            int cmp = string.CompareOrdinal(sorted[mid].rec.BaseSequence, target);

            if (cmp <= 0)
            {
                if (cmp == 0) found = mid;
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }
        return found;
    }


    /// <summary>
    /// TextWriter that writes to two underlying writers.
    /// </summary>
    internal class MultiTextWriter : TextWriter
    {
        private readonly TextWriter _a;
        private readonly TextWriter _b;

        public MultiTextWriter(TextWriter a, TextWriter b)
        {
            _a = a ?? throw new ArgumentNullException(nameof(a));
            _b = b ?? throw new ArgumentNullException(nameof(b));
        }

        public override Encoding Encoding => _a.Encoding;

        public override void Write(char value)
        {
            _a.Write(value);
            _b.Write(value);
        }

        public override void Write(string? value)
        {
            _a.Write(value);
            _b.Write(value);
        }

        public override void WriteLine(string? value)
        {
            _a.WriteLine(value);
            _b.WriteLine(value);
        }

        public override void Flush()
        {
            _a.Flush();
            _b.Flush();
        }
    }

    internal static string GetUniqueFilePath(string desiredPath)
    {
        if (string.IsNullOrWhiteSpace(desiredPath))
            throw new ArgumentException("Invalid path", nameof(desiredPath));

        var dir = Path.GetDirectoryName(desiredPath) ?? ".";
        var file = Path.GetFileNameWithoutExtension(desiredPath);
        var ext = Path.GetExtension(desiredPath);
        var candidate = Path.Combine(dir, file + ext);
        int i = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(dir, $"{file}_{i}{ext}");
            i++;
        }
        return candidate;
    }

    /// <summary>
    /// Writes matched proteins to a FASTA file. Only writes targets/decoys that had peptide matches.
    /// Decoys are generated on-demand from target sequences.
    /// </summary>
    static int WriteMatchedFasta(string outputPath, Dictionary<string, (MinimalProtein Protein, bool TargetMatched, bool DecoyMatched)> matchedProteins, CommandLineOptions options)
    {
        int written = 0;
        using var writer = new StreamWriter(outputPath);

        foreach (var (protein, targetMatched, decoyMatched) in matchedProteins.Values)
        {
            // Write target if it had matches
            if (targetMatched)
            {
                writer.WriteLine(">" + protein.Header);
                WriteSequenceInLines(writer, protein.Sequence);
                written++;
            }

            // Write decoy only if it had matches AND decoy generation is enabled
            if (options.GenerateDecoys && decoyMatched)
            {
                var decoyAccession = $"{options.DecoyPrefix}_{protein.Accession}";
                var decoyHeader = protein.Header.Replace(protein.Accession, decoyAccession);
                var decoySequence = protein.GetDecoySequence();
                
                writer.WriteLine(">" + decoyHeader);
                WriteSequenceInLines(writer, decoySequence);
                written++;
            }
        }

        return written;
    }

    /// <summary>
    /// Writes a protein sequence in 60-character lines (standard FASTA format)
    /// </summary>
    static void WriteSequenceInLines(StreamWriter writer, string sequence)
    {
        for (int i = 0; i < sequence.Length; i += 60)
        {
            int length = Math.Min(60, sequence.Length - i);
            writer.WriteLine(sequence.Substring(i, length));
        }
    }
}