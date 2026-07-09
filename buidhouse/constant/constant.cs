namespace Simpleform.buidhouse.constant;

/// <summary>
/// Đường dẫn file .rfa tương đối so với thư mục chứa Simpleform.dll.
/// Source: buidhouse/families/Columns/ → output: buildhouse_Files/families/Columns/
/// </summary>
public static class Constant
{
    public static readonly Dictionary<string, string> ColumnFamilyFiles = new()
    {
        { "square", Path.Combine("families", "Columns", "Concrete-Square-Column.rfa") },
        { "rectangular", Path.Combine("families", "Columns", "Concrete-Rectangular-Column.rfa") },
    };
}
