using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SQL.MLDatabase;

namespace Test
{
    public static class AA_DummySQLTests
    {
        [Test]
        public static void TestMockDataIsCorrect()
        {
            IMLData mockAccess = new MockedMLDataAccess();

            Assert.That(!mockAccess.Data.AllProteins.IsValueCreated);
            Assert.That(!mockAccess.Data.AllPsms.IsValueCreated);
            Assert.That(!mockAccess.Data.AllScans.IsValueCreated);

            var proteins = mockAccess.Data.AllProteins.Value;
            Assert.That(mockAccess.Data.AllProteins.IsValueCreated);
            Assert.That(proteins.Count, Is.EqualTo(17));

            var psms = mockAccess.Data.AllPsms.Value;
            Assert.That(mockAccess.Data.AllPsms.IsValueCreated);
            Assert.That(psms.Count, Is.EqualTo(10));

            var scans = mockAccess.Data.AllScans.Value;
            Assert.That(mockAccess.Data.AllScans.IsValueCreated);
            Assert.That(scans.Count, Is.EqualTo(13));
        }
    }
}
