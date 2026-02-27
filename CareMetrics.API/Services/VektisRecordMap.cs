using CareMetrics.API.Models;
using CsvHelper.Configuration;

namespace CareMetrics.API.Services
{
    /// <summary>
    /// CsvHelper mapping for <see cref="VektisRecord"/>.
    /// The real Vektis open data header names are mostly English but sometimes
    /// Dutch; we accept a handful of common variants so the service can parse
    /// either raw downloads or transformed files.
    /// </summary>
    public sealed class VektisRecordMap : ClassMap<VektisRecord>
    {
        public VektisRecordMap()
        {
            Map(m => m.Year)
                .Name("Year", "year", "Jaar", "jaar");
            Map(m => m.CareType)
                .Name("CareType", "careType", "zorgsoort", "Zorgsoort", "zorgtype", "Zorgtype");
            Map(m => m.Municipality)
                .Name("Municipality", "municipality", "gemeente", "Gemeente");
            Map(m => m.Postcode3)
                .Name("Postcode3", "postcode3", "Postcode", "postcode");
            Map(m => m.AgeGroup)
                .Name("AgeGroup", "ageGroup", "leeftijdsgroep", "Leeftijdsgroep");
            Map(m => m.Gender)
                .Name("Gender", "gender", "geslacht", "Geslacht");
            Map(m => m.InsuredCount)
                .Name("InsuredCount", "insuredCount", "verzekerden", "Verzekerden");
            Map(m => m.TotalCost)
                .Name("TotalCost", "totalCost", "TotaleKosten", "totaleKosten", "kosten", "Kosten");
            Map(m => m.AvgCostPerInsured)
                .Name("AvgCostPerInsured", "avgCostPerInsured", "gemiddeldeKostenPerVerzekerde", "GemiddeldeKostenPerVerzekerde");
        }
    }
}
