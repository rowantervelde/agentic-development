using CareMetrics.API.Models;

namespace CareMetrics.API.Services;

public interface IVektisDataService
{
    IReadOnlyList<VektisRecord> GetAll();
    IReadOnlyList<string> GetMunicipalities();
    IReadOnlyList<string> GetCareTypes();

    List<CostSummary> GetCostsByMunicipality(string municipality);
    CostTrend GetCostTrend(string careType, int years);
    List<DemographicUsage> GetDemographicUsage(string ageGroup, string gender);
    RegionComparison CompareRegions(string careType);
    List<Hotspot> GetHotspots(int n, string metric);
}
