using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MzLibSpectralAveraging;
using NUnit.Framework;
using SpectralAveraging;

namespace Test
{
    public static class Averaging
    {

        [Test]
        public static void AverageShit2()
        {
            MzLibSpectralAveragingOptions options = new MzLibSpectralAveragingOptions();
            options.RejectionType = RejectionType.WinsorizedSigmaClipping;
            options.BinSize = 0.01;
            options.MaxSigmaValue = 3;
            options.MinSigmaValue = 1.3;
            options.NumberOfScansToAverage = 5;
            options.SpectraFileProcessingType = SpectraFileProcessingType.AverageDDAScans;
            options.PerformNormalization = true;
            options.OutputType = OutputType.mzML;
            options.SpectrumMergingType = SpectrumMergingType.SpectrumBinning;

            List<string> spectraPaths = new();
            
            //spectraPaths.Add(@"D:\Projects\Top Down MetaMorpheus\ChimeraValidation\CaMyoUbiq\Sample9CaliClassic\Task1-CalibrateTask\221110_CaMyoUbiq_402040_5%_Sample9_50IW-calib.mzML");
            //spectraPaths.Add(@"D:\Projects\Top Down MetaMorpheus\ChimeraValidation\CaMyoUbiq\Sample10CaliClassic\Task1-CalibrateTask\221110_CaMyoUbiq_472330_5%_Sample10_50IW-calib.mzML");
            //spectraPaths.Add(@"D:\Projects\Top Down MetaMorpheus\ChimeraValidation\CaMyoUbiq\Sample11CaliClassic\Task1-CalibrateTask\221110_CaMyoUbiq_532620_5%_Sample11_50IW-calib.mzML");
            //spectraPaths.Add(@"D:\Projects\Top Down MetaMorpheus\ChimeraValidation\CaMyoUbiqCytC\Sample12CaliClassic\Task1-CalibrateTask\221110_CaMyoUbiqCytC_37192420_5%_Sample12_50IW-calib.mzML");
            //spectraPaths.Add(@"D:\Projects\Top Down MetaMorpheus\ChimeraValidation\CaMyoUbiqCytC\Sample13CaliClassic\Task1-CalibrateTask\221110_CaMyoUbiqCytC_33162130_5%_Sample13_50IW-calib.mzML");
            //spectraPaths.Add(@"D:\Projects\Top Down MetaMorpheus\ChimeraValidation\CaMyoUbiqCytC\Sample14CaliClassic\Task1-CalibrateTask\221110_CaMyoUbiqCytC_28141840_5%_Sample14_50IW-calib.mzML");
            spectraPaths.Add(@"D:\Projects\Top Down MetaMorpheus\ChimeraValidation\CaMyoUbiqCytC\Sample24CaliClassic\Task1-CalibrateTask\221110_CaMyoUbiqCytC_1576810_5%_Sample24_25IW-calib.mzML");
            spectraPaths.Add(@"D:\Projects\Top Down MetaMorpheus\ChimeraValidation\CaMyoUbiqCytC\Sample24NewAcquisitionMethodCaliClassic\Task1-CalibrateTask\221110_CaMyoUbiqCytC_1576810_5%_Sample24_25IW_ReducedMS1LowerBound-calib.mzML");
            spectraPaths.Add(@"D:\Projects\Top Down MetaMorpheus\ChimeraValidation\CaMyoUbiqCytC\Sample25CaliClassic\Task1-CalibrateTask\221110_CaMyoUbiqCytC_1676908_5%_Sample25_25IW-calib.mzML");
            spectraPaths.Add(@"D:\Projects\Top Down MetaMorpheus\ChimeraValidation\CaMyoUbiqCytC\Sample25SteppedHCDCaliClassic\Task1-CalibrateTask\221110_CaMyoUbiqCytC_1676908_5%_Sample25_25IWSteppedHCD-calib.mzML");

            AverageMany(spectraPaths, options);
           
        }

        [Test]
        public static void AverageShit()
        {
            MzLibSpectralAveragingOptions options = new MzLibSpectralAveragingOptions();
            options.RejectionType = RejectionType.WinsorizedSigmaClipping;
            options.BinSize = 0.01;
            options.MaxSigmaValue = 3;
            options.MinSigmaValue = 1.3;
            options.NumberOfScansToAverage = 5;
            options.ScanOverlap = 0;
            options.SpectraFileProcessingType = SpectraFileProcessingType.AverageDDAScans;
            options.PerformNormalization = true;
            options.OutputType = OutputType.mzML;
            options.SpectrumMergingType = SpectrumMergingType.SpectrumBinning;

            List<string> spectraPaths = new();
            spectraPaths.Add(@"R:\Nic\Chimera Validation\CaMyoUbiq\221110_CaMyoUbiq_241660_5%_Sample20_50IW.raw");
            

            AverageMany(spectraPaths, options);
        }

        private static void AverageMany(List<string> spectraPaths, MzLibSpectralAveragingOptions options)
        {
            foreach (var spec in spectraPaths)
            {
                Average(spec, options);
            }
        }

        private static void Average(string spectraPath, MzLibSpectralAveragingOptions options)
        {
            var spectra = SpectraFileHandler.LoadAllScansFromFile(spectraPath);
            var averaged = SpectraFileProcessing.ProcessSpectra(spectra, options);
            AveragedSpectraOutputter.OutputAveragedScans(averaged, options, spectraPath);
        }
    }
}
