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
  in demo; Vektis schema preserved for easy swap-in
- **Singleton `VektisDataService`** — data is read-only/static in demo
