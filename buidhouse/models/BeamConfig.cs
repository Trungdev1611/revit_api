using Autodesk.Revit.DB;

namespace Simpleform.buidhouse.models;

public record BeamConfig
{
    public double width { get; set; }
    public double height { get; set; }

    public XYZ StartPoint { get; set; }
    public XYZ EndPoint { get; set; }

    public string levelName { get; set; } = "";

    public string mark { get; set; } = "";
    public string comment { get; set; } = "";
}
