using System.Reflection;
using Autodesk.Revit.DB;

namespace Simpleform.buidhouse.utils;

public static class FamilyPathUtil
{
    public static string AddinDirectory =>
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

    /// <summary>
    /// Ghép path tuyệt đối tới file .rfa trong thư mục Families/ cạnh DLL add-in.
    /// relativePath ví dụ: "Columns/Concrete-Square-Column.rfa"
    /// </summary>
    public static string Resolve(string relativePath) =>
        Path.Combine(AddinDirectory, "Families", relativePath.Replace('/', Path.DirectorySeparatorChar));

    public static bool Exists(string relativePath) =>
        File.Exists(Resolve(relativePath));

    public static bool TryLoadFamily(Document doc, string relativePath, out Family? family)
    {
        family = null;
        string fullPath = Resolve(relativePath);

        if (!File.Exists(fullPath))
        {
            return false;
        }

        return doc.LoadFamily(fullPath, out family) && family != null;
    }
}
