using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
namespace Simpleform.buidhouse.models;

public enum ColumnShape {
    square,
    rectangular,
    circular
}

public record ColumnConfig(
    string SymbolName,
    ElementId BaseLevelId,
    ElementId TopLevelId,
    double BaseOffset = 0.0,
    double TopOffset = 0.0,
    ColumnShape TypeColumn = ColumnShape.square,
    // Tên Family trong Project Browser (Autodesk), không phải tên file
    string FamilyName = "Concrete Square"
);
