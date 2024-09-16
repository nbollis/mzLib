#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Transcriptomics;
using Easy.Common;
using System.Text.RegularExpressions;
using Chemistry;
using Easy.Common.Extensions;
using MzLibUtil;

namespace UsefulProteomicsDatabases.Transcriptomics
{
    public enum RnaFastaHeaderType
    {
        Modomics,
        Unknown,
        Ensemble,
        NCBIassembly,
        NCBIRefSeqGene,
        NCBIRefSeqRNA,
        NCBISRA,
        NCBIPD,
    }

    public static class RnaDbLoader
    {
        /// <summary>
        /// Dictionary that extract accession number, species, name, and additional dataField of modomics
        /// </summary>
        public static readonly Dictionary<string, IFastaHeaderFieldRegex> ModomicsFieldRegexes =
            new Dictionary<string, IFastaHeaderFieldRegex>()
            {
                { "Id", new FastaHeaderFieldRegex("Id", @"id:(?<id>.+?)\|", 0, 1) },
                { "Name", new FastaHeaderFieldRegex("Name", @"Name:(?<Name>.+?)\|", 0, 1) },
                { "SOterm", new FastaHeaderFieldRegex("SOterm", @"SOterm:(?<SOterm>.+?)\|", 0, 1) },
                { "Type", new FastaHeaderFieldRegex("Type", @"Type:(?<Type>.+?)\|", 0, 1) },
                { "Subtype", new FastaHeaderFieldRegex("Subtype", @"Subtype:(?<Subtype>.+?)\|", 0, 1) },
                { "Feature", new FastaHeaderFieldRegex("Feature", @"Feature:(?<Feature>.+?)\|", 0, 1) },
                { "Organism", new FastaHeaderFieldRegex("Organism", @"Species:(?<Species>.+?)$", 0, 1) },
                { "Cellular Localization", new FastaHeaderFieldRegex("CellularLocalization", @"Cellular_Localization:(?<Cellular_Localization>.+?)\|", 0, 1) },
            };

        /// <summary>
        /// Dictionary that extract accession number, species, name, and additional dataField of ensemble
        /// </summary>
        public static readonly Dictionary<string, IFastaHeaderFieldRegex> EnsembleFieldRegexes =
            new Dictionary<string, IFastaHeaderFieldRegex>()
            {
                { "ENST", new FastaHeaderFieldRegex("ENST", @"(ENST\d+\.\d+)", 0, 1) },
                { "Name", new FastaHeaderMultiFieldRegex("Name", @"gene:(ENSG\d+\.\d+).*description:([^[]+)", 0, new[]{1,2}) }, 
                { "Organism", new FastaHeaderFieldRegex("Organism", "", 0, 1, "N/A") },
                { "ncrna scaffold", new FastaHeaderFieldRegex("ncrna scaffold", @"ncrna scaffold:(?<scaffold>.+?)\ ", 0, 1) },
                //{ "gene", new FastaHeaderFieldRegex("gene", @"gene:(?<gene>.+?)\ ", 0, 1) },
                { "gene_biotype", new FastaHeaderFieldRegex("gene_biotype", @"gene_biotype:(?<gene_biotype>.+?)\ ", 0, 1) },
                { "transcript_biotype", new FastaHeaderFieldRegex("transcript_biotype", @"transcript_biotype:(?<transcript_biotype>.+?)\ ", 0, 1)},
                { "gene_symbol", new FastaHeaderFieldRegex("gene_symbol", @"gene_symbol:(?<gene_symbol>.+?)\ ", 0, 1) },
                //{ "description", new FastaHeaderFieldRegex("description", @"description:([^[]+)\ ", 0, 1) },
                { "RNA_Source", new FastaHeaderFieldRegex("RNA_Source", @"Source:(?<Source>.+?)\;", 0, 1) },
                { "RNA_Acc", new FastaHeaderFieldRegex("RNA_Acc", @"Acc:(?<Acc>.+?)\]", 0, 1) },
            };

