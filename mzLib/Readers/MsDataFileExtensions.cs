using Chemistry;
using MzLibUtil;
using MassSpectrometry; 
using System;
using System.Collections.Generic;
using System.Linq;
namespace Readers
{
    public static class MsDataFileExtensions
    {
        private sealed class SummedEnvelopePeak : IIndexedPeak
        {
            public float Intensity { get; }
            public float RetentionTime { get; }
            public int ZeroBasedScanIndex { get; }
            public float M { get; }

            public SummedEnvelopePeak(double mz, double intensity, int zeroBasedScanIndex, double retentionTime)
            {
                M = (float)mz;
                Intensity = (float)intensity;
                ZeroBasedScanIndex = zeroBasedScanIndex;
                RetentionTime = (float)retentionTime;
            }
        }

        // <summary>
        /// Extracts an ion chromatogram from the spectra file, given a mass, charge, retention time, and mass tolerance.
        /// </summary>
        public static ExtractedIonChromatogram ExtractIonChromatogram(this MsDataFile file, double neutralMass, int charge, Tolerance massTolerance, double retentionTimeInMinutes, int msOrder = 1, double retentionTimeWindowWidthInMinutes = 5)
        {
            double theorMz = neutralMass.ToMz(charge);
            double startRt = retentionTimeInMinutes - retentionTimeWindowWidthInMinutes / 2;
            double endRt = retentionTimeInMinutes + retentionTimeWindowWidthInMinutes / 2;
            List<IIndexedPeak> xicData = new();
            IEnumerable<MsDataScan> scansInRtWindow = file.GetMsScansInTimeRange(startRt, endRt);
            foreach (MsDataScan scan in scansInRtWindow.Where(p => p.MsnOrder == msOrder))
            {
                int ind = scan.MassSpectrum.GetClosestPeakIndex(theorMz);
                double expMz = scan.MassSpectrum.XArray[ind];
                if (massTolerance.Within(expMz.ToMass(charge), neutralMass))
                {
                    xicData.Add(new IndexedMassSpectralPeak(expMz, scan.MassSpectrum.YArray[ind], scan.OneBasedScanNumber - 1, scan.RetentionTime));
                }
                else
                {
                    xicData.Add(new IndexedMassSpectralPeak(expMz, 0, scan.OneBasedScanNumber - 1, scan.RetentionTime));
                }
            }
            return new ExtractedIonChromatogram(xicData);
        }

