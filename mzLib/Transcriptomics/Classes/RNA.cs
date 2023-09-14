using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;

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
        /// For use with Modomics Databases
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="fivePrimeTerm"></param>
        /// <param name="threePrimeTerm"></param>
        /// <param name="name"></param>
        public RNA(string sequence, int dbId, string name, string dbFilePath, bool isDecoy, 
            bool isContaminant, string organism, string soTerm, string rnaType,
            string rnaSubType, string rnaFeature, string cellularOrganization)
            : base(sequence)
        {
            //DatabaseId = dbId;
            Name = name;
            DatabaseFilePath = dbFilePath;
            IsDecoy = isDecoy;
            IsContaminant = isContaminant;
            Organism = organism;
            //SoTerm = soTerm;
            //RnaType = rnaType;
            //RnaSubType = rnaSubType;
            //RnaFeature = rnaFeature;
            //CellularOrganization = cellularOrganization;
        }
    }
}
