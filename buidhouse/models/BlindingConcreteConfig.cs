using Simpleform.buidhouse.Interface;

namespace Simpleform.buidhouse.models;

/// <summary>Runtime config bê tông lót — length fields là Revit internal units (feet).</summary>
public class BlindingConcreteConfig : IFloor
{
    public string FloortypeName { get; set; } = "Bê tông lót 100mm";

    /// <summary>Độ dày (feet).</summary>
    public double Thickness { get; set; }

    /// <summary>Mở rộng mép footprint (feet).</summary>
    public double EdgeExtension { get; set; }

    public string LevelName { get; set; } = "";

    /// <summary>Offset so với level (feet).</summary>
    public double offsetFromLevelTarget { get; set; }
}
