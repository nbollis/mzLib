using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UsefulProteomicsDatabases;

public class FastaStreamReader 
{
    public static IEnumerable<(string Header, string Sequence)> ReadFasta(string filePath, bool replaceIWithL)
    {
        using var reader = new StreamReader(filePath);
        string? line;
        string? currentHeader = null;
        var sequenceBuilder = new System.Text.StringBuilder();
        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith(">"))
            {
                if (currentHeader != null)
                {
                    yield return (currentHeader, sequenceBuilder.ToString());
                    sequenceBuilder.Clear();
                }
                currentHeader = line.Substring(1).Trim();
            }
            else
            {
                sequenceBuilder.Append(line.Trim());
            }
        }
        if (currentHeader != null)
        {
            yield return (currentHeader, sequenceBuilder.ToString());
        }
    }

    /// <summary>
    /// Streams fasta entries in fixed-size chunks. Each yielded list contains up to <paramref name="chunkSize"/> protein entries.
    /// This preserves streaming semantics (the file is read lazily) while allowing callers to process chunks in parallel.
    /// Note: the returned enumerable is an iterator that keeps the underlying file open until enumeration completes or is disposed.
    /// </summary>
    /// <param name="filePath">Path to fasta file.</param>
    /// <param name="chunkSize">Maximum number of proteins per yielded chunk (must be &gt; 0).</param>
    public static IEnumerable<List<(string Header, string Sequence)>> ReadFastaChunks(string filePath, int chunkSize)
    {
        if (chunkSize <= 0) throw new System.ArgumentOutOfRangeException(nameof(chunkSize));

        using var reader = new StreamReader(filePath);
        string? line;
        string? currentHeader = null;
        var sequenceBuilder = new System.Text.StringBuilder();
        var chunk = new List<(string Header, string Sequence)>(chunkSize);

        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith(">"))
            {
                if (currentHeader != null)
                {
                    chunk.Add((currentHeader, sequenceBuilder.ToString()));
                    sequenceBuilder.Clear();

                    if (chunk.Count >= chunkSize)
                    {
                        yield return chunk;
                        chunk = new List<(string Header, string Sequence)>(chunkSize);
                    }
                }
                currentHeader = line.Substring(1).Trim();
            }
            else
            {
                sequenceBuilder.Append(line.Trim());
            }
        }

        if (currentHeader != null)
        {
            chunk.Add((currentHeader, sequenceBuilder.ToString()));
        }

        if (chunk.Count > 0)
            yield return chunk;
    }

    /// <summary>
    /// Writes a filtered FASTA file containing only proteins whose headers match the provided set.
    /// This method streams through the input file and writes matching entries, preserving memory efficiency.
    /// </summary>
    /// <param name="inputFilePath">Path to the input FASTA file.</param>
    /// <param name="outputFilePath">Path where the filtered FASTA will be written.</param>
    /// <param name="headersToInclude">Set of protein headers to include (as they appear after '&gt;' in the FASTA file).</param>
    /// <returns>Number of proteins written to the output file.</returns>
    public static (int Written, int Total) WriteFilteredFasta(string inputFilePath, string outputFilePath, HashSet<string> headersToInclude)
    {
        if (headersToInclude == null || headersToInclude.Count == 0)
            return (0, 0);

        int proteinCount = 0;
        int proteinTotal = 0;
        using var reader = new StreamReader(inputFilePath);
        using var writer = new StreamWriter(outputFilePath);

        string? line;
        string? currentHeader = null;
        var sequenceBuilder = new System.Text.StringBuilder();
        bool includeCurrentProtein = false;

        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith(">"))
            {
                // Write previous protein if it should be included
                if (includeCurrentProtein && currentHeader != null)
                {
                    writer.WriteLine(">" + currentHeader);
                    string sequence = sequenceBuilder.ToString();
                    
                    // Write sequence in 60-character lines (standard FASTA format)
                    for (int i = 0; i < sequence.Length; i += 60)
                    {
                        int length = Math.Min(60, sequence.Length - i);
                        writer.WriteLine(sequence.Substring(i, length));
                    }
                    
                    proteinCount++;
                }

                // Start new protein
                proteinTotal++;
                currentHeader = line.Substring(1).Trim();
                sequenceBuilder.Clear();
                includeCurrentProtein = headersToInclude.Contains(currentHeader);
            }
            else if (includeCurrentProtein)
            {
                sequenceBuilder.Append(line.Trim());
            }
        }

        // Write last protein if it should be included
        if (includeCurrentProtein && currentHeader != null)
        {
            writer.WriteLine(">" + currentHeader);
            string sequence = sequenceBuilder.ToString();
            
            for (int i = 0; i < sequence.Length; i += 60)
            {
                int length = Math.Min(60, sequence.Length - i);
                writer.WriteLine(sequence.Substring(i, length));
            }
            
            proteinCount++;
        }

        return (proteinCount, proteinTotal);
    }

    /// <summary>
    /// Combine multiple FASTA files into a single FASTA, skipping duplicate headers.
    /// Streams input files and writes unique entries to the output file to preserve memory efficiency.
    /// </summary>
    /// <param name="inputFastaPaths">Enumerable of input FASTA file paths (order preserved).</param>
    /// <param name="outputFilePath">Path to write the combined FASTA.</param>
    /// <returns>Number of unique proteins written to the combined FASTA.</returns>
    public static int CombineFastas(IEnumerable<string> inputFastaPaths, string outputFilePath)
    {
        var seen = new HashSet<string>();
        int written = 0;

        using var writer = new StreamWriter(outputFilePath);

        foreach (var input in inputFastaPaths)
        {
            if (!File.Exists(input))
                continue;

            using var reader = new StreamReader(input);
            string? line;
            string? currentHeader = null;
            var seqBuilder = new System.Text.StringBuilder();
            bool includeCurrent = false;

            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith(">"))
                {
                    if (includeCurrent && currentHeader != null)
                    {
                        writer.WriteLine(">" + currentHeader);
                        var seq = seqBuilder.ToString();
                        for (int i = 0; i < seq.Length; i += 60)
                        {
                            int len = Math.Min(60, seq.Length - i);
                            writer.WriteLine(seq.Substring(i, len));
                        }
                        written++;
                    }

                    currentHeader = line.Substring(1).Trim();
                    seqBuilder.Clear();
                    includeCurrent = !seen.Contains(currentHeader);
                    if (includeCurrent)
                        seen.Add(currentHeader);
                }
                else if (includeCurrent)
                {
                    seqBuilder.Append(line.Trim());
                }
            }

            if (includeCurrent && currentHeader != null)
            {
                writer.WriteLine(">" + currentHeader);
                var seq = seqBuilder.ToString();
                for (int i = 0; i < seq.Length; i += 60)
                {
                    int len = Math.Min(60, seq.Length - i);
                    writer.WriteLine(seq.Substring(i, len));
                }
                written++;
            }
        }

        return written;
    }

    /// <summary>
    /// Writes a filtered target-decoy FASTA file containing only proteins whose headers match the provided set.
    /// Includes both target proteins and their corresponding decoys if decoys are generated.
    /// </summary>
    /// <param name="inputFilePath">Path to the input FASTA file.</param>
    /// <param name="outputFilePath">Path where the filtered FASTA will be written.</param>
    /// <param name="headersToInclude">Set of protein headers to include (as they appear after '>' in the FASTA file).</param>
    /// <param name="includeDecoys">Whether to generate and include decoy proteins.</param>
    /// <param name="decoyType">Type of decoy to generate (only used if includeDecoys is true).</param>
    /// <param name="decoyPrefix">Prefix for decoy protein accessions.</param>
    /// <param name="maxThreads">Maximum number of threads for decoy generation.</param>
    /// <returns>Tuple containing (proteins written, total proteins processed).</returns>
    public static (int Written, int Total) WriteFilteredTargetDecoyFasta(string inputFilePath, string outputFilePath, 
        HashSet<string> headersToInclude, bool includeDecoys = false, DecoyType decoyType = DecoyType.Reverse, 
        string decoyPrefix = "DECOY_", int maxThreads = -1)
    {
        if (headersToInclude == null || headersToInclude.Count == 0)
            return (0, 0);

        int proteinCount = 0;
        int proteinTotal = 0;
        var matchedProteins = new List<(string Header, string Sequence)>();

        // First pass: collect matched proteins
        using (var reader = new StreamReader(inputFilePath))
        {
            string? line;
            string? currentHeader = null;
            var sequenceBuilder = new System.Text.StringBuilder();
            bool includeCurrentProtein = false;

            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith(">"))
                {
                    // Save previous protein if it should be included
                    if (includeCurrentProtein && currentHeader != null)
                    {
                        matchedProteins.Add((currentHeader, sequenceBuilder.ToString()));
                    }

                    // Start new protein
                    proteinTotal++;
                    currentHeader = line.Substring(1).Trim();
                    sequenceBuilder.Clear();
                    includeCurrentProtein = headersToInclude.Contains(currentHeader);
                }
                else if (includeCurrentProtein)
                {
                    sequenceBuilder.Append(line.Trim());
                }
            }

            // Handle last protein
            if (includeCurrentProtein && currentHeader != null)
            {
                matchedProteins.Add((currentHeader, sequenceBuilder.ToString()));
            }
        }

        // Generate decoys if requested
        var allProteinsToWrite = new List<(string Header, string Sequence)>(matchedProteins);
        if (includeDecoys && matchedProteins.Count > 0)
        {
            var proteins = matchedProteins.Select(p => new Proteomics.Protein(p.Sequence, p.Header)).ToList();
            var decoys = DecoyProteinGenerator.GenerateDecoys(proteins, decoyType, maxThreads, decoyPrefix.TrimEnd('_'));
            
            foreach (var decoy in decoys)
            {
                allProteinsToWrite.Add((decoy.Accession, decoy.BaseSequence));
            }
        }

        // Write all proteins to output
        using (var writer = new StreamWriter(outputFilePath))
        {
            foreach (var (header, sequence) in allProteinsToWrite)
            {
                writer.WriteLine(">" + header);
                
                // Write sequence in 60-character lines (standard FASTA format)
                for (int i = 0; i < sequence.Length; i += 60)
                {
                    int length = Math.Min(60, sequence.Length - i);
                    writer.WriteLine(sequence.Substring(i, length));
                }
                
                proteinCount++;
            }
        }

        return (proteinCount, proteinTotal);
    }
}