        /// <summary>
        /// Extracts an ion chromatogram from the spectra file by summing the isotopic envelope in each scan.
        /// Peak finding begins at the most abundant theoretical isotope and all matching isotopes are summed.
        /// </summary>
        public static ExtractedIonChromatogram ExtractEnvelopeIonChromatogram(this MsDataFile file, double neutralMass, int charge, Tolerance massTolerance,
            double retentionTimeInMinutes, int msOrder = 1, double retentionTimeWindowWidthInMinutes = 5, int maxIsotopes = 6)
        {
            var averageResidue = new OxyriboAveragine();
            int massIndex = averageResidue.GetMostIntenseMassIndex(neutralMass);
            double[] theoreticalMasses = averageResidue.GetAllTheoreticalMasses(massIndex);
            double[] theoreticalIntensities = averageResidue.GetAllTheoreticalIntensities(massIndex);
            double monoisotopicTheoreticalMass = theoreticalMasses[0] - averageResidue.GetDiffToMonoisotopic(massIndex);
            var isotopes = theoreticalMasses
                .Zip(theoreticalIntensities, (mass, intensity) => (massShift: mass - monoisotopicTheoreticalMass, intensity))
                .OrderBy(p => p.massShift)
                .ToList();

            int apexIndex = isotopes
                .Select((isotope, index) => (isotope, index))
                .MaxBy(p => p.isotope.intensity)
                .index;

            int startIsotopeIndex = Math.Max(0, apexIndex - maxIsotopes + 1);
            var selectedIsotopes = isotopes
                .Skip(startIsotopeIndex)
                .Take(maxIsotopes)
                .ToList();

            double mostAbundantMass = neutralMass + isotopes[apexIndex].massShift;
            double mostAbundantMz = mostAbundantMass.ToMz(charge);

            double startRt = retentionTimeInMinutes - retentionTimeWindowWidthInMinutes / 2;
            double endRt = retentionTimeInMinutes + retentionTimeWindowWidthInMinutes / 2;
            List<IIndexedPeak> xicData = new();
            IEnumerable<MsDataScan> scansInRtWindow = file.GetMsScansInTimeRange(startRt, endRt);

            foreach (MsDataScan scan in scansInRtWindow.Where(p => p.MsnOrder == msOrder))
            {
                if (scan.MassSpectrum.Size == 0)
                {
                    xicData.Add(new SummedEnvelopePeak(mostAbundantMz, 0, scan.OneBasedScanNumber - 1, scan.RetentionTime));
                    continue;
                }

                int seedIndex = scan.MassSpectrum.GetClosestPeakIndex(mostAbundantMz);
                double seedMz = scan.MassSpectrum.XArray[seedIndex];
                double seedMass = seedMz.ToMass(charge);

                if (!massTolerance.Within(seedMass, mostAbundantMass))
                {
                    xicData.Add(new SummedEnvelopePeak(mostAbundantMz, 0, scan.OneBasedScanNumber - 1, scan.RetentionTime));
                    continue;
                }

                double massOffset = seedMass - mostAbundantMass;
                double totalIntensity = 0;
                double weightedMz = 0;

                foreach (var isotope in selectedIsotopes)
                {
                    double expectedMass = neutralMass + isotope.massShift + massOffset;
                    double expectedMz = expectedMass.ToMz(charge);
                    int isotopePeakIndex = scan.MassSpectrum.GetClosestPeakIndex(expectedMz);
                    double observedMz = scan.MassSpectrum.XArray[isotopePeakIndex];
                    double observedMass = observedMz.ToMass(charge);

                    if (!massTolerance.Within(observedMass, expectedMass))
                    {
                        continue;
                    }

                    double intensity = scan.MassSpectrum.YArray[isotopePeakIndex];
                    totalIntensity += intensity;
                    weightedMz += observedMz * intensity;
                }

                double averagedMz = totalIntensity > 0 ? weightedMz / totalIntensity : mostAbundantMz;
                xicData.Add(new SummedEnvelopePeak(averagedMz, totalIntensity, scan.OneBasedScanNumber - 1, scan.RetentionTime));
            }

            return new ExtractedIonChromatogram(xicData);
        }

        /// <summary>
        /// Export any MsDataFile as an MzML file to a specific file location
        /// CAUTION: some software will check the NativeID for originalScan numbers
        ///     be sure to set the new NativeID in each MsDataScan if the order has been changed
        /// </summary>
        /// <param name="file"></param>
        /// <param name="destinationPath"></param>
        /// <param name="writeIndexed"></param>
        public static void ExportAsMzML(this MsDataFile file, string destinationPath, bool writeIndexed)
        {
            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(file, destinationPath, writeIndexed);
        }

