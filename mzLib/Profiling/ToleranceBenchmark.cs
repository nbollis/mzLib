using BenchmarkDotNet.Attributes;
using MzLibUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiling
{
    [MemoryDiagnoser]
    public class ToleranceBenchmarks
    {
        public int Iterations = 1000;
        public NewPpmTolerance NewTolerance;
        public PpmTolerance OldTolerance;

        [GlobalSetup]
        public void Setup()
        {
            OldTolerance = new PpmTolerance(10);
            NewTolerance = new NewPpmTolerance(10);
        }

        [Benchmark]
        public bool[] Within_OldTolerance()
        {
            bool[] bools = new bool[Iterations];
            for (int i = 0; i < Iterations; i++)
            {
                bools[i] = OldTolerance.Within(100, i);
            }
            return bools;
        }

        [Benchmark]
        public DoubleRange[] GetRange_OldTolerance()
        {
            DoubleRange[] ranges = new DoubleRange[Iterations];
            for (int i = 0; i < Iterations; i++)
            {
                ranges[i] = OldTolerance.GetRange(i);
            }
            return ranges;
        }

        [Benchmark]
        public (double, double)[] MinMax_OldTolerance()
        {
            (double, double)[] ranges = new (double, double)[Iterations];
            for (int i = 0; i < Iterations; i++)
            {
                ranges[i] = (OldTolerance.GetMinimumValue(i), OldTolerance.GetMaximumValue(i));
            }
            return ranges;
        }

        [Benchmark]
        public bool[] Within_NewTolerance()
        {
            bool[] bools = new bool[Iterations];
            for (int i = 0; i < Iterations; i++)
            {
                bools[i] = NewTolerance.Within(100, i);
            }
            return bools;
        }

        [Benchmark]
        public DoubleRange[] GetRange_NewTolerance()
        {
            DoubleRange[] ranges = new DoubleRange[Iterations];
            for (int i = 0; i < Iterations; i++)
            {
                ranges[i] = NewTolerance.GetRange(i);
            }
            return ranges;
        }

        [Benchmark]
        public (double, double)[] MinMax_NewTolerance()
        {
            (double, double)[] ranges = new (double, double)[Iterations];
            for (int i = 0; i < Iterations; i++)
            {
                ranges[i] = (NewTolerance.GetMinimumValue(i), NewTolerance.GetMaximumValue(i));
            }
            return ranges;
        }
    }
}
