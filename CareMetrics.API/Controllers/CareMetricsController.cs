using CareMetrics.API.Models;
using CareMetrics.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CareMetrics.API.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class CareMetricsController(IVektisDataService dataService) : ControllerBase
{
    /// <summary>
    /// Returns average cost per insured person for all care types in a given municipality.
    /// </summary>
    /// <param name="gemeente">Municipality name, e.g. "Amsterdam"</param>
    [HttpGet("costs/municipality/{gemeente}")]
    [ProducesResponseType(typeof(IEnumerable<CostSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetCostsByMunicipality(string gemeente)
    {
        var result = dataService.GetCostsByMunicipality(gemeente);
        if (result.Count == 0)
            return NotFound(new { message = $"No data found for municipality '{gemeente}'." });

        return Ok(result);
    }

    /// <summary>
    /// Returns year-over-year cost trend for a specific care type.
    /// </summary>
    /// <param name="careType">Care type in Dutch, e.g. "huisartsenzorg", "ziekenhuiszorg"</param>
    /// <param name="years">Number of years to include (default: 5)</param>
    [HttpGet("costs/trend/{careType}")]
    [ProducesResponseType(typeof(CostTrend), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetCostTrend(string careType, [FromQuery] int years = 5)
    {
        if (years < 1 || years > 20)
            return BadRequest(new { message = "Parameter 'years' must be between 1 and 20." });

        var result = dataService.GetCostTrend(careType, years);
        if (result.Trend.Count == 0)
            return NotFound(new { message = $"No data found for care type '{careType}'." });

        return Ok(result);
    }

    /// <summary>
    /// Returns healthcare usage and cost patterns for a specific age group and gender.
    /// </summary>
    /// <param name="ageGroup">Age group: "0-17", "18-44", "45-64", "65-74" or "75+"</param>
    /// <param name="gender">"M" (male) or "V" (female)</param>
    [HttpGet("demographics/{ageGroup}/{gender}")]
    [ProducesResponseType(typeof(IEnumerable<DemographicUsage>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetDemographicUsage(string ageGroup, string gender)
    {
        var validAgeGroups = new[] { "0-17", "18-44", "45-64", "65-74", "75+" };
        var validGenders = new[] { "M", "V" };

        if (!validAgeGroups.Contains(ageGroup, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = $"Invalid age group. Valid values: {string.Join(", ", validAgeGroups)}" });

        if (!validGenders.Contains(gender, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = "Invalid gender. Valid values: M, V" });

        var result = dataService.GetDemographicUsage(ageGroup, gender);
        if (result.Count == 0)
            return NotFound(new { message = "No data found for the specified demographic." });

        return Ok(result);
    }

    /// <summary>
    /// Returns side-by-side cost comparison across all municipalities for a care type.
    /// </summary>
    /// <param name="type">Care type in Dutch, e.g. "farmacie"</param>
    [HttpGet("compare/regions")]
    [ProducesResponseType(typeof(RegionComparison), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult CompareRegions([FromQuery] string type = "huisartsenzorg")
    {
        var result = dataService.CompareRegions(type);
        if (result.Regions.Count == 0)
            return NotFound(new { message = $"No data found for care type '{type}'." });

        return Ok(result);
    }

    /// <summary>
    /// Returns the top-N most expensive (or most utilised) postcode areas.
    /// </summary>
    /// <param name="n">Number of results to return (default: 10, max: 50)</param>
    /// <param name="metric">"costs" (avg cost per insured) or "insured" (total insured count)</param>
    [HttpGet("hotspots/topN")]
    [ProducesResponseType(typeof(IEnumerable<Hotspot>), StatusCodes.Status200OK)]
    public IActionResult GetHotspots([FromQuery] int n = 10, [FromQuery] string metric = "costs")
    {
        if (n < 1 || n > 50)
            return BadRequest(new { message = "Parameter 'n' must be between 1 and 50." });

        var validMetrics = new[] { "costs", "insured" };
        if (!validMetrics.Contains(metric, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = "Invalid metric. Valid values: costs, insured" });

        var result = dataService.GetHotspots(n, metric);
        return Ok(result);
    }

    /// <summary>
    /// Returns all available municipalities in the dataset.
    /// </summary>
    [HttpGet("metadata/municipalities")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public IActionResult GetMunicipalities() => Ok(dataService.GetMunicipalities());

    /// <summary>
    /// Returns all available care types in the dataset.
    /// </summary>
    [HttpGet("metadata/caretypes")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public IActionResult GetCareTypes() => Ok(dataService.GetCareTypes());
}
