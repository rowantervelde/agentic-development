using System.Globalization;
using System.Text.RegularExpressions;
using CareMetrics.API.Models;

namespace CareMetrics.API.Services;

/// <summary>
/// Parses real Vektis Open Data CSV files (semicolon-delimited, wide format)
/// and pivots them into the normalised <see cref="VektisRecord"/> model used
/// by the rest of the application.
///
/// The real CSV has one row per (gender × age-class × area) with many
/// individual cost columns.  This parser groups those columns into the 7
/// high-level care types used by the API and emits one
/// <see cref="VektisRecord"/> per care type per source row.
/// </summary>
public static partial class VektisCsvParser
{
    // ── care-type column mappings ──────────────────────────────────────
    // Each entry maps a care-type label to the set of CSV column prefixes
    // whose values should be summed.  Column names changed between years,
    // so we use prefix matching (case-insensitive).

    private static readonly (string CareType, string[] ColumnPrefixes)[] CareTypeMappings =
    [
        ("huisartsenzorg", [
            "kosten_huisarts_inschrijftarief",
            "kosten_huisarts_consult",
            "kosten_huisarts_mdz",
            "kosten_huisarts_overig"
        ]),
        ("ziekenhuiszorg", [
            "kosten_medisch_specialistische_z"   // covers both _zorg and _z
        ]),
        ("ggz", [
            "kosten_tweedelijns_ggz",                // 2011-2013
            "kosten_eerstelijns_psychologische_zorg", // 2011-2013
            "kosten_specialistische_ggz",             // 2014-2019
            "kosten_generalistische_basis_ggz",       // 2014-2019
            "kosten_langdurige_ggz",                  // 2014-2019
            "kosten_consulten_ggz",                   // 2020+
            "kosten_intramuraal_verblijf_ggz",        // 2020+
            "kosten_overige_prestaties_ggz",          // 2020+
            "kosten_innovatiegelden_ggz"              // 2022+
        ]),
        ("farmacie", [
            "kosten_farmacie"
        ]),
        ("fysiotherapie", [
            "kosten_paramedische_zorg_fysioth"  // covers _fysiotherapie and _fysioth
        ]),
        ("tandheelkunde", [
            "kosten_mondzorg"
        ]),
        ("wijkverpleging", [
            "kosten_verpleging_en_verzorging"
        ])
    ];

    // ── age-group normalisation ────────────────────────────────────────
    // The real CSV uses labels like " 5 t/m  9 jaar", "75 jaar en ouder",
    // "0" (for age 0).  We normalise to the 5 buckets used by the API.

    private static string NormaliseAgeGroup(string raw)
    {
        var s = raw.Trim().ToLowerInvariant();

        // exact single-digit ages (0, 1, …)
        if (int.TryParse(s, out var exact))
            return exact switch
            {
                <= 17 => "0-17",
                <= 44 => "18-44",
                <= 64 => "45-64",
                <= 74 => "65-74",
                _ => "75+"
            };

        // "75 jaar en ouder"
        if (s.Contains("75") && s.Contains("ouder"))
            return "75+";

        // "X t/m Y jaar" patterns
        var match = AgeRangeRegex().Match(s);
        if (match.Success)
        {
            var lo = int.Parse(match.Groups[1].Value);
            return lo switch
            {
                <= 17 => "0-17",
                <= 44 => "18-44",
                <= 64 => "45-64",
                <= 74 => "65-74",
                _ => "75+"
            };
        }

        // fallback: return trimmed original
        return raw.Trim();
    }

    [GeneratedRegex(@"(\d+)\s*t/m\s*(\d+)")]
    private static partial Regex AgeRangeRegex();

    // ── year extraction from filename ──────────────────────────────────

    private static int ExtractYear(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        var m = YearRegex().Match(name);
        return m.Success ? int.Parse(m.Value) : 0;
    }

