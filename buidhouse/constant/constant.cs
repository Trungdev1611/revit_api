namespace Simpleform.buidhouse.constant;

public static class Constant {
  public static string pathFamily = "C:\\ProgramData\\Autodesk\\RVT 2023\\Libraries\\English\\South Asia";
       public static Dictionary<string, string> ColumnTypes = new Dictionary<string, string>
    {
        // Lưu ý: Đã bỏ dấu "\" ở trước "Columns" để Path.Combine không nuốt mất familyPath
        { "square", Path.Combine(pathFamily, "Columns", "Square") },
        { "rectangular", Path.Combine(pathFamily, "Columns", "Rectangular") }
    };
}