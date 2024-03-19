#nullable enable
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace UsefulProteomicsDatabases
{

    public interface IFastaHeaderFieldRegex
    {
        string FieldName { get; }
        Regex Regex { get; }
        int Match { get; }

        string ApplyRegex(string input);
    }

    public class FastaHeaderFieldRegex : IFastaHeaderFieldRegex
    {
        public FastaHeaderFieldRegex(string fieldName, string regularExpression, int match, int group, string? hardCodedValue = null)
        {
            FieldName = fieldName;
            Regex = new Regex(regularExpression);
            Match = match;
            Group = group;
            HardCodedValue = hardCodedValue;
        }

        public string FieldName { get; }

        public Regex Regex { get; }

        public int Match { get; }

        public int Group { get; }
        
        public string? HardCodedValue { get; }

        public string ApplyRegex(string input)
        {
            if (HardCodedValue is not null)
                return HardCodedValue;

            string result = null;
            var matches = Regex.Matches(input);
            if (matches.Count > Match && matches[Match].Groups.Count > Group)
            {
                result = matches[Match].Groups[Group].Value;
            }

            return result;
        }
    }

    public class FastaHeaderMultiFieldRegex : IFastaHeaderFieldRegex
    {
        public FastaHeaderMultiFieldRegex (string fieldName, string regex, int match, int[] group)
        {
            FieldName = fieldName;
            Regex = new Regex(regex);
            Match = match;
            Group = group;
        }
        public string FieldName { get; }
        public Regex Regex { get; }
        public int Match { get; }

        public int[] Group { get; }

        public string ApplyRegex(string input)
        {
            // parse each difference match
            // concatonate as desired
            // return concatonated
            string result = null;
            var matches = Regex.Matches(input);
            if (matches.Count > Match && matches[Match].Groups.Count > Group.Length)
            {
                foreach (int i in Group)
                {
                    result = result + matches[Match].Groups[i].Value + " ";
                }
            }

            return result?.Trim() ?? result;
            throw new NotImplementedException();
        }
    }
}