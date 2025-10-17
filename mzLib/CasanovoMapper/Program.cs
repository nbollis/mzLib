using CommandLine;
using Proteomics;
using Proteomics.ProteolyticDigestion;
using Readers.ExternalResults.ResultFiles;
using Readers.ExternalResults.IndividualResultRecords;
using UsefulProteomicsDatabases;

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
        try
        {
            Console.WriteLine("CasanovoMapper - Mapping Casanovo results to protein databases");
            Console.WriteLine("================================================================");
            Console.WriteLine();

            // Validate inputs
            if (!ValidateInputs(options))
                return 1;

            // Load Casanovo files
            Console.WriteLine("Loading Casanovo mzTab files...");
            var casanovoFiles = LoadCasanovoFiles(options);
            Console.WriteLine($"Loaded {casanovoFiles.Count} file(s) with {casanovoFiles.Sum(f => f.Results.Count)} total PSMs");
            Console.WriteLine();

            // Prepare sorted records for efficient lookup
            Console.WriteLine("Preparing records for mapping...");
            var casanovoMelted = casanovoFiles
                .SelectMany(p => p.Results.Select((rec, ind) => (Path.GetFileNameWithoutExtension(p.FilePath), rec, ind)))
                .OrderBy(p => p.rec.BaseSequence, StringComparer.Ordinal)
                .ToList();
            Console.WriteLine($"Sorted {casanovoMelted.Count} records");
            Console.WriteLine();

            // Build lookup ranges
            var firstCharRanges = BuildFirstCharRanges(casanovoMelted);

            // Create digestion parameters
            var digParams = new DigestionParams(
                protease: options.Protease,
                maxMissedCleavages: options.MissedCleavages,
                minPeptideLength: options.MinPeptideLength,
                maxPeptideLength: options.MaxPeptideLength);

            Console.WriteLine($"Digestion parameters: {options.Protease}, {options.MissedCleavages} missed cleavages, length {options.MinPeptideLength}-{options.MaxPeptideLength}");
            Console.WriteLine();

            // Prepare for parallel processing
            var recLocks = new object[casanovoMelted.Count];
            for (int i = 0; i < recLocks.Length; i++)
                recLocks[i] = new object();

            // Process each database
            foreach (var fastaPath in options.DatabasePaths)
            {
                Console.WriteLine($"Processing database: {Path.GetFileName(fastaPath)}");
                var dbName = Path.GetFileNameWithoutExtension(fastaPath);
                int proteinsProcessed = 0;
                int matchesFound = 0;

                foreach (var chunk in FastaStreamReader.ReadFastaChunks(fastaPath, options.ReplaceIWithL, options.ChunkSize))
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

                            foreach (var pep in new Protein(sequence, "").Digest(digParams, [], []))
                            {
                                var pepSeq = pep.BaseSequence;
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
                                        splits ??= header.Split('|');
                                        acc ??= splits.Length > 1 ? splits[1] : header;

                                        lock (recLocks[i])
                                        {
                                            recTuple.rec.Accession = Join(acc, recTuple.rec.Accession);
                                            recTuple.rec.Database = Join(dbName, recTuple.rec.Database);
                                        }
                                        
                                        Interlocked.Increment(ref matchesFound);
                                    }
                                }
                            }
                        }
                    });

                    proteinsProcessed += total;
                    if (proteinsProcessed % 10000 == 0)
                        Console.WriteLine($"  Processed {proteinsProcessed:N0} proteins...");
                }

                Console.WriteLine($"  Completed. Found {matchesFound:N0} peptide matches");
                Console.WriteLine();
            }

            // Write results
            Console.WriteLine("Writing mapped results...");
            Directory.CreateDirectory(options.OutputDirectory);

            foreach (var group in casanovoMelted.GroupBy(p => p.Item1))
            {
                var original = casanovoFiles.First(p => p.FilePath.Contains(group.Key));
                original.Results = group.OrderBy(p => p.ind).Select(p => p.rec).ToList();
                
                var outPath = Path.Combine(options.OutputDirectory, 
                    Path.GetFileNameWithoutExtension(original.FilePath) + "_Mapped");
                original.WriteResults(outPath);
                
                Console.WriteLine($"  Wrote {Path.GetFileName(outPath)}.mztab");
            }

            Console.WriteLine();
            Console.WriteLine("Mapping completed successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
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
}
