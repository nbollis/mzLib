﻿using Chemistry;
using MzLibUtil;
using Omics.Fragmentation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Easy.Common.Extensions;
using Omics.Fragmentation.Peptide;
using Omics.SpectrumMatch;
using ThermoFisher.CommonCore.Data.Business;

namespace Readers.SpectralLibrary
{
    public class SpectralLibrary : ResultFile<LibrarySpectrum>, IResultFile
    {
        public override SupportedFileType FileType => FilePath.ParseFileType();
        public override Software Software { get; set; }
        public SpectralLibrary() : base() { }
        public SpectralLibrary(string filePath) : base(filePath, Software.MetaMorpheus) { }

        public override void LoadResults()
        {
            Results = GetAllLibrarySpectra().ToList();
        }

        //This is from WriteSpectrumLibrary in MetaMorpheusTask
        public override void WriteResults(string outputPath)
        {
            using (StreamWriter output = new StreamWriter(outputPath))
            {
                foreach (var x in Results)
                {
                    output.WriteLine(x.ToString());
                }
            }
        }

        private List<string> LibraryPaths;
        private Dictionary<string, (string filePath, long byteOffset)> SequenceToFileAndLocation;
        private Queue<string> LibrarySpectrumBufferList;
        public Dictionary<string, LibrarySpectrum> LibrarySpectrumBuffer;
        private int MaxElementsInBuffer = 10000;
        private Dictionary<string, StreamReader> StreamReaders;
        private static Regex IonParserRegex = new Regex(@"^(\D{1,})(\d{1,})(?:[\^]|$)(-?\d{1,}|$)");

        private static Dictionary<string, string> PrositToMetaMorpheusModDictionary = new Dictionary<string, string>
        {
            { "Oxidation","[Common Variable:Oxidation on M]" },
            { "Carbamidomethyl", "[Common Fixed:Carbamidomethyl on C]" }
        };

        private static Dictionary<string, string> pDeepToMetaMorpheusModDictionary = new Dictionary<string, string>
        {
            { "Oxidation","[Common Variable:Oxidation on M]" },
            {"CAM", "[Common Fixed:Carbamidomethyl on C]" }
        };

        public SpectralLibrary(List<string> pathsToLibraries)
        {
            LibraryPaths = pathsToLibraries;
            SequenceToFileAndLocation = new Dictionary<string, (string, long)>();
            LibrarySpectrumBufferList = new Queue<string>();
            LibrarySpectrumBuffer = new Dictionary<string, LibrarySpectrum>();
            StreamReaders = new Dictionary<string, StreamReader>();

            foreach (var path in LibraryPaths)
            {
                IndexSpectralLibrary(path);
            }
        }

        public bool ContainsSpectrum(string sequence, int charge)
        {
            string lookupString = sequence + "/" + charge;

            return SequenceToFileAndLocation.ContainsKey(lookupString);
        }

        public bool TryGetSpectrum(string sequence, int charge, out LibrarySpectrum librarySpectrum)
        {
            string lookupString = sequence + "/" + charge;
            librarySpectrum = null;

            // look up in buffer to see if this library spectrum was read in already
            if (LibrarySpectrumBuffer.TryGetValue(lookupString, out var spectrum))
            {
                librarySpectrum = spectrum;

                if (librarySpectrum.Name != lookupString)
                {
                    throw new MzLibException("Bad spectral library formatting or indexing: Found \""
                        + librarySpectrum.Name + "\" but expected \"" + lookupString + "\"");
                }

                return true;
            }

            // go find the library spectrum in the spectral library file
            if (SequenceToFileAndLocation.TryGetValue(lookupString, out var value))
            {
                lock (StreamReaders[value.filePath])
                {
                    librarySpectrum = ReadSpectrumFromLibraryFile(value.filePath, value.byteOffset);

                    if (librarySpectrum.Name != lookupString)
                    {
                        throw new MzLibException("Bad spectral library formatting or indexing: Found \""
                            + librarySpectrum.Name + "\" but expected \"" + lookupString + "\"");
                    }

                    // add this item to the buffer
                    lock (LibrarySpectrumBuffer)
                    {
                        lock (LibrarySpectrumBufferList)
                        {
                            LibrarySpectrumBuffer.TryAdd(lookupString, librarySpectrum);

                            LibrarySpectrumBufferList.Enqueue(lookupString);


                            // remove items from buffer if the buffer is at max capacity
                            while (LibrarySpectrumBuffer.Count > MaxElementsInBuffer)
                            {
                                var item = LibrarySpectrumBufferList.Dequeue();
                                LibrarySpectrumBuffer.Remove(item);
                            }
                        }
                    }
                }

                return true;
            }

            return false;
        }

