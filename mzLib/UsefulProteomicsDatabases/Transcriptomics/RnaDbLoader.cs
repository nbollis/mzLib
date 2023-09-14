using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transcriptomics;
using Easy.Common;
using System.Text.RegularExpressions;

namespace UsefulProteomicsDatabases.Transcriptomics
{
    public enum RnaFastaHeaderType
    {
        Modomics,
        Unknown,
    }

    public static class RnaDbLoader
    {
        public static readonly FastaHeaderFieldRegex ModomicsIdRegex = new FastaHeaderFieldRegex("Id", "", 0, 1);
        public static readonly FastaHeaderFieldRegex ModomicsNameRegex = new FastaHeaderFieldRegex("Name", @"Name:(?<Name>.+?)\|", 0, 1);
        public static readonly FastaHeaderFieldRegex ModomicsSOtermRegex = new FastaHeaderFieldRegex("SOterm", @"SOterm:(?<SOterm>.+?)\|", 0, 1);
        public static readonly FastaHeaderFieldRegex ModomicsRNAtypeRegex = new FastaHeaderFieldRegex("RNAtype", @"Type:(?<Type>.+?)\|", 0, 1);
        public static readonly FastaHeaderFieldRegex ModomicsRNAsubtypeRegex = new FastaHeaderFieldRegex("RNAsubtype", @"Subtype:(?<Subtype>.+?)\|", 0, 1);
        public static readonly FastaHeaderFieldRegex ModomicsRNAfeatureRegex = new FastaHeaderFieldRegex("RNAfeature", @"Feature:(?<Feature>.+?)\|", 0, 1);
        public static readonly FastaHeaderFieldRegex ModomicsOrganismRegex = new FastaHeaderFieldRegex("Organism", @"Species:(?<Species>.+?)\|", 0, 1);
        public static readonly FastaHeaderFieldRegex ModomicsCellularLocalizationRegex = new FastaHeaderFieldRegex("CellularLocalization", @"Cellular_Localization:(?<Cellular_Localization>.+?)\R", 0, 1);

        public static List<RNA> LoadRnaFasta(string rnaDbLocation, bool generateTargets, DecoyType decoyType,
            bool isContaminant, out List<string> errors)
        {
            RnaFastaHeaderType? headerType = null;
            Regex substituteWhitespace = new Regex(@"\s+");
            errors = new List<string>();
            List<RNA> targets = new List<RNA>();

            string id;
            string name;
            string soTerm;
            string rnaType;
            string rnaSubType;
            string rnaFeature;
            string organism;
            string cellularLocalization;
            FastaHeaderFieldRegex idRegex = null;
            FastaHeaderFieldRegex nameRegex = null;
            FastaHeaderFieldRegex soTermRegex = null;
            FastaHeaderFieldRegex rnaTypeRegex = null;
            FastaHeaderFieldRegex rnaSubTypeRegex = null;
            FastaHeaderFieldRegex rnaFeatureRegex = null;
            FastaHeaderFieldRegex organismRegex = null;
            FastaHeaderFieldRegex cellularLocalizationRegex = null;

            string newDbLocation = rnaDbLocation;

            //we had trouble decompressing and streaming on the fly so we decompress completely first, then stream the file, then delete the decompressed file
            if (rnaDbLocation.EndsWith(".gz"))
            {
                newDbLocation = Path.Combine(Path.GetDirectoryName(rnaDbLocation), "temp.fasta");
                using var stream = new FileStream(rnaDbLocation, FileMode.Open, FileAccess.Read, FileShare.Read);
                using FileStream outputFileStream = File.Create(newDbLocation);
                using var decompressor = new GZipStream(stream, CompressionMode.Decompress);
                decompressor.CopyTo(outputFileStream);
            }

            using (var fastaFileStream = new FileStream(newDbLocation, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                StringBuilder sb = null;
                StreamReader fasta = new StreamReader(fastaFileStream);

                while (true)
                {
                    string line = "";
                    line = fasta.ReadLine();
                    if (line == null) { break; }


                    if (line.StartsWith(">"))
                    {
                        if (headerType is null)
                        {
                            headerType = DetectFastaHeaderType(line);

                            switch (headerType)
                            {
                                case RnaFastaHeaderType.Modomics:
                                    idRegex = ModomicsIdRegex;
                                    nameRegex = ModomicsNameRegex;
                                    soTermRegex = ModomicsSOtermRegex;
                                    rnaTypeRegex = ModomicsRNAtypeRegex;
                                    rnaSubTypeRegex = ModomicsRNAsubtypeRegex;
                                    rnaFeatureRegex = ModomicsRNAfeatureRegex;
                                    organismRegex = ModomicsOrganismRegex;
                                    cellularLocalizationRegex = ModomicsCellularLocalizationRegex;
                                    break;

                                case RnaFastaHeaderType.Unknown:
                                case null:
                                default:
                                    throw new MzLibUtil.MzLibException("Unknown fasta header format: " + line);
                            }
                        }

                        id = ProteinDbLoader.ApplyRegex(idRegex, line);
                        name = ProteinDbLoader.ApplyRegex(nameRegex, line);
                        soTerm = ProteinDbLoader.ApplyRegex(soTermRegex, line);
                        rnaType = ProteinDbLoader.ApplyRegex(rnaTypeRegex, line);
                        rnaSubType = ProteinDbLoader.ApplyRegex(rnaSubTypeRegex, line);
                        rnaFeature = ProteinDbLoader.ApplyRegex(rnaFeatureRegex, line);
                        organism = ProteinDbLoader.ApplyRegex(organismRegex, line);
                        cellularLocalization = ProteinDbLoader.ApplyRegex(cellularLocalizationRegex, line);

                        sb = new StringBuilder();
                    }
                    else if (sb is not null)
                    {
                        sb.Append(line.Trim());
                    }

                    if ((fasta.Peek() == '>' || fasta.Peek() == -1) /*&& accession != null*/ && sb != null)
                    {
                        string sequence = substituteWhitespace.Replace(sb.ToString(), "");

                        // Do we need to sanitize the sequence? 

                        RNA rna = new Rna()
                    }

                    // no input left
                    if (fasta.Peek() == -1)
                    {
                        break;
                    }
                }
            }


            throw new NotImplementedException();
        }

        private static RnaFastaHeaderType DetectFastaHeaderType(string line)
        {

            // modomics -> >id:1|Name:tdbR00000010|SOterm:SO:0000254

            return RnaFastaHeaderType.Modomics;
        }

    
    }
}
