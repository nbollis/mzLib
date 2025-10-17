using System.Collections.Generic;
using System.IO;

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
                    string sequence = replaceIWithL
                        ? sequenceBuilder.ToString().Replace('I', 'L')
                        : sequenceBuilder.ToString();

                    yield return (currentHeader, sequence);
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
            string sequence = replaceIWithL
                ? sequenceBuilder.ToString().Replace('I', 'L')
                : sequenceBuilder.ToString();

            yield return (currentHeader, sequence);
        }
    }

    /// <summary>
    /// Streams fasta entries in fixed-size chunks. Each yielded list contains up to <paramref name="chunkSize"/> protein entries.
    /// This preserves streaming semantics (the file is read lazily) while allowing callers to process chunks in parallel.
    /// Note: the returned enumerable is an iterator that keeps the underlying file open until enumeration completes or is disposed.
    /// </summary>
    /// <param name="filePath">Path to fasta file.</param>
    /// <param name="replaceIWithL">Whether to replace 'I' with 'L' in sequences.</param>
    /// <param name="chunkSize">Maximum number of proteins per yielded chunk (must be &gt; 0).</param>
    public static IEnumerable<List<(string Header, string Sequence)>> ReadFastaChunks(string filePath, bool replaceIWithL, int chunkSize)
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
                    string sequence = replaceIWithL
                        ? sequenceBuilder.ToString().Replace('I', 'L')
                        : sequenceBuilder.ToString();

                    chunk.Add((currentHeader, sequence));
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
            string sequence = replaceIWithL
                ? sequenceBuilder.ToString().Replace('I', 'L')
                : sequenceBuilder.ToString();
            chunk.Add((currentHeader, sequence));
        }

        if (chunk.Count > 0)
            yield return chunk;
    }
}