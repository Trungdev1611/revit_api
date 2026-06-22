using Autodesk.Revit.DB;

namespace Simpleform.buidhouse.models;

public class GridDataModel
{
    public string Name { get; set; } = "";
    public bool IsHorizontal { get; set; }
    
    public XYZ startPoint { get; set; }
    public XYZ endPoint { get; set; }
}