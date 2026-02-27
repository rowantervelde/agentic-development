# Plan: Add CareMetrics Controller with 5 Endpoints

## Summary

The project is a clean .NET 10 minimal API scaffold. This plan adds a
controller-based layer, wires up a data service backed by Vektis-shaped
in-memory mock data, and exposes 5 endpoints under `/api/costs`,
`/api/demographics`, `/api/compare`, and `/api/hotspots`.
The mock data mirrors the real Vektis Open Data CSV schema so a live CSV
file can be swapped in later with minimal changes.

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

7. **Seed realistic mock data** in `VektisDataService`
   - ~500–1000 `VektisRecord` rows
   - Multiple years (2019–2023), municipalities, care types, demographics
   - (Later) allow swapping in a real Vektis CSV via configuration (`Vektis:CsvPath` or `Vektis:CsvUrl`)
      so that production apps can ingest the actual dataset instead of generated values
      *The path can also be a directory containing multiple CSVs (useful after
      downloading the full archive with the script).

8. **Verify Swagger UI** at `/swagger` shows all 5 routes with parameters

---

## Verification

- `dotnet build` — zero errors
- `/swagger` — all 5 endpoints visible and documented
- `/api/hotspots/topN?n=5&metric=costs` — returns top 5 postcode areas
- `/api/costs/trend/huisartsenzorg?years=3` — returns 3-year GP cost trend

---

## Decisions

- **Controller over minimal API groups** — explicitly requested
- **Mock data over live CSV download** — removes runtime file dependency
  in demo; Vektis schema preserved for easy swap-in.  The service also supports
  reading a real CSV when pointed to one via configuration, making the switch
  trivial once a dataset has been downloaded.
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
