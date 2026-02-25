using CareMetrics.API.Models;

namespace CareMetrics.API.Services;

public class VektisDataService : IVektisDataService
{
    private readonly List<VektisRecord> _data;

    private static readonly string[] CareTypes =
    [
        "huisartsenzorg",
        "ziekenhuiszorg",
        "ggz",
        "farmacie",
        "fysiotherapie",
        "tandheelkunde",
        "wijkverpleging"
    ];

    private static readonly string[] Municipalities =
    [
        "Amsterdam", "Rotterdam", "Den Haag", "Utrecht", "Eindhoven",
        "Groningen", "Tilburg", "Almere", "Breda", "Nijmegen",
        "Enschede", "Haarlem", "Arnhem", "Zaanstad", "Amersfoort",
        "Apeldoorn", "Maastricht", "Leiden", "Dordrecht", "Zwolle"
    ];

    private static readonly string[] AgeGroups = ["0-17", "18-44", "45-64", "65-74", "75+"];
    private static readonly string[] Genders = ["M", "V"];
    private static readonly int[] Years = [2019, 2020, 2021, 2022, 2023];

    // Base avg cost per insured per care type (realistic Dutch ZVW ballpark figures in EUR)
    private static readonly Dictionary<string, decimal> BaseCosts = new()
    {
        ["huisartsenzorg"] = 290m,
        ["ziekenhuiszorg"] = 1350m,
        ["ggz"] = 420m,
        ["farmacie"] = 310m,
        ["fysiotherapie"] = 175m,
        ["tandheelkunde"] = 210m,
        ["wijkverpleging"] = 680m
    };

    // Postcode3 ranges per municipality (simplified mapping)
    private static readonly Dictionary<string, string[]> MunicipalityPostcodes = new()
    {
        ["Amsterdam"] = ["100", "101", "102", "103", "104"],
        ["Rotterdam"] = ["300", "301", "302", "303"],
        ["Den Haag"] = ["250", "251", "252", "253"],
        ["Utrecht"] = ["350", "351", "352"],
        ["Eindhoven"] = ["560", "561", "562"],
        ["Groningen"] = ["971", "972", "973"],
        ["Tilburg"] = ["500", "501", "502"],
        ["Almere"] = ["130", "131", "132"],
        ["Breda"] = ["480", "481"],
        ["Nijmegen"] = ["653", "654"],
        ["Enschede"] = ["753", "754"],
        ["Haarlem"] = ["201", "202"],
        ["Arnhem"] = ["680", "681"],
        ["Zaanstad"] = ["150", "151"],
        ["Amersfoort"] = ["380", "381"],
        ["Apeldoorn"] = ["731", "732"],
        ["Maastricht"] = ["621", "622"],
        ["Leiden"] = ["231", "232"],
        ["Dordrecht"] = ["331", "332"],
        ["Zwolle"] = ["800", "801"]
    };

    public VektisDataService()
    {
        _data = GenerateMockData();
    }

    private static List<VektisRecord> GenerateMockData()
    {
        var rng = new Random(42); // fixed seed → reproducible results
        var records = new List<VektisRecord>(5000);

        // Age-group multipliers (older = higher cost)
        var ageCostMultiplier = new Dictionary<string, decimal>
        {
            ["0-17"] = 0.55m,
            ["18-44"] = 0.80m,
            ["45-64"] = 1.20m,
            ["65-74"] = 1.80m,
            ["75+"] = 2.60m
        };

        // Year trend: ~3% annual increase
        var yearMultiplier = new Dictionary<int, decimal>
        {
            [2019] = 0.88m,
            [2020] = 0.91m,
            [2021] = 0.94m,
            [2022] = 0.97m,
            [2023] = 1.00m
        };

        foreach (var year in Years)
            foreach (var municipality in Municipalities)
                foreach (var careType in CareTypes)
                    foreach (var ageGroup in AgeGroups)
                        foreach (var gender in Genders)
                        {
                            var postcodes = MunicipalityPostcodes[municipality];
                            var postcode = postcodes[rng.Next(postcodes.Length)];

                            var baseAvg = BaseCosts[careType];
                            var ageMult = ageCostMultiplier[ageGroup];
                            var yearMult = yearMultiplier[year];
                            // Small random jitter per municipality ±15%
                            var jitter = 1.0m + (decimal)(rng.NextDouble() * 0.30 - 0.15);

                            var avgCost = Math.Round(baseAvg * ageMult * yearMult * jitter, 2);
                            var insured = rng.Next(200, 12000);
                            var totalCost = Math.Round(avgCost * insured, 2);

                            records.Add(new VektisRecord
                            {
                                Year = year,
                                CareType = careType,
                                Municipality = municipality,
                                Postcode3 = postcode,
                                AgeGroup = ageGroup,
                                Gender = gender,
                                InsuredCount = insured,
                                TotalCost = totalCost,
                                AvgCostPerInsured = avgCost
                            });
                        }

        return records;
    }

    public IReadOnlyList<VektisRecord> GetAll() => _data;

    public IReadOnlyList<string> GetMunicipalities() =>
        _data.Select(r => r.Municipality).Distinct().Order().ToList();

