namespace CareMetrics.API.Models;

/// <summary>
/// Represents a single row from the Vektis Open Data ZVW claims dataset.
/// Schema mirrors the real Vektis CSV structure so a live CSV can be swapped in.
/// </summary>
public class VektisRecord
{
    public int Year { get; set; }

    /// <summary>Care type in Dutch, e.g. "huisartsenzorg", "ziekenhuiszorg", "ggz"</summary>
    public string CareType { get; set; } = string.Empty;

    /// <summary>Municipality name (gemeente)</summary>
    public string Municipality { get; set; } = string.Empty;

    /// <summary>3-digit postcode area, e.g. "101"</summary>
    public string Postcode3 { get; set; } = string.Empty;

    /// <summary>Age group, e.g. "0-17", "18-44", "45-64", "65-74", "75+"</summary>
    public string AgeGroup { get; set; } = string.Empty;

    /// <summary>"M" or "V" (male/female)</summary>
    public string Gender { get; set; } = string.Empty;

    public int InsuredCount { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AvgCostPerInsured { get; set; }
}
