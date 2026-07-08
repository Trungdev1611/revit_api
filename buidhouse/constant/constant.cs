namespace Simpleform.buidhouse.constant;

public static class Constant
{
    /// <summary>
    /// Đường dẫn tương đối (so với thư mục chứa Simpleform.dll) tới file .rfa.
    /// Copy file .rfa từ thư viện Revit vào buidhouse\families\Columns\ trước khi build.
    /// </summary>
    public static readonly Dictionary<string, string> ColumnFamilyFiles = new()
    {
        { "square", Path.Combine("families", "Columns", "Concrete-Square-Column.rfa") },
        { "rectangular", Path.Combine("families", "Columns", "Concrete-Rectangular-Column.rfa") },
    };
}
