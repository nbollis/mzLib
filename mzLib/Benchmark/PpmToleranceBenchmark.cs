using BenchmarkDotNet.Attributes;
using MzLibUtil;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class ToleranceBenchmark
    {
        [GlobalSetup]
        public void Setup()
        {
            Random random = new Random();
            First = new double[NumberOfItems];
            Second = new double[NumberOfItems];
            for (int i = 0; i < NumberOfItems; i++)
            {
                First[i] = random.Next(1000, 10000);
                Second[i] = random.NextDouble();
            }
        }

        int NumberOfItems = 10000;
        int Value = 100;

        double[] First;
        double[] Second;

        [Benchmark]
        public bool[] NewWithin()
        {
            var tolerance = new PpmTolerance(Value);
            bool[] result = new bool[NumberOfItems];

            for (int i = 0; i < NumberOfItems; i++)
            {
                result[i] = tolerance.Within(First[i], Second[i]);
            }
            return result;
        }

        [Benchmark]
        public bool[] OldWithin()
        {
            var tolerance = new OldPpmTolerance(Value);
            bool[] result = new bool[NumberOfItems];

            for (int i = 0; i < NumberOfItems; i++)
            {
                result[i] = tolerance.Within(First[i], Second[i]);
            }
            return result;
        }
    }

    internal class OldPpmTolerance(double value) : PpmTolerance(value)
    {
        public override bool Within(double experimental, double theoretical)
        {
            return Math.Abs((experimental - theoretical) / theoretical * 1e6) <= Value;
        }
    }
}
