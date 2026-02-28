# Plan: Add CareMetrics Controller with 7 Endpoints

> **Status: Completed** — All steps below have been implemented.
> The controller, service, models, and CSV ingestion are fully wired up.

## Summary

The project is a clean .NET 10 minimal API scaffold. This plan added a
controller-based layer, wired up a data service backed by Vektis CSV data,
and exposed 7 endpoints under `/api/costs`, `/api/demographics`,
`/api/compare`, `/api/hotspots`, and `/api/metadata`.
The service loads real Vektis Open Data CSVs via configuration
(`Vektis:CsvPath` or `Vektis:CsvUrl`) and falls back to seed data when
no CSV is configured.

---

## Steps

1. **Enable controllers** in `CareMetrics.API/Program.cs`
   - Add `builder.Services.AddControllers()`
   - Add `app.MapControllers()`
   - Remove the `WeatherForecast` stub

2. **Add `CsvHelper` NuGet package** to `CareMetrics.API.csproj`
   - Used for CSV parsing when real Vektis data is loaded
   - Keeps mock→real swap trivial
   - A helper script (`scripts/download-vektis-data.ps1`) can be used to
     download all historic files; point the service at the resulting directory
     instead of a single file.

3. **Create models** in a new `Models/` folder
   - `VektisRecord.cs` — maps Vektis CSV row:
     year, careType, municipality, postcode3, ageGroup, gender,
     insuredCount, totalCost, avgCostPerInsured
   - Response DTOs: `CostSummary`, `CostTrend`, `DemographicUsage`,
     `RegionComparison`, `Hotspot`

4. **Create `IVektisDataService` + `VektisDataService`** in `Services/`
   - Loads/seeds data on startup
   - Exposes query methods used by the controller:
     `GetCostsByMunicipality`, `GetCostTrend`, `GetDemographicUsage`,
     `CompareRegions`, `GetHotspots`

5. **Register `VektisDataService`** as a singleton in `Program.cs`

6. **Create `Controllers/CareMetricsController.cs`**
   - `[ApiController]`, `[Route("api")]`

   | Method | Route | Description |
   |--------|-------|-------------|
   | GET | `/api/costs/municipality/{gemeente}` | Avg cost per insured for a municipality |
   | GET | `/api/costs/trend/{careType}?years=N` | Year-over-year cost trend for a care type |
   | GET | `/api/demographics/{ageGroup}/{gender}` | Usage patterns by age + gender |
   | GET | `/api/compare/regions?type={careType}` | Side-by-side regional comparison |
   | GET | `/api/hotspots/topN?n=10&metric=costs` | Top-N most expensive postcodes |
   | GET | `/api/metadata/municipalities` | List all available municipalities |
   | GET | `/api/metadata/caretypes` | List all available care types |

7. **Seed data + real CSV ingestion** in `VektisDataService`
   - Seed data provides ~500–1000 `VektisRecord` rows for demo use
   - Multiple years (2019–2023), municipalities, care types, demographics
   - Real Vektis CSVs can be loaded via `Vektis:CsvPath` or `Vektis:CsvUrl`
     configuration; the path can be a directory containing multiple CSVs
     (useful after downloading the full archive with the script)

8. **Verify Swagger UI** at `/swagger` shows all 7 routes with parameters

---

## Verification

- `dotnet build` — zero errors
- `/swagger` — all 7 endpoints visible and documented
- `/api/hotspots/topN?n=5&metric=costs` — returns top 5 postcode areas
- `/api/costs/trend/huisartsenzorg?years=3` — returns 3-year GP cost trend
- `/api/metadata/municipalities` — returns all known municipalities
- `/api/metadata/caretypes` — returns all known care types

---

## Decisions

- **Controller over minimal API groups** — explicitly requested
- **Seed data + live CSV support** — seed data removes runtime file
  dependency in demo; the service also loads real Vektis CSVs when pointed
  to a file or directory via `Vektis:CsvPath` or `Vektis:CsvUrl`.
- **Singleton `VektisDataService`** — data is read-only/static in demo

### Downloading real data
There’s a small helper script at `CareMetrics.API/scripts/download-vektis-data.ps1` which will pull every CSV file published by Vektis (2011–2023, both postcode3 and gemeentebestand) into `CareMetrics.API/Data/vektis/`.

Run it from the repo root:

```powershell
pwsh -File CareMetrics.API\scripts\download-vektis-data.ps1
```

Once the files are on disk you can point the service at either a single file or
an entire directory. Pointing at the full `postcode3` folder will combine all 13
years of data – the resulting dataset contains hundreds of thousands of rows and
can be used for more realistic testing.

Example using the postcode3 directory:

```powershell
set Vektis__CsvPath=C:\git\github\agentic-development\CareMetrics.API\Data\vektis\postcode3

# start the app; it will ingest all CSVs in that directory
dotnet run --urls "https://localhost:7002"
```

For quick experiments you can also mount the smaller sample file described above.
The service will read every `.csv` file in that folder at startup and combine
the records automatically.
