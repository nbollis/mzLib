﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chemistry;
using MassSpectrometry;

namespace Transcriptomics
{
    /// <summary>
    /// Methods dealing with specific product type for RNA molecules
    /// </summary>
    public static class DissociationTypeCollection
    {
        /// <summary>
        /// Product Ion types by dissociation method
        /// </summary>
        private static readonly Dictionary<DissociationType, List<ProductType>> ProductsFromDissociationType =
            new Dictionary<DissociationType, List<ProductType>>()
            {
                { DissociationType.Unknown, new List<ProductType>() },
                {
                    DissociationType.CID,
                    new List<ProductType> { ProductType.w, ProductType.y, ProductType.aBase, ProductType.dWaterLoss }
                },
                { DissociationType.LowCID, new List<ProductType>() { } },
                { DissociationType.IRMPD, new List<ProductType>() { } },
                { DissociationType.ECD, new List<ProductType> { } },
                { DissociationType.PQD, new List<ProductType> { } },
                { DissociationType.ETD, new List<ProductType> { } },
                {
                    DissociationType.HCD,
                    new List<ProductType> { ProductType.w, ProductType.y, ProductType.aBase, ProductType.dWaterLoss }
                },
                { DissociationType.AnyActivationType, new List<ProductType> { } },
                { DissociationType.EThcD, new List<ProductType> { } },
                { DissociationType.Custom, new List<ProductType> { } },
                { DissociationType.ISCID, new List<ProductType> { } }
            };

        /// <summary>
        /// Returns list of products types based upon the dissociation type
        /// </summary>
        /// <param name="dissociationType"></param>
        /// <returns></returns>
        public static List<ProductType> GetRnaProductTypesFromDissociationType(this DissociationType dissociationType) =>
            ProductsFromDissociationType[dissociationType];


        /// <summary>
        /// Mass to be added or subtracted
        /// </summary>
        private static readonly Dictionary<ProductType, ChemicalFormula> FragmentIonCaps =
            new Dictionary<ProductType, ChemicalFormula>
            {
                { ProductType.a, ChemicalFormula.ParseFormula("H") },
                { ProductType.aWaterLoss, ChemicalFormula.ParseFormula("H-1O-1") },
                { ProductType.b, ChemicalFormula.ParseFormula("OH") },
                { ProductType.bWaterLoss, ChemicalFormula.ParseFormula("H-1") },
                { ProductType.c, ChemicalFormula.ParseFormula("O3H2P") },
                { ProductType.cWaterLoss, ChemicalFormula.ParseFormula("O2P") },
                { ProductType.d, ChemicalFormula.ParseFormula("O4H2P") },
                { ProductType.dWaterLoss, ChemicalFormula.ParseFormula("O3P") },

                { ProductType.w, ChemicalFormula.ParseFormula("H") },
                { ProductType.wWaterLoss, ChemicalFormula.ParseFormula("H-1O-1") },
                { ProductType.x, ChemicalFormula.ParseFormula("O-1H") },
                { ProductType.xWaterLoss, ChemicalFormula.ParseFormula("O-2H-1") },
                { ProductType.y, ChemicalFormula.ParseFormula("O-3P-1") },
                { ProductType.yWaterLoss, ChemicalFormula.ParseFormula("O-4H-2P-1") },
                { ProductType.z, ChemicalFormula.ParseFormula("O-4P-1") },
                { ProductType.zWaterLoss, ChemicalFormula.ParseFormula("O-5H-2P-1") },
                //fragment - Base chemical formula is the corresponding fragment chemical formula subtracing 1 H as H is lost when base is removed
                { ProductType.aBase, ChemicalFormula.ParseFormula("H-2") }, // "H-1" -H 
                { ProductType.bBase, ChemicalFormula.ParseFormula("O1H-2") }, //"OH1" -H
                { ProductType.cBase, ChemicalFormula.ParseFormula("O3H-1P") }, //"O3P" -H
                { ProductType.dBase, ChemicalFormula.ParseFormula("O4H-1P") }, //"O4H2P" -H

                { ProductType.wBase, ChemicalFormula.ParseFormula("H-2") }, //"H"-H
                { ProductType.xBase, ChemicalFormula.ParseFormula("O-1H-2") }, //"O-1H" -H
                { ProductType.yBase, ChemicalFormula.ParseFormula("O-3H-2P-1") }, //"O-3P-1" -H
                { ProductType.zBase, ChemicalFormula.ParseFormula("O-4H-3P-1") }, //"O-4H-1P-1" -1
            };

