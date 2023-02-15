using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Statistics;

namespace Test.AveragingTests
{
    public readonly record struct BasicStats : ITsv
    {
        public string Name { get; init; }
        public double Mean { get; init; }
        public double Median { get; init; }
        public double Minimum { get; init; }
        public double Maximum { get; init; }
        public double FirstQuartile { get; init; }
        public double ThirdQuartile { get; init;}

        public BasicStats(string name, IEnumerable<double> values)
        {
            Name = name;
            var vals = values.ToArray();
            Mean = vals.Mean();
            Median = vals.Median();
            Minimum = vals.Minimum();
            Maximum = vals.Maximum();
            FirstQuartile = vals.Where(p => p <= vals.Median()).Median();
            ThirdQuartile = vals.Where(p => p >= vals.Median()).Median();
        }

        public string TabSeparatedHeader
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append($"{Name} Mean\t");
                sb.Append($"{Name} Median\t");
                sb.Append($"{Name} Minimum\t");
                sb.Append($"{Name} Maximum\t");
                sb.Append($"{Name} FirstQuartile\t");
                sb.Append($"{Name} ThirdQuartile\t");
                var tsvString = sb.ToString().TrimEnd('\t');
                return tsvString;
            }
        }

        public string ToTsvString()
        {
            var sb = new StringBuilder();
            sb.Append($"{Mean}\t");
            sb.Append($"{Median}\t");
            sb.Append($"{Minimum}\t");
            sb.Append($"{Maximum}\t");
            sb.Append($"{FirstQuartile}\t");
            sb.Append($"{ThirdQuartile}\t");
            var tsvString = sb.ToString().TrimEnd('\t');
            return tsvString;
        }
    }
}
