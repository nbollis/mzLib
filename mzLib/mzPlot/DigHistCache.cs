using CsvHelper;
using CsvHelper.Configuration;
using Omics;
using Omics.Digestion;
using Omics.Fragmentation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace mzPlot;

public sealed class DigHistCache
{
    public DigHistCache(string cacheDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheDirectoryPath);
        CacheDirectoryPath = cacheDirectoryPath;
    }

    public string CacheDirectoryPath { get; }

    public DigHistResult GetOrCreate(string sourceId, DigestionPolymerType polymerType, IDigestionParams digestionParams,
        IEnumerable<IBioPolymer> bioPolymers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentNullException.ThrowIfNull(digestionParams);
        ArgumentNullException.ThrowIfNull(bioPolymers);

        List<IBioPolymer> bioPolymerList = bioPolymers.ToList();
        string cacheFilePath = GetCacheFilePath(sourceId, polymerType, digestionParams, bioPolymerList);

        if (TryLoad(cacheFilePath, out DigHistResult cachedResult))
        {
            return cachedResult;
        }

        DigHistResult computedResult = new DigHist(digestionParams, bioPolymerList).Run(sourceId, polymerType);
        Save(cacheFilePath, computedResult);
        return computedResult;
    }

    public string GetCacheFilePath(string sourceId, DigestionPolymerType polymerType, IDigestionParams digestionParams,
        IEnumerable<IBioPolymer> bioPolymers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentNullException.ThrowIfNull(digestionParams);
        ArgumentNullException.ThrowIfNull(bioPolymers);

        List<IBioPolymer> bioPolymerList = bioPolymers.ToList();

        string sourceDirectory = SanitizePathSegment(sourceId);
        string digestionSignature = ComputeSha256Hex(GetDigestionSignature(digestionParams));
        string sourceSignature = ComputeSourceSignature(polymerType, bioPolymerList);
        string fileName = $"{digestionSignature}_{sourceSignature}.csv";

        return Path.Combine(CacheDirectoryPath, polymerType.ToString().ToLowerInvariant(), sourceDirectory, fileName);
    }

    public bool TryLoad(string cacheFilePath, out DigHistResult result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheFilePath);

        result = null;
        if (!File.Exists(cacheFilePath))
        {
            return false;
        }

        using StreamReader reader = new(cacheFilePath);
        using CsvReader csv = new(reader, DigHistCsvRecord.CsvConfiguration);

        List<DigHistCsvRecord> records = csv.GetRecords<DigHistCsvRecord>().ToList();
        if (records.Count == 0)
        {
            return false;
        }

        DigHistCsvRecord metadata = records[0];
        Dictionary<int, int> histogram = records
            .Where(record => record.HasLengthBin)
            .ToDictionary(record => record.Length, record => record.Count);

        result = new DigHistResult(
            metadata.SourceId,
            Enum.Parse<DigestionPolymerType>(metadata.PolymerType, ignoreCase: true),
            metadata.DigestionAgentName,
            metadata.MaxMissedCleavages,
            metadata.MinLength,
            metadata.MaxLength,
            Enum.Parse<FragmentationTerminus>(metadata.FragmentationTerminus, ignoreCase: true),
            Enum.Parse<CleavageSpecificity>(metadata.SearchModeType, ignoreCase: true),
            metadata.BioPolymersCount,
            metadata.DigestedProductsCount,
            metadata.UniqueProductsCount,
            histogram);

        return true;
    }

    public void Save(string cacheFilePath, DigHistResult result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheFilePath);
        ArgumentNullException.ThrowIfNull(result);

        string directoryPath = Path.GetDirectoryName(cacheFilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        List<DigHistCsvRecord> records = result.DigestionLengthHistogram.Count > 0
            ? result.DigestionLengthHistogram.Select(bin => DigHistCsvRecord.FromResult(result, true, bin.Key, bin.Value)).ToList()
            : [DigHistCsvRecord.FromResult(result, false, 0, 0)];

        using StreamWriter writer = new(cacheFilePath, false, Encoding.UTF8);
        using CsvWriter csv = new(writer, DigHistCsvRecord.CsvConfiguration);
        csv.WriteRecords(records);
    }

    private static string ComputeSourceSignature(DigestionPolymerType polymerType, List<IBioPolymer> bioPolymers)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        AppendHashText(hash, polymerType.ToString());
        AppendHashText(hash, bioPolymers.Count.ToString(CultureInfo.InvariantCulture));

        foreach (IBioPolymer bioPolymer in bioPolymers.OrderBy(p => p.Accession).ThenBy(p => p.FullName).ThenBy(p => p.BaseSequence))
        {
            AppendHashText(hash, bioPolymer.Accession);
            AppendHashText(hash, bioPolymer.FullName);
            AppendHashText(hash, bioPolymer.BaseSequence);
            AppendHashText(hash, bioPolymer.DatabaseFilePath);
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static string GetDigestionSignature(IDigestionParams digestionParams)
    {
        StringBuilder builder = new();
        builder.Append(digestionParams.GetType().FullName);

        foreach (var property in digestionParams.GetType().GetProperties().Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                     .OrderBy(p => p.Name))
        {
            object value = property.GetValue(digestionParams);
            string stringValue = value switch
            {
                DigestionAgent digestionAgent => digestionAgent.Name,
                null => string.Empty,
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.ToString() ?? string.Empty,
            };

            builder.Append('|');
            builder.Append(property.Name);
            builder.Append('=');
            builder.Append(stringValue);
        }

        return builder.ToString();
    }

    private static string ComputeSha256Hex(string text)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash);
    }

    private static void AppendHashText(IncrementalHash hash, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        hash.AppendData(bytes);
        hash.AppendData([0]);
    }

    private static string SanitizePathSegment(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (char character in value)
        {
            builder.Append(Path.GetInvalidFileNameChars().Contains(character) ? '_' : character);
        }

        return builder.ToString();
    }

    private sealed class DigHistCsvRecord
    {
        public static CsvConfiguration CsvConfiguration => new(CultureInfo.InvariantCulture)
        {
            Encoding = Encoding.UTF8,
            HasHeaderRecord = true,
        };

        public string SourceId { get; set; } = string.Empty;
        public string PolymerType { get; set; } = string.Empty;
        public string DigestionAgentName { get; set; } = string.Empty;
        public int MaxMissedCleavages { get; set; }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public string FragmentationTerminus { get; set; } = string.Empty;
        public string SearchModeType { get; set; } = string.Empty;
        public int BioPolymersCount { get; set; }
        public int DigestedProductsCount { get; set; }
        public int UniqueProductsCount { get; set; }
        public bool HasLengthBin { get; set; }
        public int Length { get; set; }
        public int Count { get; set; }

        public static DigHistCsvRecord FromResult(DigHistResult result, bool hasLengthBin, int length, int count)
        {
            return new DigHistCsvRecord
            {
                SourceId = result.SourceId,
                PolymerType = result.PolymerType.ToString(),
                DigestionAgentName = result.DigestionAgentName,
                MaxMissedCleavages = result.MaxMissedCleavages,
                MinLength = result.MinLength,
                MaxLength = result.MaxLength,
                FragmentationTerminus = result.FragmentationTerminus.ToString(),
                SearchModeType = result.SearchModeType.ToString(),
                BioPolymersCount = result.BioPolymersCount,
                DigestedProductsCount = result.DigestedProductsCount,
                UniqueProductsCount = result.UniqueProductsCount,
                HasLengthBin = hasLengthBin,
                Length = length,
                Count = count,
            };
        }
    }
}