        /// <summary>
        /// Creates a snip of the data file, starting at the first ms1 after the start originalScan until the end originalScan. 
        /// </summary>
        public static string ExportSnipAsMzML(this MsDataFile originalFile, int startScan, int endScan)
        {
            var filePath = originalFile.FilePath;
            if (originalFile.Scans is null)
                originalFile.LoadAllStaticData();

            var scansToKeep = new List<MsDataScan>(endScan - startScan + 1);

            bool foundFirstMs1 = false;
            int scanIndex = startScan;
            while (scanIndex < endScan + 1)
            {
                var scan = originalFile.GetOneBasedScan(scanIndex);

                // Skip until we find the first MS1 originalScan
                if (!foundFirstMs1)
                {
                    if (scan.MsnOrder == 1)
                        foundFirstMs1 = true;
                    else
                    {
                        scanIndex++;
                        continue;
                    }
                }

                scansToKeep.Add(scan);
                scanIndex++;
            }

            if (!scansToKeep.Any())
                throw new IndexOutOfRangeException(
                    $"No scans found in the range {startScan} to {endScan}. Please check the scan numbers.");

            // Replace this line
            // int scanNumberAdjustment = scansToKeep[0].OneBasedScanNumber - startScan;

            // IF we have faims, we need to ensure all MS2's have their MS1's 
            // This means removing all MS2's between the first two MS1's
            var ms1Scans = scansToKeep.Where(p => p.MsnOrder == 1).ToList();
            if (ms1Scans.Count >= 2 && ms1Scans.All(scan => scan is { CompensationVoltage: not null, MzAnalyzer: MZAnalyzerType.Orbitrap }))
            {
                int start = scansToKeep.FindIndex(p => p.MsnOrder == 1);
                int end = scansToKeep.IndexOf(scansToKeep.Where(p => p.MsnOrder == 1).Skip(1).First());

                var scansToKeepAfterFaimsCheck = new List<MsDataScan>(scansToKeep.Count - (end - start - 1));
                for (int i = 0; i < scansToKeep.Count; i++)
                {
                    if (i <= start || i >= end)
                        scansToKeepAfterFaimsCheck.Add(scansToKeep[i]);
                }
                scansToKeep = scansToKeepAfterFaimsCheck;
            }


            // With this line to ensure the first scan is always 1
            int scanNumberAdjustment = scansToKeep[0].OneBasedScanNumber - 1;
            var originalScanNumbers = new List<(int oneBasedScanNumber, int? oneBasedPrecursorScanNumber)>(scansToKeep.Count);
            var scanNumberMap = new Dictionary<int, int>(scansToKeep.Count * 2);
            var scanLookup = new Dictionary<int, MsDataScan>(scansToKeep.Count);

            int previousScanNumber = scansToKeep[0].OneBasedScanNumber - 1;
            foreach (var scan in scansToKeep)
            {
                int scanNumber = scan.OneBasedScanNumber;
                int? precursorScanNumber = scan.OneBasedPrecursorScanNumber;

                int scanNumberDelta = scanNumber - previousScanNumber;
                if (scanNumberDelta != 1)
                    scanNumberAdjustment += scanNumberDelta - 1;


                scanLookup[scanNumber] = scan;
                originalScanNumbers.Add((scanNumber, precursorScanNumber));
                scanNumberMap.TryAdd(scanNumber, scanNumber - scanNumberAdjustment);

                if (precursorScanNumber is not null)
                {
                    scanNumberMap.TryAdd(precursorScanNumber.Value, precursorScanNumber.Value - scanNumberAdjustment);
                }

                previousScanNumber = scanNumber;
            }

            var scansForTheNewFile = new List<MsDataScan>(scansToKeep.Count);
            foreach (var scanNumber in originalScanNumbers.OrderBy(p => p.oneBasedScanNumber))
            {
                var originalScan = scanLookup[scanNumber.oneBasedScanNumber];
                int newScanNumber = scanNumberMap[originalScan.OneBasedScanNumber];
                int? newPrecursorScanNumber = originalScan.OneBasedPrecursorScanNumber.HasValue
                    ? scanNumberMap[originalScan.OneBasedPrecursorScanNumber.Value]
                    : null;
                string newNativeId = originalScan.NativeId.Replace($"scan={originalScan.OneBasedScanNumber}",$"scan={newScanNumber}");

                var newDataScan = new MsDataScan(
                    originalScan.MassSpectrum,
                    newScanNumber,
                    originalScan.MsnOrder,
                    originalScan.IsCentroid,
                    originalScan.Polarity,
                    originalScan.RetentionTime,
                    originalScan.ScanWindowRange,
                    originalScan.ScanFilter,
                    originalScan.MzAnalyzer,
                    originalScan.TotalIonCurrent,
                    originalScan.InjectionTime,
                    originalScan.NoiseData,
                    newNativeId,
                    originalScan.SelectedIonMZ,
                    originalScan.SelectedIonChargeStateGuess,
                    originalScan.SelectedIonIntensity,
                    originalScan.IsolationMz,
                    originalScan.IsolationWidth,
                    originalScan.DissociationType,
                    newPrecursorScanNumber,
                    originalScan.SelectedIonMonoisotopicGuessMz,
                    originalScan.HcdEnergy,
                    originalScan.ScanDescription,
                    originalScan.CompensationVoltage
                );
                scansForTheNewFile.Add(newDataScan);
            }

            string outPath = Path.Combine(Path.GetDirectoryName(filePath)!,
                filePath.GetPeriodTolerantFilenameWithoutExtension() + "_snip_" + startScan + "-" + endScan + ".mzML");

            var sourceFile = new SourceFile(
                originalFile.SourceFile.NativeIdFormat,
                originalFile.SourceFile.MassSpectrometerFileFormat,
                originalFile.SourceFile.CheckSum,
                originalFile.SourceFile.FileChecksumType,
                originalFile.SourceFile.Uri,
                originalFile.SourceFile.Id,
                originalFile.SourceFile.FileName);

            var dataFile = new GenericMsDataFile(scansForTheNewFile.ToArray(), sourceFile);
            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(dataFile, outPath, false);
            return outPath;
        }
    }
}