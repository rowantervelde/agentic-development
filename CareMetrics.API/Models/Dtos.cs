namespace CareMetrics.API.Models;

public record CostSummary(
    string Municipality,
    string CareType,
    int InsuredCount,
    decimal TotalCost,
    decimal AvgCostPerInsured
);

public record CostTrend(
    string CareType,
    List<YearlyDataPoint> Trend
);

public record YearlyDataPoint(
    int Year,
    decimal AvgCostPerInsured,
    decimal TotalCost,
    int InsuredCount
);

public record DemographicUsage(
    string AgeGroup,
    string Gender,
    string CareType,
    decimal AvgCostPerInsured,
    int InsuredCount,
    decimal TotalCost
);

public record RegionComparison(
    string CareType,
    List<RegionDataPoint> Regions
);

public record RegionDataPoint(
    string Municipality,
    decimal AvgCostPerInsured,
    int InsuredCount,
    decimal TotalCost
);

public record Hotspot(
    int Rank,
    string Postcode3,
    string Municipality,
    string TopCareType,
    decimal Value
);
