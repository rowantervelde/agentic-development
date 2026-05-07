using System.Globalization;
using CareMetrics.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CareMetrics.Tests;

public class SolutionRootFixture
{
    public string SolutionRoot { get; }

    public SolutionRootFixture()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CareMetrics.slnx")))
            dir = dir.Parent;

        Assert.NotNull(dir);
        SolutionRoot = dir!.FullName;
    }
}

public class UnitTests : IClassFixture<SolutionRootFixture>
{
    private readonly string _solutionRoot;

    public UnitTests(SolutionRootFixture fixture)
        => _solutionRoot = fixture.SolutionRoot;

    // ── Parser_CanParseRealVektisFile (split into focused tests) ──────

    [Fact]
    public void Parser_RealVektisFile_HasExpectedHeaders()
    {
        var csvDir = Path.Combine(_solutionRoot, "CareMetrics.API", "Data", "vektis", "postcode3");
        Assert.True(Directory.Exists(csvDir), $"dir should exist at {csvDir}");
        var files = Directory.GetFiles(csvDir, "*.csv");
        Assert.NotEmpty(files);
        var targetFile = files.FirstOrDefault(f => f.Contains("2023")) ?? files[0];

        using var reader = new StreamReader(targetFile);
        var headers = reader.ReadLine()!.Split(';').Select(h => h.Trim().ToLowerInvariant()).ToArray();

        Assert.Equal(31, headers.Length);
        Assert.Equal("geslacht", headers[0]);
        Assert.Equal("aantal_verzekerdejaren", headers[4]);
    }

    [Fact]
    public void Parser_RealVektisFile_InsuredColumnParsesAsPositiveDecimal()
    {
        var csvDir = Path.Combine(_solutionRoot, "CareMetrics.API", "Data", "vektis", "postcode3");
        var files = Directory.GetFiles(csvDir, "*.csv");
        var targetFile = files.FirstOrDefault(f => f.Contains("2023")) ?? files[0];

        using var reader = new StreamReader(targetFile);
        var headers = reader.ReadLine()!.Split(';').Select(h => h.Trim().ToLowerInvariant()).ToArray();
        var cols = reader.ReadLine()!.Split(';');

        Assert.Equal(headers.Length, cols.Length);

        var insuredStr = cols[4].Trim();
        var canParse = decimal.TryParse(insuredStr, NumberStyles.Any,
            CultureInfo.InvariantCulture, out var insuredDec);

        Assert.True(canParse,
            $"Failed to parse insured value: [{insuredStr}] " +
            $"(bytes={string.Join(",", System.Text.Encoding.UTF8.GetBytes(insuredStr).Select(b => b.ToString("X2")))})");
        Assert.True((int)Math.Round(insuredDec) > 0, $"Insured={insuredDec}");
    }

    [Fact]
    public void Parser_RealVektisFile_ProducesRecords()
    {
        var csvDir = Path.Combine(_solutionRoot, "CareMetrics.API", "Data", "vektis", "postcode3");
        var files = Directory.GetFiles(csvDir, "*.csv");
        var targetFile = files.FirstOrDefault(f => f.Contains("2023")) ?? files[0];

        var records = VektisCsvParser.ParseFile(targetFile);

        Assert.True(records.Count > 0, $"Parser produced 0 records from {Path.GetFileName(targetFile)}");
    }

    // ── ParseFile edge cases ──────────────────────────────────────────

    [Theory]
    [InlineData("no_year_here.csv", "")]                  // no year in filename → year=0 → empty
    [InlineData("data_2023.csv", "")]                     // year present, empty file → no header → empty
    [InlineData("data_2023.csv", "col1;col2\n")]          // year present, header only, no data rows → empty
    public void ParseFile_EdgeCases_ReturnsEmptyList(string fileName, string content)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"vektis_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var tempFile = Path.Combine(tempDir, fileName);
            File.WriteAllText(tempFile, content);

            var result = VektisCsvParser.ParseFile(tempFile);

            Assert.Empty(result);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // ── ParseDirectory ────────────────────────────────────────────────

    [Fact]
    public void ParseDirectory_WithRealFiles_ReturnsRecords()
    {
        var csvDir = Path.Combine(_solutionRoot, "CareMetrics.API", "Data", "vektis", "postcode3");
        Assert.True(Directory.Exists(csvDir), $"dir should exist at {csvDir}");

        var records = VektisCsvParser.ParseDirectory(csvDir);

        Assert.True(records.Count > 0, $"ParseDirectory produced 0 records from {csvDir}");
    }

    [Fact]
    public void ParseDirectory_EmptyDirectory_ReturnsEmptyList()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"vektis_empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var result = VektisCsvParser.ParseDirectory(tempDir);

            Assert.Empty(result);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // ── Service_CanLoadSampleCsv_FromConfiguration ────────────────────

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