        /// <summary>
        /// Dictionary that extract accession number, species, name, and additional dataField of NCBIassembly
        /// </summary>
        public static readonly Dictionary<string, IFastaHeaderFieldRegex> NCBIassemblyFieldRegexes =
            new Dictionary<string, IFastaHeaderFieldRegex>()
            {
                { "NM", new FastaHeaderFieldRegex("NM", @"(NM_\d+\.\d+)", 0, 1) },
                { "Name", new FastaHeaderFieldRegex("Name", @"(\b[A-Z][a-z]+ [a-z]+\b ([^,]+))", 0, 2) },
                { "Organism", new FastaHeaderFieldRegex("Organism", @"(\b[A-Z][a-z]+ [a-z]+\b)", 0, 1) },
                { "description", new FastaHeaderFieldRegex("description", @"(,\s*(.+))", 0, 2) },
            };

        /// <summary>
        /// Dictionary that extract accession number, species, name, and additional dataField of NCBIRefSeqGene
        /// </summary>
        public static readonly Dictionary<string, IFastaHeaderFieldRegex> NCBIRefSeqGeneRegexes =
            new Dictionary<string, IFastaHeaderFieldRegex>()
            {
                { "NC", new FastaHeaderFieldRegex("NC", @"(NC_\d+\.\d+)", 0, 1) },
                { "Name", new FastaHeaderFieldRegex("Name", @"(:(.*?)[ ]\[)", 0, 2) },
                { "Organism", new FastaHeaderFieldRegex("Organism", @"(organism=(.*?)])", 0, 2) },
                { "GeneID", new FastaHeaderFieldRegex("GeneID", @"(GeneID=(.*?)])", 0, 2) },
                { "chromosome", new FastaHeaderFieldRegex("chromosome", @"(chromosome=(.*?)])", 0, 2) },
            };

        /// <summary>
        /// Dictionary that extract accession number, species, name, and additional dataField of NCBIRefSeqRNA
        /// </summary>
        public static readonly Dictionary<string, IFastaHeaderFieldRegex> NCBIRefSeqRNARegexes =
            new Dictionary<string, IFastaHeaderFieldRegex>()
            {
                { "NM", new FastaHeaderFieldRegex("NM", @"(NM_\d+\.\d+)", 0, 1) },
                { "Name", new FastaHeaderFieldRegex("Name", @"(^\S+\s(.*?)(?=\s\[organism))", 0, 2) },
                { "Organism", new FastaHeaderFieldRegex("Organism", @"(organism=(.*?)])", 0, 2) },
                { "GeneID", new FastaHeaderFieldRegex("GeneID", @"(GeneID=(.*?)])", 0, 2) },
            };

        /// <summary>
        /// Dictionary that extract accession number, species, name, and additional dataField of NCBISRA
        /// </summary>
        public static readonly Dictionary<string, IFastaHeaderFieldRegex> NCBISRARegexes =
            new Dictionary<string, IFastaHeaderFieldRegex>()
            {
                { "SRR", new FastaHeaderFieldRegex("SRR", @"(@SRR\d+\.\d+\.\d+)", 0, 1) },
                { "Name", new FastaHeaderFieldRegex("Name", @"@[^ ]+ (\S+)", 0, 1) },
                { "Organism", new FastaHeaderFieldRegex("Organism", "", 0, 1, "N/A") },
            };

        /// <summary>
        /// Dictionary that extract accession number, species, name, and additional dataField of NCBIPD
        /// </summary>
        public static readonly Dictionary<string, IFastaHeaderFieldRegex> NCBIPDRegexes =
            new Dictionary<string, IFastaHeaderFieldRegex>()
            {
                { "PDT", new FastaHeaderFieldRegex("PDT", @"(PDT\d+\.\d+)", 0, 1) },
                { "Name", new FastaHeaderFieldRegex("Name", @"(PDT\d+\.\d+)", 0, 1) },
                { "Organism", new FastaHeaderFieldRegex("Organism", "", 0, 1, "N/A") },
            };

        //public static List<RNA> LoadRnaFasta_generic(string rnaDbLocation, bool generateTargets, DecoyType decoyType,
        //    bool isContaminant, out List<string> errors, IHasChemicalFormula? fivePrimeTerm = null,
        //    IHasChemicalFormula? threePrimeTerm = null)
        //{


        //}

