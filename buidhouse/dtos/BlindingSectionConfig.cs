using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.dtos;

/// <summary>Settings bê tông lót (mm) — gần UI / house.json.</summary>
public class BlindingSectionConfig
{
    public string TypeName { get; set; } = "Bê tông lót 100mm";
    public double ThicknessMm { get; set; } = 100;
    public double EdgeExtensionMm { get; set; } = 100;
    public double OffsetFromLevelMm { get; set; } = -150;
    public string LevelName { get; set; } = "Level 1";

    /// <summary>Map DTO (mm) → BlindingConcreteConfig (internal feet).</summary>
    public BlindingConcreteConfig ToConfig(string fallbackLevelName) =>
        new()
        {
            FloortypeName = TypeName,
            Thickness = RevitUtil.ConvertToFeet(ThicknessMm),
            EdgeExtension = RevitUtil.ConvertToFeet(EdgeExtensionMm),
            offsetFromLevelTarget = RevitUtil.ConvertToFeet(OffsetFromLevelMm),
            LevelName = string.IsNullOrWhiteSpace(LevelName)
                ? fallbackLevelName
                : LevelName,
        };
}
