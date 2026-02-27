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
        [Fact]
        public void Truth_IsTrue()
        {
            Assert.True(true);
        }

        [Fact]
        public void Parser_CanParseRealVektisFile()
        {
            // find solution root
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CareMetrics.slnx")))
                dir = dir.Parent;
            Assert.NotNull(dir);
            var solutionRoot = dir!.FullName;

            var csvDir = Path.Combine(solutionRoot, "CareMetrics.API", "Data", "vektis", "postcode3");
            Assert.True(Directory.Exists(csvDir), $"dir should exist at {csvDir}");

            var files = Directory.GetFiles(csvDir, "*.csv");
            Assert.NotEmpty(files);

            var targetFile = files.FirstOrDefault(f => f.Contains("2023")) ?? files[0];

            // manually trace what the parser would do:
            using var reader = new StreamReader(targetFile);
            var headerLine = reader.ReadLine()!;
            var headers = headerLine.Split(';').Select(h => h.Trim().ToLowerInvariant()).ToArray();
            Assert.Equal(31, headers.Length);
            Assert.Equal("geslacht", headers[0]);
            Assert.Equal("aantal_verzekerdejaren", headers[4]);

            var dataLine = reader.ReadLine()!;
            var cols = dataLine.Split(';');
            Assert.Equal(headers.Length, cols.Length);

            // test decimal parsing of insured column
            var insuredStr = cols[4].Trim();
            var canParse = decimal.TryParse(insuredStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var insuredDec);
            Assert.True(canParse, $"Failed to parse insured value: [{insuredStr}] (len={insuredStr.Length}, bytes={string.Join(",", System.Text.Encoding.UTF8.GetBytes(insuredStr).Select(b => b.ToString("X2")))})");
            Assert.True((int)Math.Round(insuredDec) > 0, $"Insured={insuredDec}");

            reader.Close();

            // now run the actual parser
            var records = VektisCsvParser.ParseFile(targetFile);
            Assert.True(records.Count > 0,
                $"Parser produced 0 records from {Path.GetFileName(targetFile)}. " +
                $"Headers[4]=[{headers[4]}], insuredStr=[{insuredStr}], parsed={canParse}/{insuredDec}");
        }

        [Fact]
        public void Service_CanLoadSampleCsv_FromConfiguration()
        {
            // walk up from test output until we find the solution file
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CareMetrics.slnx")))
                dir = dir.Parent;
            Assert.NotNull(dir);
            var solutionRoot = dir!.FullName;

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
