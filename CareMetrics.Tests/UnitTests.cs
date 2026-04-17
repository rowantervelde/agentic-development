using System.Collections.Generic;
using System.IO;
using CareMetrics.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareMetrics.Tests
{
    public class UnitTests
    {
        // ── Parser.CsvHeader ──────────────────────────────────────────────────

        [Fact]
        public void Parser_CsvHeaderHasExpectedColumns()
        {
            var targetFile = FindVektisCsvFile();
            using var reader = new StreamReader(targetFile);
            var headers = reader.ReadLine()!.Split(';')
                .Select(h => h.Trim().ToLowerInvariant()).ToArray();

            Assert.Equal(31, headers.Length);
            Assert.Equal("geslacht", headers[0]);
            Assert.Equal("aantal_verzekerdejaren", headers[4]);
        }

        // ── Parser.ParseFile – happy path ─────────────────────────────────────

        [Fact]
        public void Parser_ParseFile_ReturnsNonEmptyRecords()
        {
            var targetFile = FindVektisCsvFile();

            var records = VektisCsvParser.ParseFile(targetFile);

            Assert.NotEmpty(records);
            Assert.All(records, r => Assert.False(string.IsNullOrEmpty(r.CareType)));
            Assert.All(records, r => Assert.True(r.InsuredCount > 0));
        }

        // ── Parser.ParseFile – edge cases ─────────────────────────────────────

        [Theory]
        [InlineData("vektis_no_year.csv")]   // no year in filename → returns empty
        [InlineData("vektis_empty.csv")]     // empty file → returns empty
        public void Parser_ParseFile_EdgeCases_ReturnsEmpty(string fileName)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            var path = Path.Combine(tempDir, fileName);
            File.WriteAllText(path, "");

            try
            {
                var result = VektisCsvParser.ParseFile(path);

                Assert.Empty(result);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        // ── Parser.ParseDirectory – edge cases ───────────────────────────────

        [Fact]
        public void Parser_ParseDirectory_EmptyDirectory_ReturnsEmpty()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                var result = VektisCsvParser.ParseDirectory(tempDir);

                Assert.Empty(result);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        // ── shared helper ─────────────────────────────────────────────────────

        private static string FindVektisCsvFile()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CareMetrics.slnx")))
                dir = dir.Parent;
            Assert.NotNull(dir);
            var csvDir = Path.Combine(dir!.FullName, "CareMetrics.API", "Data", "vektis", "postcode3");
            var files = Directory.GetFiles(csvDir, "*.csv");
            Assert.NotEmpty(files);
            return files.FirstOrDefault(f => f.Contains("2023")) ?? files[0];
        }
    }

    // ── Integration tests (reads from filesystem at runtime) ─────────────────

    public class VektisDataServiceIntegrationTests
    {
        [Fact]
        public void Service_LoadsRealCsvData_FromConfiguration()
        {
            // walk up from test output until we find the solution file
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CareMetrics.slnx")))
                dir = dir.Parent;
            Assert.NotNull(dir);
            var solutionRoot = dir!.FullName;

            var csvPath = Path.Combine(solutionRoot, "CareMetrics.API", "Data", "vektis");
            Assert.True(Directory.Exists(csvPath));
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
}
