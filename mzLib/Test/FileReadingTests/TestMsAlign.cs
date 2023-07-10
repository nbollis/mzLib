using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Easy.Common.Extensions;
using Easy.Common.Interfaces;
using MassSpectrometry;
using NUnit.Framework;
using Readers;

namespace Test.FileReadingTests
{
    [TestFixture]
    public class TestMsAlign
    {
        private static string Ms1AlignPath = @"FileReadingTest/ExternalFileTypes\Ms1Align_FlashDeconvOpenMs3,0,0_ms1.msalign";
        private static string Ms2AlignPath_TopFd = @"Ms2Align_TopFDv1,6,2_ms2.msalign";
        private static string Ms2AlignPath_FlashDeconv = @"Ms2Align_FlashDeconvOPenMs3,0,0_ms2.msalign";

        public static IEnumerable<string> GetMs1AlignTestCases()
        {
            yield return Path.Combine(TestContext.CurrentContext.TestDirectory, "FileReadingTests", "ExternalFileTypes", Ms1AlignPath);
        }

        public static IEnumerable<string> GetMs2AlignTestCases()
        {
            yield return Path.Combine(TestContext.CurrentContext.TestDirectory, "FileReadingTests", "ExternalFileTypes", Ms2AlignPath_TopFd);
            yield return Path.Combine(TestContext.CurrentContext.TestDirectory, "FileReadingTests", "ExternalFileTypes", Ms2AlignPath_FlashDeconv);
        }

        [Test]
        [TestCaseSource(nameof(Ms2AlignPath_TopFd))]
        public static void TestMs2AlignHeaderParsing(string testFilePath)
        {

        }
    }
}
