﻿using CsvHelper.Configuration.Attributes;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;

namespace Readers
{
    /// <summary>
    /// A class representing a single entry in a ms2.feature file
    /// For supported versions and software this file type can come from see
    ///     Readers.ExternalResources.SupportedVersions.txt
    /// </summary>
    public class Ms2Feature : IResult
    {
        [Ignore]
        public static CsvConfiguration CsvConfiguration => new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Encoding = Encoding.UTF8,
            HasHeaderRecord = true,
            Delimiter = "\t",
        };

        [Name("Spec_ID")]
        public int SpectraId { get; set; }

        [Name("Fraction_ID")]
        public int FractionId { get; set; }

        [Name("File_name")]
        public string FileName { get; set; }

        [Name("Scans")]
        public int Scans { get; set; }

        [Name("MS_one_ID")]
        public int Ms1Id { get; set; }

        [Name("MS_one_scans")]
        public int Ms1Scans { get; set; }

        [Name("Precursor_mass")]
        public double PrecursorMass { get; set; }

        [Name("Precursor_intensity")]
        public double PrecursorIntensity { get; set; }

        [Name("Fraction_feature_ID")]
        public int FractionFeatureId { get; set; }

        [Name("Fraction_feature_intensity")]
        public double FractionFeatureIntensity { get; set; }

        [Name("Fraction_feature_score")]
        public double FractionFeatureScore { get; set; }

        [Name("Fraction_feature_time_apex")]
        public double FractionFeatureApex { get; set; }

        [Name("Sample_feature_ID")]
        public int SampleFeatureId { get; set; }

        [Name("Sample_feature_intensity")]
        public double SampleFeatureIntensity { get; set; }
    }
}