        /// <summary>
        /// Returns mass shift by product type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static double GetRnaMassShiftFromProductType(this ProductType type) => FragmentIonCaps[type].MonoisotopicMass;

        public static FragmentationTerminus GetRnaTerminusType(this ProductType fragmentType)
        {
            switch (fragmentType)
            {
                case ProductType.a:
                case ProductType.aWaterLoss:
                case ProductType.aBase:
                case ProductType.b:
                case ProductType.bWaterLoss:
                case ProductType.bBase:
                case ProductType.c:
                case ProductType.cWaterLoss:
                case ProductType.cBase:
                case ProductType.d:
                case ProductType.dWaterLoss:
                case ProductType.dBase:
                    return FragmentationTerminus.FivePrime;

                case ProductType.w:
                case ProductType.wWaterLoss:
                case ProductType.wBase:
                case ProductType.x:
                case ProductType.xWaterLoss:
                case ProductType.xBase:
                case ProductType.y:
                case ProductType.yWaterLoss:
                case ProductType.yBase:
                case ProductType.z:
                case ProductType.zWaterLoss:
                case ProductType.zBase:
                    return FragmentationTerminus.ThreePrime;

                case ProductType.aStar:
                case ProductType.aDegree:
                case ProductType.bAmmoniaLoss:
                case ProductType.yAmmoniaLoss:
                case ProductType.zPlusOne:
                case ProductType.M:
                case ProductType.D:
                case ProductType.Ycore:
                case ProductType.Y:
                default:
                    throw new ArgumentOutOfRangeException(nameof(fragmentType), fragmentType, null);
            }
        }

        /// <summary>
        /// Product ion types by Fragmentation Terminus
        /// </summary>
        private static readonly Dictionary<FragmentationTerminus, List<ProductType>>
            ProductIonTypesFromSpecifiedTerminus = new Dictionary<FragmentationTerminus, List<ProductType>>
            {
                {
                    FragmentationTerminus.FivePrime, new List<ProductType>
                    {
                        ProductType.a, ProductType.aWaterLoss, ProductType.aBase,
                        ProductType.b, ProductType.bWaterLoss, ProductType.bBase,
                        ProductType.c, ProductType.cWaterLoss, ProductType.cBase,
                        ProductType.d, ProductType.dWaterLoss, ProductType.dBase, 
                    }
                },
                {
                    FragmentationTerminus.ThreePrime, new List<ProductType>
                    {
                        ProductType.w, ProductType.wWaterLoss, ProductType.wBase,
                        ProductType.x, ProductType.xWaterLoss, ProductType.xBase,
                        ProductType.y, ProductType.yWaterLoss, ProductType.yBase,
                        ProductType.z, ProductType.zWaterLoss, ProductType.zBase,
                    }
                },
                {
                    FragmentationTerminus.Both, new List<ProductType>
                    {

                        ProductType.a, ProductType.aWaterLoss, ProductType.aBase,
                        ProductType.b, ProductType.bWaterLoss, ProductType.bBase,
                        ProductType.c, ProductType.cWaterLoss, ProductType.cBase,
                        ProductType.d, ProductType.dWaterLoss, ProductType.dBase, 
                        ProductType.w, ProductType.wWaterLoss, ProductType.wBase,
                        ProductType.x, ProductType.xWaterLoss, ProductType.xBase,
                        ProductType.y, ProductType.yWaterLoss, ProductType.yBase,
                        ProductType.z, ProductType.zWaterLoss, ProductType.zBase,
                    }
                }
            };


        public static List<ProductType> GetRnaTerminusSpecificProductTypes(
            this FragmentationTerminus fragmentationTerminus)
        {
            return ProductIonTypesFromSpecifiedTerminus[fragmentationTerminus];
        }

        /// <summary>
        /// Returns all product ion types based upon specified terminus
        /// </summary>
        /// <param name="dissociationType"></param>
        /// <param name="fragmentationTerminus"></param>
        /// <returns></returns>
        public static List<ProductType> GetRnaTerminusSpecificProductTypesFromDissociation(
            this DissociationType dissociationType, FragmentationTerminus fragmentationTerminus)
        {
            var terminusSpecific = fragmentationTerminus.GetRnaTerminusSpecificProductTypes();
            var dissociationSpecific = dissociationType.GetRnaProductTypesFromDissociationType();
            return terminusSpecific.Intersect(dissociationSpecific).ToList();
        }
    }
}