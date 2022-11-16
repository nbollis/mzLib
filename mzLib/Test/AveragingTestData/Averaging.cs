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
        public static void AverageShit()
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

            string spectraPath = @"D:\Projects\Top Down MetaMorpheus\ChimeraValidation\CaMyoOnly\Sample8Searches\CaliSmallGPTMDClasssic\Task1-CalibrateTask\221110_CaMyo_6040_5%_Sample8_50IW-calib.mzML";

            var spectra = SpectraFileHandler.LoadAllScansFromFile(spectraPath);
            var averaged = SpectraFileProcessing.ProcessSpectra(spectra, options);
            AveragedSpectraOutputter.OutputAveragedScans(averaged, options, spectraPath);
        }
    }
}
