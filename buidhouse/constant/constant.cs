namespace Simpleform.buidhouse.constant;

public static class Constant
{

    //trỏ đến thư mục chưa families sau khi build trong csproj file
    // $(TargetDir)public\families\%(RecursiveDir)
    public static readonly Dictionary<string, string> ColumnFamilyFiles = new()
    {
        { "square", Path.Combine("public","families", "Columns", "Concrete-Square.rfa") }, //public là thư mục chứa các file family - thể hiện ở simpleForm.csproj
        { "rectangular", Path.Combine("public","families", "Columns", "Concrete-Rectangular-Column.rfa") },
    };

    public static readonly Dictionary<string, string> BeamFamilyFiles = new()
    {
        { "square", Path.Combine("public", "families", "Beams", "Concrete-Square-Beam.rfa") },
        { "rectangular", Path.Combine("public", "families", "Beams", "Concrete-Rectangular-Beam.rfa") },
    };



 
}