        public IEnumerable<LibrarySpectrum> GetAllLibrarySpectra()
        {
            foreach (var item in SequenceToFileAndLocation)
            {
                yield return ReadSpectrumFromLibraryFile(item.Value.filePath, item.Value.byteOffset);
            }
        }

        public void CloseConnections()
        {
            foreach (var item in StreamReaders)
            {
                item.Value.Close();
            }
        }

        private LibrarySpectrum ReadSpectrumFromLibraryFile(string path, long byteOffset)
        {
            if (!StreamReaders.TryGetValue(path, out var reader))
            {
                throw new MzLibException("????");
                //IndexSpectralLibrary(path);

                //if (!StreamReaders.TryGetValue(path, out reader))
                //{
                //    // TODO: throw an exception
                //}
            }

            // seek to the byte of the scan
            reader.BaseStream.Position = byteOffset;
            reader.DiscardBufferedData();

            // return the library spectrum
            if (path.Contains("pdeep"))
            {
                return ReadLibrarySpectrum_pDeep(reader);
            }
            else
            {
                return ReadLibrarySpectrum(reader);
            }
        }

        private LibrarySpectrum ReadLibrarySpectrum(StreamReader reader, bool onlyReadHeader = false)
        {
            char[] nameSplit = new char[] { '/' };
            char[] mwSplit = new char[] { ':' };
            char[] commentSplit = new char[] { ' ', ':', '=' };
            char[] modSplit = new char[] { '=', '/' };
            char[] fragmentSplit = new char[] { '\t', '\"', ')', '/' };
            char[] neutralLossSplit = new char[] { '-' };

            bool readingPeaks = false;
            string sequence = null;
            int z = 2;
            double precursorMz = 0;
            double rt = 0;
            List<MatchedFragmentIon> matchedFragmentIons = new List<MatchedFragmentIon>();

            while (reader.Peek() > 0)
            {
                string line = reader.ReadLine();
                string[] split;

                if (line.StartsWith("Name", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (CrosslinkLibrarySpectrum.CrosslinkRegex.Match(line).Success)
                    {
                        return ReadLibrarySpectrum_Crosslink(reader, line, onlyReadHeader);
                    }

                    if (sequence != null)
                    {
                        return new LibrarySpectrum(sequence, precursorMz, z, matchedFragmentIons, rt);
                    }

                    split = line.Split(nameSplit);

                    // get sequence
                    sequence = split[0].Replace("Name:", string.Empty).Trim();

                    // get charge
                    z = int.Parse(split[1].Trim());
                }
                else if (line.StartsWith("MW", StringComparison.InvariantCultureIgnoreCase))
                {
                    split = line.Split(mwSplit);

                    // get precursor m/z
                    precursorMz = double.Parse(split[1].Trim(), CultureInfo.InvariantCulture);
                }
                else if (line.StartsWith("Comment", StringComparison.InvariantCultureIgnoreCase))
                {
                    split = line.Split(commentSplit);

                    // get precursor m/z if not defined yet
                    if (precursorMz == 0)
                    {
                        int indOfParent = Array.IndexOf(split, "Parent");
                        if (indOfParent > 0)
                        {
                            precursorMz = double.Parse(split[indOfParent + 1], CultureInfo.InvariantCulture);
                        }
                    }

                    // get RT
                    int indOfRt = Array.IndexOf(split, "iRT");
                    if (indOfRt > 0)
                    {
                        rt = double.Parse(split[indOfRt + 1], CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        indOfRt = Array.IndexOf(split, "RT");

                        if (indOfRt > 0)
                        {
                            rt = double.Parse(split[indOfRt + 1], CultureInfo.InvariantCulture);
                        }
                    }

                    // get mods
                    // be careful about spaces! mod names can have spaces in them
                    StringBuilder sb = new StringBuilder();
                    int ind = line.IndexOf("Mods", StringComparison.InvariantCultureIgnoreCase);

                    if (ind > 0)
                    {
                        bool readingModName = false;
                        int bracketCount = 0;

                        for (int i = ind; i < line.Length; i++)
                        {
                            if (line[i] == ' ' && !readingModName)
                            {
                                break;
                            }
                            if (line[i] == '[')
                            {
                                bracketCount++;
                                readingModName = true;
                            }
                            else if (line[i] == ']')
                            {
                                bracketCount--;

                                if (bracketCount == 0)
                                {
                                    readingModName = false;
                                }
                            }

                            sb.Append(line[i]);
                        }

                        if (sb.ToString() != "Mods=0")
                        {
                            split = sb.ToString().Split(modSplit);

                            for (int i = split.Length - 1; i >= 2; i--)
                            {
                                string modString = split[i];

                                string[] modInfo = modString.Split(',');
                                int modPosition = int.Parse(modInfo[0]);
                                string modName = modInfo[2];
                                string modNameNoBrackets = modName;

                                if (modName.StartsWith('['))
                                {
                                    modNameNoBrackets = modName.Substring(1, modName.Length - 2);
                                }

                                if (!ModificationConverter.AllKnownMods.Select(m => m.IdWithMotif).Contains(modNameNoBrackets))
                                {
                                    if (PrositToMetaMorpheusModDictionary.TryGetValue(modName, out var metaMorpheusMod))
                                    {
                                        modName = metaMorpheusMod;
                                    }
                                }

                                // add the mod name into the sequence
                                string leftSeq = sequence.Substring(0, modPosition + 1);
                                string rightSeq = sequence.Substring(modPosition + 1);

                                sequence = leftSeq + modName + rightSeq;
                            }
                        }
                    }
                }
                else if (line.StartsWith("Num peaks", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (onlyReadHeader)
                    {
                        return new LibrarySpectrum(sequence, precursorMz, z, matchedFragmentIons, rt);
                    }

                    // this assumes that the peaks are listed after the "Num peaks" line
                    readingPeaks = true;
                }
                else if (readingPeaks)
                {
                    matchedFragmentIons.Add(ReadFragmentIon(line, fragmentSplit, neutralLossSplit, sequence));
                }
            }

            return new LibrarySpectrum(sequence, precursorMz, z, matchedFragmentIons, rt);
        }

        private LibrarySpectrum ReadLibrarySpectrum_pDeep(StreamReader reader, bool onlyReadHeader = false)
        {
            char[] nameSplit = new char[] { '/', '_' };
            char[] mwSplit = new char[] { ':' };
            char[] commentSplit = new char[] { ' ', ':', '=' };
            char[] modSplit = new char[] { '/', '(', ')' };
            char[] fragmentSplit = new char[] { '\t', '/' };

            bool readingPeaks = false;
            string sequence = null;
            int z = 2;
            double precursorMz = 0;
            double rt = 0;
            List<MatchedFragmentIon> matchedFragmentIons = new List<MatchedFragmentIon>();

            while (reader.Peek() > 0)
            {
                string line = reader.ReadLine();
                string[] split;

                if (line.StartsWith("Name", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (sequence != null)
                    {
                        return new LibrarySpectrum(sequence, precursorMz, z, matchedFragmentIons, rt);
                    }

                    split = line.Split(nameSplit);

                    // get sequence
                    sequence = split[0].Replace("Name:", string.Empty).Trim();

                    // get charge
                    z = int.Parse(split[1].Trim());

                    string[] mods = split[2].Split(modSplit, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = mods.Length - 1; i > 0; i--)
                    {
                        string[] modInfo = mods[i].Split(',');
                        int index = Convert.ToInt32(modInfo[0]);
                        string mod = modInfo[2];
                        string metaMorpheusMod = pDeepToMetaMorpheusModDictionary[mod];
                        //add the mod into the sequence
                        string leftSeq = sequence.Substring(0, index + 1);
                        string rightSeq = sequence.Substring(index + 1);
                        sequence = leftSeq + metaMorpheusMod + rightSeq;
                    }

                }
                else if (line.StartsWith("Comment", StringComparison.InvariantCultureIgnoreCase))
                {
                    split = line.Split(commentSplit);

                    // get precursor m/z in comment
                    int indOfParent = Array.IndexOf(split, "Parent");
                    if (indOfParent > 0)
                    {
                        precursorMz = double.Parse(split[indOfParent + 1]);
                    }

                    // get RT
                    int indOfRt = Array.IndexOf(split, "RTInSeconds");
                    if (indOfRt > 0)
                    {
                        rt = double.Parse(split[indOfRt + 1]);
                    }
                }
                else if (line.StartsWith("Num peaks", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (onlyReadHeader)
                    {
                        return new LibrarySpectrum(sequence, precursorMz, z, matchedFragmentIons, rt);
                    }

                    // this assumes that the peaks are listed after the "Num peaks" line
                    readingPeaks = true;
                }
                else if (readingPeaks && line != "")
                {
                    split = line.Split(fragmentSplit, StringSplitOptions.RemoveEmptyEntries);

                    // read fragment m/z
                    var experMz = double.Parse(split[0], CultureInfo.InvariantCulture);

                    // read fragment intensity
                    var experIntensity = double.Parse(split[1], CultureInfo.InvariantCulture);

                    // read fragment type, number      

                    string fragmentType = split[2].ToCharArray()[0].ToString();
                    int fragmentNumber = int.Parse(new string(split[2].Split(new char[] { '^' })[0].Where(Char.IsDigit).ToArray()));
                    int fragmentCharge = 1;


                    if (split[2].Contains('^'))
                    {
                        fragmentCharge = int.Parse(split[2].Split('^')[1]);
                    }

                    ProductType peakProductType = (ProductType)Enum.Parse(typeof(ProductType), fragmentType, true);

                    //TODO: figure out terminus
                    FragmentationTerminus terminus = (FragmentationTerminus)Enum.Parse(typeof(FragmentationTerminus), "None", true);

                    //TODO: figure out amino acid position
                    var product = new Product(peakProductType, terminus, experMz, fragmentNumber, 0, 0);

                    matchedFragmentIons.Add(new MatchedFragmentIon(product, experMz, experIntensity, fragmentCharge));
                }
            }

            return new LibrarySpectrum(sequence, precursorMz, z, matchedFragmentIons, rt);
        }

        internal CrosslinkLibrarySpectrum ReadLibrarySpectrum_Crosslink(StreamReader reader, string nameLine, bool onlyReadHeader)
        {
            char[] nameSplit = new char[] { '/' };
            char[] mwSplit = new char[] { ':' };
            char[] commentSplit = new char[] { ' ', ':', '=' };
            char[] fragmentSplit = new char[] { '\t', '\"', ')', '/' };
            char[] neutralLossSplit = new char[] { '-' };

            string[] splitNameLine = nameLine.Split(nameSplit);
            string uniqueSequence = splitNameLine[0].Replace("Name:", string.Empty).Trim();
            string alphaSequence = "";
            string betaSequence = "";
            string[] splitAlphaBetaSequence = new Regex(pattern: @"\(\d+\)").Split(uniqueSequence);
            if (splitAlphaBetaSequence.Length >= 2)
            {
                alphaSequence = splitAlphaBetaSequence[0];
                betaSequence = splitAlphaBetaSequence[1];
            }
            else if (splitAlphaBetaSequence.Length == 1)
            {
                alphaSequence = splitAlphaBetaSequence[0];
            }
            int z = int.Parse(splitNameLine[1].Trim());
            double precursorMz = 0;
            double rt = 0;
            int indOfRt = -1;
            bool readingPeaks = false;
            List<MatchedFragmentIon> alphaPeptideIons = new List<MatchedFragmentIon>();
            List<MatchedFragmentIon> betaPeptideIons = new List<MatchedFragmentIon>();

            while (reader.Peek() > 0)
            {
                string line = reader.ReadLine();
                string[] split;

                if (line.StartsWith("Name", StringComparison.InvariantCultureIgnoreCase))
                {
                    break;
                }
                else if (line.StartsWith("MW", StringComparison.InvariantCultureIgnoreCase))
                {
                    split = line.Split(mwSplit);

                    // get precursor m/z
                    precursorMz = double.Parse(split[1].Trim(), CultureInfo.InvariantCulture);
                }
                else if (line.StartsWith("Comment", StringComparison.InvariantCultureIgnoreCase))
                {
                    split = line.Split(commentSplit);

                    // get precursor m/z if not defined yet
                    if (precursorMz == 0)
                    {
                        int indOfParent = Array.IndexOf(split, "Parent");
                        if (indOfParent > 0)
                        {
                            precursorMz = double.Parse(split[indOfParent + 1], CultureInfo.InvariantCulture);
                        }
                    }


                    indOfRt = Array.IndexOf(split, "RT");

                    if (indOfRt > 0)
                    {
                        rt = double.Parse(split[indOfRt + 1], CultureInfo.InvariantCulture);
                    }
                }
                else if (line.StartsWith("Num alpha peaks", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (onlyReadHeader)
                    {
                        CrosslinkLibrarySpectrum betaPeptideSpectrumHeaderOnly = new CrosslinkLibrarySpectrum(uniqueSequence, precursorMz, z, betaPeptideIons, rt);
                        return new CrosslinkLibrarySpectrum(uniqueSequence, precursorMz, z, alphaPeptideIons, rt, betaPeptideSpectrumHeaderOnly);
                    }
                    // this assumes that the peaks are listed after the "Num peaks" line
                    readingPeaks = true;
                }
                else if (readingPeaks)
                {
                    bool isBetaPeptideIon = line.Contains("BetaPeptide");
                    string peptideSequence = isBetaPeptideIon ? betaSequence : alphaSequence;
                    MatchedFragmentIon fragmentIon =
                        ReadFragmentIon(line, fragmentSplit, neutralLossSplit, peptideSequence);
                    if (isBetaPeptideIon)
                    {
                        betaPeptideIons.Add(fragmentIon);
                    }
                    else
                    {
                        alphaPeptideIons.Add(fragmentIon);
                    }
                }
            }

            CrosslinkLibrarySpectrum betaPeptideSpectrum = new CrosslinkLibrarySpectrum(uniqueSequence, precursorMz, z, betaPeptideIons, rt);
            return new CrosslinkLibrarySpectrum(uniqueSequence, precursorMz, z, alphaPeptideIons, rt, betaPeptideSpectrum);
        }

        /// <summary>
        /// Creates a matched fragment ion from a line in a spectral library. Does not work with P-Deep libraries.
        /// </summary>
        public static MatchedFragmentIon ReadFragmentIon(string fragmentIonLine, char[] fragmentSplit,
            char[] neutralLossSplit, string peptideSequence)
        {
            string[] split = fragmentIonLine.Split(fragmentSplit, StringSplitOptions.RemoveEmptyEntries);

            // read fragment m/z
            var experMz = double.Parse(split[0], CultureInfo.InvariantCulture);

            // read fragment intensity
            var experIntensity = double.Parse(split[1], CultureInfo.InvariantCulture);

            // read fragment type, number
            Match regexMatchResult = IonParserRegex.Match(split[2]);

            string fragmentType = regexMatchResult.Groups[1].Value;
            int fragmentNumber = int.Parse(regexMatchResult.Groups[2].Value);
            int fragmentCharge = 1;

            if (regexMatchResult.Groups.Count > 3 && !string.IsNullOrWhiteSpace(regexMatchResult.Groups[3].Value))
            {
                fragmentCharge = int.Parse(regexMatchResult.Groups[3].Value);
            }

            double neutralLoss = 0;
            if (split[2].Contains("-") && fragmentCharge > 0)
            {
                String[] neutralLossInformation = split[2].Split(neutralLossSplit, StringSplitOptions.RemoveEmptyEntries).ToArray();
                neutralLoss = double.Parse(neutralLossInformation[1]);
            }
            if (fragmentCharge < 0)
            {
                String[] neutralLossInformation = split[2].Split(neutralLossSplit, StringSplitOptions.RemoveEmptyEntries).ToArray();
                if (neutralLossInformation.Length > 2)
                    neutralLoss = double.Parse(neutralLossInformation[2]);
            }

            ProductType peakProductType = (ProductType)Enum.Parse(typeof(ProductType), fragmentType, true);
            // Default product for productTypes not contained in the ProductTypeToFragmentationTerminus dictionary (e.g., "M" type ions)
            Product product = new Product(peakProductType, (FragmentationTerminus)Enum.Parse(typeof(FragmentationTerminus),
                "None", true), experMz, fragmentNumber, 0, 0);

            if (TerminusSpecificProductTypes.ProductTypeToFragmentationTerminus.TryGetValue(peakProductType,
                    out var terminus))
            {
                int peptideLength = peptideSequence.IsNotNullOrEmptyOrWhiteSpace() ? peptideSequence.Length : 25; // Arbitrary default peptide length
                product = new Product(peakProductType, terminus, experMz.ToMass(fragmentCharge), fragmentNumber,
                    residuePosition: terminus == FragmentationTerminus.N ? fragmentNumber : peptideLength - fragmentNumber,
                    neutralLoss);
            }

            return new MatchedFragmentIon(product, experMz, experIntensity, fragmentCharge);
        }

        private void IndexSpectralLibrary(string path)
        {
            var reader = new StreamReader(path);
            StreamReaders.Add(path, reader);

            reader.BaseStream.Position = 0;
            reader.DiscardBufferedData();

            while (reader.Peek() > 0)
            {
                long byteOffset = TextFileReading.GetByteOffsetAtCurrentPosition(reader);
                var line = reader.ReadLine().Trim();

                if (line.StartsWith("name", StringComparison.InvariantCultureIgnoreCase))
                {
                    // seek back to beginning of line so the parser can read the "name" line
                    reader.BaseStream.Position = byteOffset;
                    reader.DiscardBufferedData();

                    // parse the header
                    LibrarySpectrum libraryItem;
                    if (path.Contains("pdeep"))
                    {
                        libraryItem = ReadLibrarySpectrum_pDeep(reader, onlyReadHeader: true);
                    }
                    else
                    {
                        libraryItem = ReadLibrarySpectrum(reader, onlyReadHeader: true);
                    }

                    // add the spectrum to the index
                    SequenceToFileAndLocation.TryAdd(libraryItem.Name, (path, byteOffset));
                }
            }
        }
    }
}
