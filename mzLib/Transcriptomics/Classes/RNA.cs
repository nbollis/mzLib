using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;
using MassSpectrometry;

namespace Transcriptomics
{
    public class RNA : NucleicAcid
    {
        public RNA(string sequence) : base(sequence)
        {
        }

        public RNA(string sequence, IHasChemicalFormula fivePrimeTerm, IHasChemicalFormula threePrimeTerm) : base(sequence, fivePrimeTerm, threePrimeTerm)
        {
        }

        /// <summary>
        /// For use with RNA loaded from a database
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="fivePrimeTerm"></param>
        /// <param name="threePrimeTerm"></param>
        /// <param name="name"></param>
        public RNA(string sequence, string name, string identifier, string organism, string databaseFilePath,
            IHasChemicalFormula? fivePrimeTerminus = null, IHasChemicalFormula? threePrimeTerminus = null,
            IDictionary<int, List<Modification>>? oneBasedPossibleModifications = null,
            bool isContaminant = false, bool isDecoy = false,
            Dictionary<string, string> databaseAdditionalsFields = null)
            : base(sequence, name, identifier, organism, databaseFilePath, fivePrimeTerminus, threePrimeTerminus,
                oneBasedPossibleModifications, isContaminant, isDecoy, databaseAdditionalsFields)
        {

        }
    }
}
