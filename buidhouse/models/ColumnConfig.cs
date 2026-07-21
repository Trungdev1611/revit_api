using Autodesk.Revit.DB;

namespace Simpleform.buidhouse.models;

public enum ColumnShape
{
    square,
    rectangular,
    circular
}

/// <summary>Runtime config cột — offset và kích thước là Revit internal units (feet).</summary>
public record ColumnConfig(
    string SymbolName,
    ElementId BaseLevelId,
    ElementId TopLevelId,
    double BaseOffset = 0.0,
    double TopOffset = 0.0,
    ColumnShape TypeColumn = ColumnShape.square,
    string FamilyName = "Concrete Square",
    double Width = 0.0,
    double Height = 0.0
);