    public IReadOnlyList<string> GetCareTypes() =>
        _data.Select(r => r.CareType).Distinct().Order().ToList();

    public List<CostSummary> GetCostsByMunicipality(string municipality)
    {
        var norm = municipality.Trim();
        return _data
            .Where(r => r.Municipality.Equals(norm, StringComparison.OrdinalIgnoreCase)
                     && r.Year == _data.Max(x => x.Year))
            .GroupBy(r => r.CareType)
            .Select(g => new CostSummary(
                Municipality: norm,
                CareType: g.Key,
                InsuredCount: g.Sum(r => r.InsuredCount),
                TotalCost: Math.Round(g.Sum(r => r.TotalCost), 2),
                AvgCostPerInsured: Math.Round(g.Sum(r => r.TotalCost) / g.Sum(r => r.InsuredCount), 2)
            ))
            .OrderByDescending(x => x.AvgCostPerInsured)
            .ToList();
    }

    public CostTrend GetCostTrend(string careType, int years)
    {
        var norm = careType.Trim().ToLowerInvariant();
        var maxYear = _data.Max(r => r.Year);
        var minYear = maxYear - years + 1;

        var trend = _data
            .Where(r => r.CareType.Equals(norm, StringComparison.OrdinalIgnoreCase)
                     && r.Year >= minYear)
            .GroupBy(r => r.Year)
            .OrderBy(g => g.Key)
            .Select(g => new YearlyDataPoint(
                Year: g.Key,
                AvgCostPerInsured: Math.Round(g.Sum(r => r.TotalCost) / g.Sum(r => r.InsuredCount), 2),
                TotalCost: Math.Round(g.Sum(r => r.TotalCost), 2),
                InsuredCount: g.Sum(r => r.InsuredCount)
            ))
            .ToList();

        return new CostTrend(careType, trend);
    }

    public List<DemographicUsage> GetDemographicUsage(string ageGroup, string gender)
    {
        var maxYear = _data.Max(r => r.Year);

        return _data
            .Where(r => r.AgeGroup.Equals(ageGroup, StringComparison.OrdinalIgnoreCase)
                     && r.Gender.Equals(gender, StringComparison.OrdinalIgnoreCase)
                     && r.Year == maxYear)
            .GroupBy(r => r.CareType)
            .Select(g => new DemographicUsage(
                AgeGroup: ageGroup,
                Gender: gender,
                CareType: g.Key,
                InsuredCount: g.Sum(r => r.InsuredCount),
                TotalCost: Math.Round(g.Sum(r => r.TotalCost), 2),
                AvgCostPerInsured: Math.Round(g.Sum(r => r.TotalCost) / g.Sum(r => r.InsuredCount), 2)
            ))
            .OrderByDescending(x => x.AvgCostPerInsured)
            .ToList();
    }

    public RegionComparison CompareRegions(string careType)
    {
        var norm = careType.Trim().ToLowerInvariant();
        var maxYear = _data.Max(r => r.Year);

        var regions = _data
            .Where(r => r.CareType.Equals(norm, StringComparison.OrdinalIgnoreCase)
                     && r.Year == maxYear)
            .GroupBy(r => r.Municipality)
            .Select(g => new RegionDataPoint(
                Municipality: g.Key,
                InsuredCount: g.Sum(r => r.InsuredCount),
                TotalCost: Math.Round(g.Sum(r => r.TotalCost), 2),
                AvgCostPerInsured: Math.Round(g.Sum(r => r.TotalCost) / g.Sum(r => r.InsuredCount), 2)
            ))
            .OrderByDescending(x => x.AvgCostPerInsured)
            .ToList();

        return new RegionComparison(careType, regions);
    }

    public List<Hotspot> GetHotspots(int n, string metric)
    {
        var maxYear = _data.Max(r => r.Year);

        var grouped = _data
            .Where(r => r.Year == maxYear)
            .GroupBy(r => new { r.Postcode3, r.Municipality });

        IEnumerable<(string Postcode3, string Municipality, string TopCareType, decimal Value)> scored;

        if (metric.Equals("insured", StringComparison.OrdinalIgnoreCase))
        {
            scored = grouped.Select(g =>
            (
                g.Key.Postcode3,
                g.Key.Municipality,
                TopCareType: g.OrderByDescending(r => r.InsuredCount).First().CareType,
                Value: (decimal)g.Sum(r => r.InsuredCount)
            ));
        }
        else // default: costs
        {
            scored = grouped.Select(g =>
            (
                g.Key.Postcode3,
                g.Key.Municipality,
                TopCareType: g.OrderByDescending(r => r.AvgCostPerInsured).First().CareType,
                Value: Math.Round(g.Sum(r => r.TotalCost) / g.Sum(r => r.InsuredCount), 2)
            ));
        }

        return scored
            .OrderByDescending(x => x.Value)
            .Take(n)
            .Select((x, i) => new Hotspot(
                Rank: i + 1,
                Postcode3: x.Postcode3,
                Municipality: x.Municipality,
                TopCareType: x.TopCareType,
                Value: x.Value
            ))
            .ToList();
    }
}
