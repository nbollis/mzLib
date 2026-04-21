using Chemistry;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using MassSpectrometry;
using Omics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Test.Transcriptomics.PAPER_OneOffs
{
    public sealed class AverageResidueModelCache
    {
        private const string RecordsFileName = "records.tsv";
        private const string MetadataFileName = "metadata.json";
        private const int SchemaVersion = 1;

        private readonly AverageResidue _model;
        private readonly string _modelName;
        private readonly string _modelFolder;
        private readonly string _recordsPath;
        private readonly string _metadataPath;
        private readonly AverageResidueCacheRecord[] _records;

        public AverageResidueModelCache(string rootFolder, AverageResidue model, string modelName = null)
        {
            _model = model;
            _modelName = string.IsNullOrWhiteSpace(modelName) ? model.GetType().Name : modelName;
            _modelFolder = Path.Combine(rootFolder, _modelName);
            _recordsPath = Path.Combine(_modelFolder, RecordsFileName);
            _metadataPath = Path.Combine(_modelFolder, MetadataFileName);

            Directory.CreateDirectory(_modelFolder);
            _records = File.Exists(_recordsPath) ? LoadRecords(_recordsPath) : CreateModelRecords();
            Save();
        }

        public string ModelName => _modelName;
        public int FoundCount => _records.Count(p => p.HasObservation);
        public int MissingCount => _records.Length - FoundCount;
        public IReadOnlyList<AverageResidueCacheRecord> Records => _records;

        public CacheUpdateStats UpdateFromBioPolymers(IEnumerable<IBioPolymerWithSetMods> bioPolymers, int maxPrecursorsToScan, string sourceTag)
        {
            int scanned = 0;
            int eligible = 0;
            int updated = 0;
            int skippedFilled = 0;
            int maxModelIndex = _records.Length - 1;

            foreach (var bioPolymer in bioPolymers)
            {
                scanned++;
                if (scanned > maxPrecursorsToScan)
                {
                    break;
                }

                double monoMass = bioPolymer.MonoisotopicMass;
                if (!double.IsFinite(monoMass) || monoMass <= 0)
                {
                    continue;
                }

                eligible++;
                int modelIndex = Math.Clamp(_model.GetMostIntenseMassIndex(monoMass), 0, maxModelIndex);
                var record = _records[modelIndex];
                if (record.HasObservation)
                {
                    skippedFilled++;
                    continue;
                }

                var formula = bioPolymer.ThisChemicalFormula;
                if (formula is null)
                {
                    continue;
                }

                var experimental = GetExperimentalApexFromFormula(formula);
                if (!experimental.Ok)
                {
                    continue;
                }

                record.HasObservation = true;
                record.Sequence = bioPolymer.BaseSequence;
                record.Length = bioPolymer.Length;
                record.ChemicalFormula = formula.Formula;
                record.ExpMonoMass = monoMass;
                record.ExpMostIntenseMass = experimental.ExpMostIntenseMass;
                record.ExpDiffToMono = experimental.ExpDiffToMono;
                record.DeltaDa = record.TheoDiffToMono - experimental.ExpDiffToMono;
                record.DeltaPpm = experimental.ExpMostIntenseMass == 0
                    ? null
                    : 1e6 * record.DeltaDa / experimental.ExpMostIntenseMass;
                record.SourceTag = sourceTag;
                record.LastUpdatedUtc = DateTime.UtcNow;
                updated++;

                if (MissingCount == 0)
                {
                    break;
                }
            }

            if (updated > 0)
            {
                Save();
            }

            return new CacheUpdateStats(scanned, eligible, updated, skippedFilled, FoundCount, MissingCount);
        }

        private AverageResidueCacheRecord[] CreateModelRecords()
        {
            int maxModelIndex = GetMaxModelIndex(_model);
            var records = new AverageResidueCacheRecord[maxModelIndex + 1];

            for (int index = 0; index <= maxModelIndex; index++)
            {
                double theoMostIntenseMass = _model.GetAllTheoreticalMasses(index)[0];
                double theoDiffToMono = _model.GetDiffToMonoisotopic(index);
                records[index] = new AverageResidueCacheRecord
                {
                    ModelIndex = index,
                    TheoMostIntenseMass = theoMostIntenseMass,
                    TheoDiffToMono = theoDiffToMono,
                    TheoMonoMass = theoMostIntenseMass - theoDiffToMono,
                    LastUpdatedUtc = DateTime.UtcNow,
                };
            }

            return records;
        }

        private void Save()
        {
            SaveRecords();
            SaveMetadata();
        }

        private void SaveRecords()
        {
            string tempPath = _recordsPath + ".tmp";
            using (var csv = new CsvWriter(new StreamWriter(File.Create(tempPath)), AverageResidueCacheRecord.CsvConfiguration))
            {
                csv.WriteHeader<AverageResidueCacheRecord>();
                foreach (var record in _records.OrderBy(p => p.ModelIndex))
                {
                    csv.NextRecord();
                    csv.WriteRecord(record);
                }
            }

            if (File.Exists(_recordsPath))
            {
                File.Delete(_recordsPath);
            }

            File.Move(tempPath, _recordsPath);
        }

        private void SaveMetadata()
        {
            string tempPath = _metadataPath + ".tmp";
            var metadata = new CacheMetadata
            {
                SchemaVersion = SchemaVersion,
                ModelName = _modelName,
                RecordCount = _records.Length,
                FoundCount = FoundCount,
                MissingCount = MissingCount,
                UpdatedUtc = DateTime.UtcNow,
            };

            File.WriteAllText(tempPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
            if (File.Exists(_metadataPath))
            {
                File.Delete(_metadataPath);
            }

            File.Move(tempPath, _metadataPath);
        }

        private AverageResidueCacheRecord[] LoadRecords(string path)
        {
            var records = CreateModelRecords();
            using var csv = new CsvReader(new StreamReader(path), AverageResidueCacheRecord.CsvConfiguration);
            foreach (var parsed in csv.GetRecords<AverageResidueCacheRecord>())
            {
                if (parsed.ModelIndex < 0 || parsed.ModelIndex >= records.Length)
                {
                    continue;
                }

                records[parsed.ModelIndex] = parsed;
            }

            return records;
        }

        private static int GetMaxModelIndex(AverageResidue model)
        {
            return model switch
            {
                Averagine => Averagine.DiffToMonoisotopic.Length - 1,
                OxyriboAveragine => OxyriboAveragine.DiffToMonoisotopic.Length - 1,
                _ => 1499,
            };
        }

        private static (bool Ok, double ExpMostIntenseMass, double ExpDiffToMono) GetExperimentalApexFromFormula(ChemicalFormula formula)
        {
            var distribution = IsotopicDistribution.GetDistribution(formula);
            var intensities = distribution.Intensities;
            var masses = distribution.Masses;

            if (intensities.Length == 0 || masses.Length == 0)
            {
                return (false, 0, 0);
            }

            int maxIndex = 0;
            double maxIntensity = intensities[0];
            for (int i = 1; i < intensities.Length; i++)
            {
                if (intensities[i] > maxIntensity)
                {
                    maxIntensity = intensities[i];
                    maxIndex = i;
                }
            }

            double expMostIntenseMass = masses[maxIndex];
            double expDiffToMono = expMostIntenseMass - formula.MonoisotopicMass;
            return (true, expMostIntenseMass, expDiffToMono);
        }

        private sealed class CacheMetadata
        {
            public int SchemaVersion { get; set; }
            public string ModelName { get; set; }
            public int RecordCount { get; set; }
            public int FoundCount { get; set; }
            public int MissingCount { get; set; }
            public DateTime UpdatedUtc { get; set; }
        }
    }

    public sealed class AverageResidueCacheRecord
    {
        [Ignore]
        public static CsvConfiguration CsvConfiguration => new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = "\t",
            MissingFieldFound = null,
            HeaderValidated = null,
        };

        [Name("ModelIndex")]
        public int ModelIndex { get; set; }

        [Name("TheoMonoMass")]
        public double TheoMonoMass { get; set; }

        [Name("TheoMostIntenseMass")]
        public double TheoMostIntenseMass { get; set; }

        [Name("TheoDiffToMono")]
        public double TheoDiffToMono { get; set; }

        [Name("HasObservation")]
        public bool HasObservation { get; set; }

        [Name("Sequence")]
        [Optional]
        public string Sequence { get; set; }

        [Name("Length")]
        [Optional]
        public int? Length { get; set; }

        [Name("ChemicalFormula")]
        [Optional]
        public string ChemicalFormula { get; set; }

        [Name("ExpMonoMass")]
        [Optional]
        public double? ExpMonoMass { get; set; }

        [Name("ExpMostIntenseMass")]
        [Optional]
        public double? ExpMostIntenseMass { get; set; }

        [Name("ExpDiffToMono")]
        [Optional]
        public double? ExpDiffToMono { get; set; }

        [Name("DeltaDa")]
        [Optional]
        public double? DeltaDa { get; set; }

        [Name("DeltaPpm")]
        [Optional]
        public double? DeltaPpm { get; set; }

        [Name("SourceTag")]
        [Optional]
        public string SourceTag { get; set; }

        [Name("LastUpdatedUtc")]
        [Optional]
        public DateTime? LastUpdatedUtc { get; set; }
    }

    public readonly record struct CacheUpdateStats(
        int ScannedCount,
        int EligibleCount,
        int UpdatedCount,
        int SkippedAlreadyFilledCount,
        int FoundCount,
        int MissingCount);
}
