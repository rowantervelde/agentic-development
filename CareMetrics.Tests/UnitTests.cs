using System.Collections.Generic;
using System.IO;
using CareMetrics.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareMetrics.Tests
{
    /// <summary>
    /// Shared fixture that resolves the solution root once for all integration tests.
    /// </summary>
    public class IntegrationTestFixture
    {
        public string SolutionRoot { get; }
        public string VektisDataPath { get; }
        public string Postcode3DataPath { get; }

        public IntegrationTestFixture()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CareMetrics.slnx")))
                dir = dir.Parent;

            Assert.NotNull(dir);
            SolutionRoot = dir!.FullName;
            VektisDataPath = Path.Combine(SolutionRoot, "CareMetrics.API", "Data", "vektis");
            Postcode3DataPath = Path.Combine(VektisDataPath, "postcode3");
        }
    }

    /// <summary>
    /// Integration tests for <see cref="VektisCsvParser"/>.
    /// </summary>
    public class VektisCsvParserTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;

        public VektisCsvParserTests(IntegrationTestFixture fixture)
            => _fixture = fixture;

        [Fact]
        public void ParseFile_CsvHeaders_HasExpectedStructure()
        {
            // Arrange
            Assert.True(Directory.Exists(_fixture.Postcode3DataPath),
                $"CSV dir should exist at {_fixture.Postcode3DataPath}");
            var files = Directory.GetFiles(_fixture.Postcode3DataPath, "*.csv");
            Assert.NotEmpty(files);
            var targetFile = files.FirstOrDefault(f => f.Contains("2023")) ?? files[0];

            // Act
            using var reader = new StreamReader(targetFile);
            var headerLine = reader.ReadLine()!;
            var headers = headerLine.Split(';').Select(h => h.Trim().ToLowerInvariant()).ToArray();

            // Assert
            Assert.Equal(31, headers.Length);
            Assert.Equal("geslacht", headers[0]);
            Assert.Equal("aantal_verzekerdejaren", headers[4]);
        }

        [Fact]
        public void ParseFile_DataRows_AreProperlyParsed()
        {
            // Arrange
            var files = Directory.GetFiles(_fixture.Postcode3DataPath, "*.csv");
            Assert.NotEmpty(files);
            var targetFile = files.FirstOrDefault(f => f.Contains("2023")) ?? files[0];

            using var reader = new StreamReader(targetFile);
            var headerLine = reader.ReadLine()!;
            var headers = headerLine.Split(';').Select(h => h.Trim().ToLowerInvariant()).ToArray();
            var dataLine = reader.ReadLine()!;
            var cols = dataLine.Split(';');

            // verify the insured-years column is parseable and then confirm the full parser produces records
            var insuredStr = cols[4].Trim();
            var canParse = decimal.TryParse(insuredStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var insuredDec);
            Assert.True(canParse,
                $"Failed to parse insured value: [{insuredStr}] " +
                $"(bytes={string.Join(",", System.Text.Encoding.UTF8.GetBytes(insuredStr).Select(b => b.ToString("X2")))})");
            Assert.True((int)Math.Round(insuredDec) > 0, $"Insured={insuredDec}");

            // Act
            var records = VektisCsvParser.ParseFile(targetFile);

            // Assert
            Assert.True(records.Count > 0,
                $"Parser produced 0 records from {Path.GetFileName(targetFile)}. " +
                $"Headers[4]=[{headers[4]}], insuredStr=[{insuredStr}], parsed={canParse}/{insuredDec}");
            Assert.All(records, r =>
            {
                Assert.False(string.IsNullOrEmpty(r.CareType));
                Assert.True(r.InsuredCount > 0);
                Assert.True(r.Year > 0);
            });
        }

        [Fact]
        public void ParseDirectory_ReturnsRecordsFromMultipleFiles()
        {
            // Arrange
            Assert.True(Directory.Exists(_fixture.Postcode3DataPath),
                $"postcode3 dir should exist at {_fixture.Postcode3DataPath}");

            // Act
            var records = VektisCsvParser.ParseDirectory(_fixture.Postcode3DataPath);

            // Assert – directory contains multiple years so we expect records from at least 2
            var years = records.Select(r => r.Year).Distinct().OrderBy(y => y).ToList();
            Assert.True(years.Count >= 2,
                $"Expected records from multiple years, got years: [{string.Join(", ", years)}]");
            Assert.True(records.Count > 0, "ParseDirectory should return records");
        }

        [Fact]
        public void ParseDirectory_EmptyDirectory_ReturnsEmptyList()
        {
            // Arrange
            var emptyDir = Path.Combine(Path.GetTempPath(), $"vektis_test_{Guid.NewGuid().ToString("N")}");
            Directory.CreateDirectory(emptyDir);

            try
            {
                // Act
                var records = VektisCsvParser.ParseDirectory(emptyDir);

                // Assert
                Assert.Empty(records);
            }
            finally
            {
                Directory.Delete(emptyDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Integration tests for <see cref="VektisDataService"/> loading real CSV data.
    /// </summary>
    public class VektisDataServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture _fixture;

        public VektisDataServiceIntegrationTests(IntegrationTestFixture fixture)
            => _fixture = fixture;

        [Fact]
        public void Service_CanLoadSampleCsv_FromConfiguration()
        {
            // Arrange
            Assert.True(Directory.Exists(_fixture.VektisDataPath),
                $"Vektis directory should exist at {_fixture.VektisDataPath}");
            Assert.NotEmpty(Directory.GetFiles(_fixture.VektisDataPath, "*.csv", SearchOption.AllDirectories));

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Vektis:CsvPath"] = _fixture.VektisDataPath
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddHttpClient();
            services.AddSingleton<IVektisDataService, VektisDataService>();

            var provider = services.BuildServiceProvider();
            var svc = provider.GetRequiredService<IVektisDataService>();

            // Act
            var all = svc.GetAll();

            // Assert
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

        [Theory]
        [InlineData(2011)]
        [InlineData(2023)]
        public void Service_LoadedData_ContainsExpectedYear(int year)
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Vektis:CsvPath"] = _fixture.VektisDataPath
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddHttpClient();
            services.AddSingleton<IVektisDataService, VektisDataService>();

            // Act
            var svc = services.BuildServiceProvider().GetRequiredService<IVektisDataService>();
            var all = svc.GetAll();

            // Assert
            Assert.Contains(all, r => r.Year == year);
        }
    }
}
