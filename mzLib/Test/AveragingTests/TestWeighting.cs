﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassSpectrometry;
using MzLibUtil;
using NUnit.Framework;
using SpectralAveraging;

namespace Test.AveragingTests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public static class TestWeighting
    {
        public static double[][] xArrays;
        public static double[][] yArrays;
        public static MzSpectrum[] spectra;

        [OneTimeSetUp]
        public static void OneTimeSetup()
        {
            var xArray = new[] { 1.0, 2, 3, 4, 5 };
            var yArray1 = new[] { 10.0, 10, 10, 10, 10 };
            var yArray2 = new[] { 20.0, 20, 20, 20, 20 };
            var yArray3 = new[] { 30.0, 30, 30, 30, 30 };
            var spec1 = new MzSpectrum(xArray, yArray1, true);
            var spec2 = new MzSpectrum(xArray, yArray2, true);
            var spec3 = new MzSpectrum(xArray, yArray3, true);
            spectra = new[] { spec1, spec2, spec3 };
            xArrays = new[] { xArray, xArray, xArray };
            yArrays = new[] { yArray1, yArray2, yArray3 };
        }

        [Test]
        public static void TestWeightingSwitchError()
        {
            var exception = Assert.Throws<MzLibException>(() =>
            {
                SpectralWeighting.CalculateSpectraWeights(xArrays, yArrays, (SpectraWeightingType)(-1));
            });
            Assert.That(exception.Message == "Spectra Weighting Type Not Implemented");
        }

        [Test]
        public static void TestWeighEvenly()
        {
            var weights = SpectralWeighting.CalculateSpectraWeights(xArrays, yArrays, SpectraWeightingType.WeightEvenly);
            var expected = new[] { 1.0, 1, 1 };
            Assert.That(expected.SequenceEqual(weights.Select(p => p.Value)));
        }

        [Test]
        public static void TestWeightByTicValue()
        {
            var weights = SpectralWeighting.CalculateSpectraWeights(xArrays, yArrays, SpectraWeightingType.TicValue);
            var expected = new[] { (1.0 / 3), (2.0 / 3), 1 };
            Assert.That(expected.SequenceEqual(weights.Select(p => p.Value)));
        }

        [Test]
        public static void TestWeightByMrsNoiseEstimation()
        {
            var xArrays = new[]
            {
                        new double[] { 0, 1, 2, 3, 3.49, 4 },
                        new double[] { 0, 1, 2, 3, 4 },
                        new double[] { 0.1, 1.1, 2.1, 3.1, 4.1}
                    };
            var yArrays = new[]
            {
                        new double[] { 10, 11, 12, 12, 13, 14 },
                        new double[] { 11, 12, 13, 14, 15 },
                        new double[] { 20, 25, 30, 35, 40 }
                    };
            Dictionary<int, double> weights = SpectralWeighting.CalculateSpectraWeights(xArrays,
                yArrays, SpectraWeightingType.MrsNoiseEstimation);
            double[] expectedWeights = { 0.499999, 0.45036, 0.090072 };

            Assert.That(weights.Values, Is.EqualTo(expectedWeights).Within(0.001));
        }

        [Test]
        public static void TestWeightByLocalizedTicValue()
        {
            var bins = new Dictionary<int, List<BinnedPeak>>
                    {
                        { 0, new List<BinnedPeak> 
                        { new BinnedPeak(0, 0, 10, 0), new BinnedPeak(0, 0, 15, 1), new BinnedPeak(0, 0, 20, 2) } },
                        { 1, new List<BinnedPeak> 
                        { new BinnedPeak(1, 1, 20, 0), new BinnedPeak(1, 1, 25, 1), new BinnedPeak(1, 1, 30, 2) } },
                        { 2, new List<BinnedPeak> 
                        { new BinnedPeak(2, 2, 30, 0), new BinnedPeak(2, 2, 35, 1), new BinnedPeak(2, 2, 40, 2) } },
                        { 3, new List<BinnedPeak> 
                        { new BinnedPeak(3, 3, 40, 0), new BinnedPeak(3, 3, 45, 1), new BinnedPeak(3, 3, 50, 2) } },
                        { 4, new List<BinnedPeak> 
                        { new BinnedPeak(4, 4, 50, 0), new BinnedPeak(4, 4, 55, 1), new BinnedPeak(4, 4, 60, 2) } }
                    };
    
            var weights = SpectralWeighting.CalculateBinWeights(bins, SpectraWeightingType.LocalizedTicValue, 1);
            
            var expectedWeights = new Dictionary<int, Dictionary<int, double>>
                    {
                        { 0, new Dictionary<int, double> { { 0, 0.25 }, { 1, 0.333 }, { 2, 0.4167 } } },
                        { 1, new Dictionary<int, double> { { 0, 0.267 }, { 1, .333 }, { 2, 0.4 } } },
                        { 2, new Dictionary<int, double> { { 0, 0.286 }, { 1, .333 }, { 2, 0.381 } } },
                        { 3, new Dictionary<int, double> { { 0, 0.296 }, { 1, .333 }, { 2, 0.370 } } },
                        { 4, new Dictionary<int, double> { { 0, 0.3 }, { 1, .333 }, { 2, 0.367 } } }
                    };

            for (int binId = 0; binId < bins.Count; binId++)
            {
                for (int scanId = 0; scanId < bins.First().Value.Count; scanId++)
                {
                    var expected = expectedWeights[binId][scanId];
                    var actual = weights[binId][scanId];
                    Assert.That(actual, Is.EqualTo(expected).Within(0.001));
                }
            }
        }
    }
}
