using System.Text.Json.Serialization;
using Autodesk.Revit.DB;
using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.dtos;

/// <summary>Settings cột (mm). SymbolName sinh từ Width×Height.</summary>
public class ColumnSectionConfig
{
    public double WidthMm { get; set; } = 220;

    /// <summary>Null hoặc = Width → cột vuông.</summary>
    public double? HeightMm { get; set; }

    public string FamilyName { get; set; } = "Concrete Square";
    public ColumnShape Shape { get; set; } = ColumnShape.square;
    public int Count { get; set; } = 4;
    public double BaseOffsetMm { get; set; }
    public double TopOffsetMm { get; set; }

    [JsonIgnore]
    public string SymbolName
    {
        get
        {
            double h = HeightMm ?? WidthMm;
            return $"{WidthMm:0}x{h:0}";
        }
    }

    [JsonIgnore]
    public double HalfWidthMm => WidthMm / 2.0;

    /// <summary>Map DTO (mm) → ColumnConfig (internal feet).</summary>
    public ColumnConfig ToConfig(ElementId baseLevelId, ElementId topLevelId)
    {
        double heightMm = HeightMm ?? WidthMm;
        return new ColumnConfig(
            SymbolName,
            baseLevelId,
            topLevelId,
            RevitUtil.ConvertToFeet(BaseOffsetMm),
            RevitUtil.ConvertToFeet(TopOffsetMm),
            Shape,
            FamilyName,
            RevitUtil.ConvertToFeet(WidthMm),
            RevitUtil.ConvertToFeet(heightMm));
    }
}
