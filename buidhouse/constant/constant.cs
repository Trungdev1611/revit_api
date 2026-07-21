namespace Simpleform.buidhouse.constant;

public static class Constant
{
    // Key: "{category}-{shape}" — path tương đối so với thư mục DLL sau build
    // csproj: $(TargetDir)public\families\...
    public static readonly Dictionary<string, string> FamilyFiles = new()
    {
        { "column-square", Path.Combine("public", "families", "Columns", "Concrete Square.rfa") },
        { "column-rectangular", Path.Combine("public", "families", "Columns", "Concrete-Rectangular-Column.rfa") },
        { "beam-square", Path.Combine("public", "families", "Beams", "Concrete-Square-Beam.rfa") },
        { "beam-rectangular", Path.Combine("public", "families", "Beams", "Concrete - Rectangular Beam.rfa") },
    };
}
