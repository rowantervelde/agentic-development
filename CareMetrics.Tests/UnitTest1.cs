using CareMetrics.API.Services;

namespace CareMetrics.Tests;

public class VektisServiceFixture
{
    public IVektisDataService Service { get; } = new VektisDataService();
}

public class VektisDataServiceTests : IClassFixture<VektisServiceFixture>
{
    // 5 years × 20 municipalities × 7 care types × 5 age groups × 2 genders
    private const int ExpectedMinimumMockRecords = 6500;

    private readonly IVektisDataService _svc;

    public VektisDataServiceTests(VektisServiceFixture fixture)
        => _svc = fixture.Service;

    // ── GetAll ────────────────────────────────────────────────────────
    [Fact]
    public void GetAll_ReturnsMockRecords()
    {
        var all = _svc.GetAll();

        Assert.NotEmpty(all);
        Assert.True(all.Count > ExpectedMinimumMockRecords, $"Expected >{ExpectedMinimumMockRecords} mock records, got {all.Count}");
        Assert.All(all, r => Assert.False(string.IsNullOrEmpty(r.CareType)));
    }

    // ── GetMunicipalities ─────────────────────────────────────────────
    [Fact]
    public void GetMunicipalities_ReturnsDistinctSortedValues()
    {
        var municipalities = _svc.GetMunicipalities();

        Assert.NotEmpty(municipalities);
        Assert.Equal(municipalities.Distinct().Count(), municipalities.Count);
        Assert.Equal(municipalities.OrderBy(x => x).ToList(), municipalities.ToList());
        Assert.Contains("Amsterdam", municipalities);
    }

    // ── GetCareTypes ──────────────────────────────────────────────────
    [Fact]
    public void GetCareTypes_ReturnsKnownCareTypes()
    {
        var careTypes = _svc.GetCareTypes();

        Assert.NotEmpty(careTypes);
        Assert.Contains("huisartsenzorg", careTypes);
        Assert.Contains("ziekenhuiszorg", careTypes);
    }

    // ── GetCostsByMunicipality ────────────────────────────────────────
    [Theory]
    [InlineData("Amsterdam", true)]
    [InlineData("Rotterdam", true)]
    [InlineData("NonExistentCity", false)]
    [InlineData("", false)]
    public void GetCostsByMunicipality_VariousInputs_ReturnsExpected(
        string municipality, bool expectData)
    {
        var result = _svc.GetCostsByMunicipality(municipality);

        if (expectData)
        {
            Assert.NotEmpty(result);
            Assert.All(result, s => Assert.Equal(municipality, s.Municipality));
        }
        else
        {
            Assert.Empty(result);
        }
    }

    // ── GetCostTrend ──────────────────────────────────────────────────
    [Theory]
    [InlineData("huisartsenzorg", 1)]
    [InlineData("farmacie", 3)]
    [InlineData("ggz", 5)]
    public void GetCostTrend_ValidInputs_ReturnsTrendData(string careType, int years)
    {
        var result = _svc.GetCostTrend(careType, years);

        Assert.NotNull(result);
        Assert.Equal(careType, result.CareType);
        Assert.NotEmpty(result.Trend);
        Assert.Equal(years, result.Trend.Count);
    }

    [Fact]
    public void GetCostTrend_UnknownCareType_ReturnsEmptyTrend()
    {
        var result = _svc.GetCostTrend("nonexistenttype", 5);

        Assert.NotNull(result);
        Assert.Empty(result.Trend);
    }

    // ── GetDemographicUsage ───────────────────────────────────────────
    [Theory]
    [InlineData("0-17", "M")]
    [InlineData("75+", "V")]
    [InlineData("45-64", "M")]
    public void GetDemographicUsage_ValidInputs_ReturnsData(string ageGroup, string gender)
    {
        var result = _svc.GetDemographicUsage(ageGroup, gender);

        Assert.NotEmpty(result);
        Assert.All(result, d =>
        {
            Assert.Equal(ageGroup, d.AgeGroup);
            Assert.Equal(gender, d.Gender);
        });
    }

    // ── GetHotspots ───────────────────────────────────────────────────
    [Theory]
    [InlineData(5, "costs")]
    [InlineData(10, "insured")]
    [InlineData(1, "costs")]
    public void GetHotspots_ValidInputs_ReturnsCorrectCount(int n, string metric)
    {
        var result = _svc.GetHotspots(n, metric);

        Assert.Equal(n, result.Count);
        Assert.All(result, h => Assert.True(h.Rank > 0));
        // ranks are sequential 1..n
        for (int i = 0; i < result.Count; i++)
            Assert.Equal(i + 1, result[i].Rank);
    }

    // ── CompareRegions ────────────────────────────────────────────────
    [Theory]
    [InlineData("huisartsenzorg")]
    [InlineData("ziekenhuiszorg")]
    public void CompareRegions_KnownCareType_ReturnsAllMunicipalities(string careType)
    {
        var result = _svc.CompareRegions(careType);

        Assert.NotNull(result);
        Assert.Equal(careType, result.CareType);
        Assert.NotEmpty(result.Regions);
        Assert.Equal(20, result.Regions.Count);
        Assert.Contains(result.Regions, r => r.Municipality == "Amsterdam");
    }
}