        /// <summary>
        /// Loads an RNA file from the specified location, optionally generating decoys and adding error tracking
        /// </summary>
        /// <param name="rnaDbLocation">The file path to the RNA FASTA database</param>
        /// <param name="generateTargets">Flag indicating whether to generate targets or not</param>
        /// <param name="decoyType">The type of decoy generation to apply</param>
        /// <param name="isContaminant">Indicates if the RNA sequence is a contaminant</param>
        /// <param name="errors">Outputs any errors encountered during the process</param>
        /// <param name="fivePrimeTerm">An optional 5' prime chemical modification term</param>
        /// <param name="threePrimeTerm">An optional 3' prime chemical modification term</param>
        /// <returns>A list of RNA sequences loaded from the FASTA database</returns>
        /// <exception cref="MzLibUtil.MzLibException">Thrown if the FASTA header format is unknown or other issues occur during loading.</exception>
        public static List<RNA> LoadRnaFasta(string rnaDbLocation, bool generateTargets, DecoyType decoyType,
            bool isContaminant, out List<string> errors, IHasChemicalFormula? fivePrimeTerm = null, IHasChemicalFormula? threePrimeTerm = null)
        {
            RnaFastaHeaderType? headerType = null;
            Regex substituteWhitespace = new Regex(@"\s+");
            errors = new List<string>();
            List<RNA> targets = new List<RNA>();
            string identifierHeader = null;

            string name = null;
            string organism = null;
            string identifier = null;
            
            string newDbLocation = rnaDbLocation;

            //we had trouble decompressing and streaming on the fly so we decompress completely first, then stream the file, then delete the decompressed file
            if (rnaDbLocation.EndsWith(".gz")|| rnaDbLocation.EndsWith(".zip"))
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
                Dictionary<string, string> regexResults = new();
                Dictionary<string, IFastaHeaderFieldRegex> regexes = null;

                while (true)
                {
                    string line = "";
                    line = fasta.ReadLine();
                    if (line == null) { break; }
                    if (line.StartsWith(">")||line.StartsWith("@")||line.StartsWith("PDT"))
                        //char.IsDigit(line.First()))
                    {
                        if (headerType is null)
                        {
                            headerType = DetectFastaHeaderType(line);

                            switch (headerType)
                            {
                                case RnaFastaHeaderType.Modomics:
                                    regexes = ModomicsFieldRegexes;
                                    identifierHeader = "SOterm";
                                    break;
                                case RnaFastaHeaderType.Ensemble:
                                    regexes = EnsembleFieldRegexes;
                                    identifierHeader = "RNA_Acc";
                                    break;
                                case RnaFastaHeaderType.NCBIassembly:
                                    regexes = NCBIassemblyFieldRegexes;
                                    identifierHeader = "NM";
                                    break;
                                case RnaFastaHeaderType.NCBIRefSeqGene:
                                    regexes = NCBIRefSeqGeneRegexes;
                                    identifierHeader = "NC";
                                    break;
                                case RnaFastaHeaderType.NCBIRefSeqRNA:
                                    regexes = NCBIRefSeqRNARegexes;
                                    identifierHeader = "NM";
                                    break;
                                case RnaFastaHeaderType.NCBISRA:
                                    regexes = NCBISRARegexes;
                                    identifierHeader = "SRR";
                                    break;
                                case RnaFastaHeaderType.NCBIPD:
                                    regexes = NCBIPDRegexes;
                                    identifierHeader = "PDT";
                                    break;
                                case RnaFastaHeaderType.Unknown:
                                    throw new MzLibUtil.MzLibException("Unknown fasta header format: " + line);
                            } 
                        } 
                        

                        //if (headerType.Equals(RnaFastaHeaderType.Modomics))
                        //{
                        //    regexResults = ParseRegexFields(line, regexes);
                        //    name = regexResults["Name"];
                        //    regexResults.Remove("Name");
                        //    organism = regexResults["Organism"];
                        //    regexResults.Remove("Organism");
                        //    identifier = regexResults[identifierHeader];
                        //    regexResults.Remove(identifierHeader);

                        //    sb = new StringBuilder();
                        //}

                        //if (headerType.Equals(RnaFastaHeaderType.Ensemble))
                        //{
                        regexResults = ParseRegexFields(line, regexes);
                        name = regexResults["Name"];
                        regexResults.Remove("Name");
                        organism = regexResults["Organism"];
                        regexResults.Remove("Organism");
                        identifier = regexResults[identifierHeader];
                        regexResults.Remove(identifierHeader);

                        sb = new StringBuilder();
                        //if (headerType == RnaFastaHeaderType.NCBIPD)
                        //{
                        //    sb.Append(line.Replace(name, "").Trim());
                        //}
                        if (!headerType.Equals(RnaFastaHeaderType.NCBIPD))
                        {
                            continue;
                        }
                        //}
                    }
                    else if (line.StartsWith("+"))
                    {
                        continue;
                    }
                    else if (sb is not null && !line.StartsWith('?'))
                    {
                        //line = fasta.ReadLine();
                        sb.Append(line.Trim());
                    }

                    if ((fasta.Peek() == '>'|| fasta.Peek() == '@' || fasta.Peek() == -1) || headerType == RnaFastaHeaderType.NCBIPD /*&& accession != null*/ && sb != null)
                    {
                        string sequence = headerType == RnaFastaHeaderType.NCBIPD 
                            ? line.Split(' ')[1] 
                            : substituteWhitespace.Replace(sb.ToString(), "");
                        if(sequence.Contains("T"))
                        {
                            sequence = sequence.Transcribe(false);
                        }

                        Dictionary<string, string> additonalDatabaseFields =
                            regexResults.ToDictionary(x => x.Key, x => x.Value);

                        // Do we need to sanitize the sequence? 

                        RNA rna = new RNA(sequence, name, identifier, organism, rnaDbLocation,
                            fivePrimeTerm, threePrimeTerm, null,
                            isContaminant, false, additonalDatabaseFields );
                        if (rna.Length == 0)
                            errors.Add("Line" + line + ", Rna length of 0: " + rna.Name + "was skipped from database: " + rnaDbLocation);
                        else
                            targets.Add(rna);

                        name = null;
                        organism = null;
                        identifier = null;
                        regexResults.Clear();
                    }

                    // no input left
                    if (fasta.Peek() == -1)
                    {
                        break;
                    }
                }
            }

