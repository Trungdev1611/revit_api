using Autodesk.Revit.DB;

namespace Simpleform.buidhouse.models;

public record BeamConfig
{
    public double Width { get; set; }
    public double Height { get; set; }

    // public XYZ StartPoint { get; set; }
    // public XYZ EndPoint { get; set; }

    // public string levelName { get; set; } = "";

    public string Mark { get; set; } = "";
    public string Comment { get; set; } = "";

    public BeamConfig(double width, double height, string mark = "", string comment = "")
    {

        Width = width;
        Height = height;
        Mark = mark;
        Comment = comment;
    }
}
