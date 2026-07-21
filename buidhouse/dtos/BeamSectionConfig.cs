using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.dtos;

/// <summary>Settings dầm (mm) — gần UI / house.json.</summary>
public class BeamSectionConfig
{
    public double WidthMm { get; set; } = 220;
    public double HeightMm { get; set; } = 400;
    public string Mark { get; set; } = "";
    public string Comment { get; set; } = "";

    /// <summary>Map DTO (mm) → BeamConfig (internal feet).</summary>
    public BeamConfig ToConfig() =>
        new(
            RevitUtil.ConvertToFeet(WidthMm),
            RevitUtil.ConvertToFeet(HeightMm),
            $"{WidthMm:0}x{HeightMm:0}",
            Mark,
            Comment);
}
