using System;
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
        private static string GetSolutionRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CareMetrics.slnx")))
                dir = dir.Parent;
            Assert.NotNull(dir);
            return dir!.FullName;
        }

        [Fact]
        public void Parser_CanParseRealVektisFile()
        {
            var solutionRoot = GetSolutionRoot();
            var csvDir = Path.Combine(solutionRoot, "CareMetrics.API", "Data", "vektis", "postcode3");
            Assert.True(Directory.Exists(csvDir), $"dir should exist at {csvDir}");

            var files = Directory.GetFiles(csvDir, "*.csv");
            Assert.NotEmpty(files);

            var targetFile = files.FirstOrDefault(f => f.Contains("2023")) ?? files[0];

            var records = VektisCsvParser.ParseFile(targetFile);

            Assert.True(records.Count > 0,
                $"Parser produced 0 records from {Path.GetFileName(targetFile)}");
            Assert.All(records, r =>
            {
                Assert.False(string.IsNullOrEmpty(r.CareType));
                Assert.True(r.Year > 2000);
                Assert.True(r.InsuredCount > 0);
            });
        }

        [Theory]
        [InlineData("2020")]
        [InlineData("2021")]
        [InlineData("2023")]
        public void Parser_ExtractsCorrectYear_FromFilename(string yearStr)
        {
            var solutionRoot = GetSolutionRoot();
            var csvDir = Path.Combine(solutionRoot, "CareMetrics.API", "Data", "vektis", "postcode3");

            var targetFile = Directory.GetFiles(csvDir, $"*{yearStr}*.csv").FirstOrDefault();
            Assert.NotNull(targetFile);

            var records = VektisCsvParser.ParseFile(targetFile!);

            Assert.NotEmpty(records);
            Assert.All(records, r => Assert.Equal(int.Parse(yearStr), r.Year));
        }

        [Fact]
        public void ParseDirectory_HappyPath_ReturnsRecords()
        {
            var solutionRoot = GetSolutionRoot();
            var csvDir = Path.Combine(solutionRoot, "CareMetrics.API", "Data", "vektis", "postcode3");
            Assert.True(Directory.Exists(csvDir), $"dir should exist at {csvDir}");

            var records = VektisCsvParser.ParseDirectory(csvDir);

            Assert.NotEmpty(records);
            var years = records.Select(r => r.Year).Distinct().OrderBy(y => y).ToList();
            Assert.True(years.Count >= 2, $"Expected records from multiple years, got: {string.Join(", ", years)}");
        }

        [Fact]
        public void ParseDirectory_EmptyDirectory_ReturnsEmpty()
        {
            var tempDir = Directory.CreateTempSubdirectory("vektis_empty_");
            try
            {
                var records = VektisCsvParser.ParseDirectory(tempDir.FullName);

                Assert.Empty(records);
            }
            finally
            {
                Directory.Delete(tempDir.FullName, recursive: true);
            }
        }

        [Fact]
        public void Service_CanLoadSampleCsv_FromConfiguration()
        {
            var solutionRoot = GetSolutionRoot();
            var csvPath = Path.Combine(solutionRoot, "CareMetrics.API", "Data", "vektis");
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
}
