namespace Simpleform.buidhouse.models;

/// <summary>Runtime config tường — Thickness là Revit internal units (feet).</summary>
public record WallConfig(
    double Thickness,
    string TypeName,
    bool IsStructural = true
);
