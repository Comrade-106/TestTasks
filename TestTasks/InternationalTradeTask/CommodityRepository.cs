using System;
using TestTasks.InternationalTradeTask.Models;

namespace TestTasks.InternationalTradeTask
{
    public class CommodityRepository
    {
        public double GetImportTariff(string commodityName)
        {
            return GetTariff(commodityName, isImport: true);
        }

        public double GetExportTariff(string commodityName)
        {
            return GetTariff(commodityName, isImport: false);
        }

        private double GetTariff(string commodityName, bool isImport)
        {
            foreach (var rootGroup in _allCommodityGroups)
            {
                double baseTariff = (isImport ? rootGroup.ImportTarif : rootGroup.ExportTarif) ?? 0.0;

                double? foundTariff = FindTariffRecursively(rootGroup, commodityName, baseTariff, isImport);

                if (foundTariff.HasValue)
                    return foundTariff.Value;
            }

            throw new ArgumentException($"Commodity '{commodityName}' not found.");
        }

        private double? FindTariffRecursively(ICommodityGroup group, string commodityName, double baseTariff, bool isImport)
        {
            double currentTariff = (isImport ? group.ImportTarif : group.ExportTarif) ?? baseTariff;

            if (group.Name == commodityName)
                return currentTariff;

            if (group.SubGroups != null)
            {
                foreach (var subGroup in group.SubGroups)
                {
                    double? result = FindTariffRecursively(subGroup, commodityName, currentTariff, isImport);

                    if (result.HasValue)
                        return result.Value;
                }
            }

            return null;
        }

        private FullySpecifiedCommodityGroup[] _allCommodityGroups = new FullySpecifiedCommodityGroup[]
        {
            new FullySpecifiedCommodityGroup("06", "Sugar, sugar preparations and honey", 0.05, 0)
            {
                SubGroups = new CommodityGroup[]
                {
                    new CommodityGroup("061", "Sugar and honey")
                    {
                        SubGroups = new CommodityGroup[]
                        {
                            new CommodityGroup("0611", "Raw sugar,beet & cane"),
                            new CommodityGroup("0612", "Refined sugar & other prod.of refining,no syrup"),
                            new CommodityGroup("0615", "Molasses", 0, 0),
                            new CommodityGroup("0616", "Natural honey", 0, 0),
                            new CommodityGroup("0619", "Sugars & syrups nes incl.art.honey & caramel"),
                        }
                    },
                    new CommodityGroup("062", "Sugar confy, sugar preps. Ex chocolate confy", 0, 0)
                }
            },
            new FullySpecifiedCommodityGroup("282", "Iron and steel scrap", 0, 0.1)
            {
                SubGroups = new CommodityGroup[]
                {
                    new CommodityGroup("28201", "Iron/steel scrap not sorted or graded"),
                    new CommodityGroup("28202", "Iron/steel scrap sorted or graded/cast iron"),
                    new CommodityGroup("28203", "Iron/steel scrap sort.or graded/tinned iron"),
                    new CommodityGroup("28204", "Rest of 282.0")
                }
            }
        };
    }
}
