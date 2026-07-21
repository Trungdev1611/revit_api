using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.dtos;

/// <summary>Settings tường (mm) — gần UI / house.json.</summary>
public class WallSectionConfig
{
    public double ThicknessMm { get; set; } = 220;
    public bool IsStructural { get; set; } = true;

    /// <summary>Map DTO (mm) → WallConfig (internal feet).</summary>
    public WallConfig ToConfig() =>
        new(
            Thickness: RevitUtil.ConvertToFeet(ThicknessMm),
            TypeName: $"Basic Wall {ThicknessMm:0}mm",
            IsStructural: IsStructural);
}
