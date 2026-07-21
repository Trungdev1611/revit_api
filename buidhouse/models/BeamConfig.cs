namespace Simpleform.buidhouse.models;

/// <summary>Runtime config dầm — Width/Height là Revit internal units (feet).</summary>
public record BeamConfig
{
    public double Width { get; set; }
    public double Height { get; set; }

    /// <summary>Tên type Revit, ví dụ "220x400" (giữ mm cho dễ đọc).</summary>
    public string TypeName { get; set; } = "";

    public string Mark { get; set; } = "";
    public string Comment { get; set; } = "";

    public BeamConfig(
        double width,
        double height,
        string typeName,
        string mark = "",
        string comment = "")
    {
        Width = width;
        Height = height;
        TypeName = typeName;
        Mark = mark;
        Comment = comment;
    }
}