            if (newDbLocation != rnaDbLocation)
                File.Delete(newDbLocation);
            
            if (!targets.Any())
                errors.Add("No targets were loaded from database: " + rnaDbLocation);
            
            List<RNA> decoys = RnaDecoyGenerator.GenerateDecoys(targets, decoyType);
            return generateTargets ? targets.Concat(decoys).ToList() : decoys;
        }

        /// <summary>
        /// Detects the type of RNA header based on the content of the provided line
        /// </summary>
        /// <param name="line">A string representing the header line from the file</param>
        /// <returns>Returns an RnaFastaHeaderType enum indicating the type of RNA header. Returns "Unknown" if the header type cannot be identified</returns>
        public static RnaFastaHeaderType DetectFastaHeaderType(string line)
        {
            if (line.StartsWith(">"))
            {
                if (line.StartsWith(">id"))
                {
                    return RnaFastaHeaderType.Modomics;
                }
                else if (line.StartsWith(">ENST"))
                {
                    return RnaFastaHeaderType.Ensemble;
                }
                else if (line.StartsWith(">NM"))
                {
                    if (line.EndsWith("]"))
                    {
                        return RnaFastaHeaderType.NCBIRefSeqRNA;
                    }
                    return RnaFastaHeaderType.NCBIassembly;
                }
                else if (line.StartsWith(">NC"))
                {
                    return RnaFastaHeaderType.NCBIRefSeqGene;
                }
            }
            else if (line.StartsWith("@"))
            {
                return RnaFastaHeaderType.NCBISRA;
            }
            else if (line.StartsWith("P"))
                //char.IsDigit(line.First()))
            {
                return RnaFastaHeaderType.NCBIPD;
            }

            return RnaFastaHeaderType.Unknown;
        }

        /// <summary>
        /// Parses the fields in a header line using the provided regular expressions and extracts relevant information
        /// </summary>
        /// <param name="line">The header line to parse</param>
        /// <param name="regexes">A dictionary of regular expressions mapped to specific field names to apply on the header line</param>
        /// <returns>Returns a dictionary where the keys are field names and the values are the matched content extracted from the header line</returns>
        private static Dictionary<string, string> ParseRegexFields(string line,
            Dictionary<string, IFastaHeaderFieldRegex> regexes)
        {
            Dictionary<string, string> fields = new Dictionary<string, string>();

            foreach (var regex in regexes)
            {
                string match = regex.Value.ApplyRegex(/*regex.Value,*/ line);
                fields.Add(regex.Key, match);
            }

            return fields;
        }
    }
}
