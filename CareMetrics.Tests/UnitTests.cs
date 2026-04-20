using CareMetrics.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CareMetrics.Tests;

/// <summary>
/// Shared fixture that locates the solution root once for all tests that need
/// to resolve paths to real Vektis CSV data files.
/// </summary>
public class SolutionRootFixture
{
    public string SolutionRoot { get; }

    public SolutionRootFixture()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CareMetrics.slnx")))
            dir = dir.Parent;
        SolutionRoot = dir?.FullName
            ?? throw new DirectoryNotFoundException("CareMetrics.slnx not found");
    }
}

/// <summary>
/// Unit tests for <see cref="VektisCsvParser"/> — stateless, no DI required.
/// </summary>
public class VektisCsvParserTests : IClassFixture<SolutionRootFixture>
{
    private readonly string _solutionRoot;

    public VektisCsvParserTests(SolutionRootFixture fixture)
        => _solutionRoot = fixture.SolutionRoot;

    private string GetSampleCsvFile()
    {
        var csvDir = Path.Combine(_solutionRoot, "CareMetrics.API", "Data", "vektis", "postcode3");
        var files = Directory.GetFiles(csvDir, "*.csv");
        return files.FirstOrDefault(f => f.Contains("2023")) ?? files[0];
    }

    [Fact]
    public void ParseFile_RealVektisFile_ReturnsNonEmptyRecordsWithRequiredFields()
    {
        var csvFile = GetSampleCsvFile();

        var records = VektisCsvParser.ParseFile(csvFile);

        Assert.NotEmpty(records);
        Assert.All(records, r =>
        {
            Assert.False(string.IsNullOrEmpty(r.CareType));
            Assert.True(r.InsuredCount > 0);
            Assert.True(r.Year > 2000);
        });
    }

    [Fact]
    public void ParseFile_VektisFile_HasExpectedColumnCount()
    {
        var csvFile = GetSampleCsvFile();

        var headers = File.ReadLines(csvFile).First().Split(';');

        Assert.Equal(31, headers.Length);
    }

    [Fact]
    public void ParseFile_NullPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => VektisCsvParser.ParseFile(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("non_existent_file.csv")]
    public void ParseFile_InvalidPath_ReturnsEmpty(string path)
    {
        var records = VektisCsvParser.ParseFile(path);

        Assert.Empty(records);
    }

    [Fact]
    public void ParseDirectory_ValidDirectory_ReturnsRecordsFromAllFiles()
    {
        var dir = Path.Combine(_solutionRoot, "CareMetrics.API", "Data", "vektis", "postcode3");

        var records = VektisCsvParser.ParseDirectory(dir);

        Assert.NotEmpty(records);
        var firstFile = Directory.GetFiles(dir, "*.csv").First();
        var single = VektisCsvParser.ParseFile(firstFile);
        Assert.True(records.Count > single.Count,
            $"ParseDirectory ({records.Count}) should return more records than a single file ({single.Count})");
    }

    [Fact]
    public void ParseDirectory_EmptyDirectory_ReturnsEmpty()
    {
        var tempDir = Directory.CreateTempSubdirectory("vektis-empty-");
        try
        {
            var records = VektisCsvParser.ParseDirectory(tempDir.FullName);

            Assert.Empty(records);
        }
        finally { tempDir.Delete(recursive: true); }
    }
}

/// <summary>
/// Integration tests for <see cref="VektisDataService"/> wired via DI with real CSV data.
/// </summary>
public class VektisServiceIntegrationTests : IClassFixture<SolutionRootFixture>
{
    private readonly string _solutionRoot;

    public VektisServiceIntegrationTests(SolutionRootFixture fixture)
        => _solutionRoot = fixture.SolutionRoot;

    [Fact]
    public void Service_CanLoadSampleCsv_FromConfiguration()
    {
        var csvPath = Path.Combine(_solutionRoot, "CareMetrics.API", "Data", "vektis");
        Assert.True(Directory.Exists(csvPath), $"downloaded vektis directory should exist at {csvPath}");
        // ensure at least one CSV is present (recursively – postcode3 + gemeente)
        Assert.NotEmpty(Directory.GetFiles(csvPath, "*.csv", SearchOption.AllDirectories));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Vektis:CsvPath"] = csvPath
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddHttpClient();
        services.AddSingleton<IVektisDataService, VektisDataService>();

        var provider = services.BuildServiceProvider();
        var svc = provider.GetRequiredService<IVektisDataService>();

        var all = svc.GetAll();
        Assert.NotEmpty(all);
        // both postcode3 and gemeente data should be loaded
        Assert.Contains(all, r => r.CareType == "huisartsenzorg");
        Assert.Contains(all, r => !string.IsNullOrEmpty(r.Postcode3));
        Assert.Contains(all, r => !string.IsNullOrEmpty(r.Municipality));

        // verify multiple years loaded (full directory has 13 years)
        var years = all.Select(r => r.Year).Distinct().ToList();
        Assert.True(years.Count >= 2, $"Expected multiple years, got {years.Count}");

        // verify reasonable record count (13 years × thousands of rows)
        Assert.True(all.Count > 10000, $"Expected >10k records, got {all.Count}");
    }
}
