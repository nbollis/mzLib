using Proteomics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transcriptomics;

namespace UsefulProteomicsDatabases.Transcriptomics
{
    public static class RnaDecoyGenerator
    {
        public static List<RNA> GenerateDecoys(List<RNA> rnas, DecoyType decoyType, int maxThreads = -1)
        {
            switch (decoyType)
            {
                case DecoyType.None:
                    return new List<RNA>();
                case DecoyType.Reverse:
                    return GenerateReverseDecoys(rnas, maxThreads);
                case DecoyType.Slide:
                    return GenerateSlidedDecoys(rnas, maxThreads);
                case DecoyType.Shuffle:
                case DecoyType.Random:
                default:
                    throw new ArgumentOutOfRangeException(nameof(decoyType), decoyType, null);
            }
        }

        private static List<RNA> GenerateReverseDecoys(List<RNA> rnas, int maxThreads = -1)
        {
            List<RNA> decoyRnas = new List<RNA>();
            Parallel.ForEach(rnas, new ParallelOptions() { MaxDegreeOfParallelism = maxThreads }, rna =>
            {

            });
            return decoyRnas;
        }

        private static List<RNA> GenerateSlidedDecoys(List<RNA> rnas, int maxThreads = -1)
        {
            List<RNA> decoyRnas = new List<RNA>();
            Parallel.ForEach(rnas, new ParallelOptions() { MaxDegreeOfParallelism = maxThreads }, rna =>
            {

            });
            return decoyRnas;
        }
    }
}