    [GeneratedRegex(@"(?<!\d)(20\d{2})(?!\d)")]
    private static partial Regex YearRegex();

    // ── public API ─────────────────────────────────────────────────────

    /// <summary>
    /// Parse a single real Vektis CSV file and return normalised records.
    /// </summary>
    public static List<VektisRecord> ParseFile(string path)
    {
        var year = ExtractYear(path);
        if (year == 0)
            return [];

        var records = new List<VektisRecord>();

        using var reader = new StreamReader(path);

        // read header line
        var headerLine = reader.ReadLine();
        if (headerLine == null) return records;

        var headers = headerLine
            .Split(';')
            .Select(h => h.Trim().ToLowerInvariant())
            .ToArray();

        // find indices for fixed columns
        int geslachtIdx = Array.IndexOf(headers, "geslacht");
        int leeftijdIdx = Array.IndexOf(headers, "leeftijdsklasse");
        int postcode3Idx = Array.IndexOf(headers, "postcode_3");
        int gemeenteIdx = Array.IndexOf(headers, "gemeentenaam");
        int bsnIdx = Array.IndexOf(headers, "aantal_bsn");
        int verzekerdIdx = Array.IndexOf(headers, "aantal_verzekerdejaren");

        bool isPostcode = postcode3Idx >= 0;
        bool isGemeente = gemeenteIdx >= 0;

        // build per-care-type column index lists
        var careTypeColumns = new List<(string CareType, List<int> Indices)>();
        foreach (var (careType, prefixes) in CareTypeMappings)
        {
            var indices = new List<int>();
            for (int i = 0; i < headers.Length; i++)
            {
                foreach (var prefix in prefixes)
                {
                    if (headers[i].StartsWith(prefix, StringComparison.Ordinal))
                    {
                        indices.Add(i);
                        break;
                    }
                }
            }
            if (indices.Count > 0)
                careTypeColumns.Add((careType, indices));
        }

        // parse data rows
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(';');
            if (cols.Length < headers.Length) continue;

            var gender = geslachtIdx >= 0 ? cols[geslachtIdx].Trim() : "";
            var ageRaw = leeftijdIdx >= 0 ? cols[leeftijdIdx].Trim() : "";
            var ageGroup = NormaliseAgeGroup(ageRaw);
            var postcode = isPostcode ? cols[postcode3Idx].Trim() : "";
            var gemeente = isGemeente ? cols[gemeenteIdx].Trim() : "";
            var insuredStr = verzekerdIdx >= 0 ? cols[verzekerdIdx].Trim() : "";

            // parse insured-years as decimal, round to int
            if (!decimal.TryParse(insuredStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var insuredDec))
                continue;
            var insured = (int)Math.Round(insuredDec);
            if (insured <= 0) continue;

            foreach (var (careType, indices) in careTypeColumns)
            {
                decimal totalCost = 0;
                foreach (var idx in indices)
                {
                    if (decimal.TryParse(cols[idx].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                        totalCost += val;
                }

                // skip rows where this care type has no cost
                if (totalCost == 0) continue;

                records.Add(new VektisRecord
                {
                    Year = year,
                    CareType = careType,
                    Municipality = gemeente,
                    Postcode3 = postcode,
                    AgeGroup = ageGroup,
                    Gender = gender,
                    InsuredCount = insured,
                    TotalCost = Math.Round(totalCost, 2),
                    AvgCostPerInsured = Math.Round(totalCost / insured, 2)
                });
            }
        }

        return records;
    }

    /// <summary>
    /// Parse all CSV files in a directory and combine results.
    /// </summary>
    public static List<VektisRecord> ParseDirectory(string directory)
    {
        var all = new List<VektisRecord>();
        foreach (var file in Directory.GetFiles(directory, "*.csv", SearchOption.AllDirectories))
        {
            try
            {
                all.AddRange(ParseFile(file));
            }
            catch
            {
                // skip individual file errors
            }
        }
        return all;
    }
}
