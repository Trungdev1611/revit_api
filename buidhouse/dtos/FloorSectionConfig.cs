using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.dtos;

/// <summary>Settings sàn (mm) — gần UI / house.json.</summary>
public class FloorSectionConfig
{
    public string TypeName { get; set; } = "Betong san tang 1";
    public double ThicknessMm { get; set; } = 100;
    public string LevelName { get; set; } = "Level 1";

    /// <summary>Map DTO (mm) → FloorConfig (internal feet).</summary>
    public BuildHouse.Models.FloorConfig ToConfig(string fallbackLevelName)
    {
        string levelName = string.IsNullOrWhiteSpace(LevelName)
            ? fallbackLevelName
            : LevelName;
        return new BuildHouse.Models.FloorConfig(
            TypeName,
            RevitUtil.ConvertToFeet(ThicknessMm),
            levelName);
    }
}
