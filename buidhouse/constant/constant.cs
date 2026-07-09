namespace Simpleform.buidhouse.constant;

public static class Constant
{
    public static readonly Dictionary<string, string> ColumnFamilyFiles = new()
    {
        { "square", Path.Combine("families", "Columns", "Concrete-Square-Column.rfa") },
        { "rectangular", Path.Combine("families", "Columns", "Concrete-Rectangular-Column.rfa") },
    };
}
