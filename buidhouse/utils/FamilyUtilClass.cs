using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Simpleform.buidhouse.constant;
using Simpleform.buidhouse.utils;

namespace BuildHouse.Utils;

public static class FamilyUtilClass
{
    private class FamilyLoadOptions : IFamilyLoadOptions
    {
        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
        {
            overwriteParameterValues = true;
            return true;
        }

        public bool OnSharedFamilyFound(
            Family sharedFamily,
            bool familyInUse,
            out FamilySource source,
            out bool overwriteParameterValues)
        {
            source = FamilySource.Family;
            overwriteParameterValues = true;
            return true;
        }
    }

    public static bool TryLoadFamily(Document doc, string absolutePath, out Family? familyloaded)
    {
        bool loaded = doc.LoadFamily(absolutePath, new FamilyLoadOptions(), out familyloaded);
        return loaded && familyloaded != null;
    }

    public static bool CheckFilePathExist(string fullPath) => File.Exists(fullPath);

    /// <summary>
    /// Family không phải ElementType — không dùng WhereElementIsElementType khi collect.
    /// </summary>
    public static Family? FindFamilyByName(Document doc, string familyName)
    {
        if (string.IsNullOrWhiteSpace(familyName))
        {
            return null;
        }

        string normalizedExpect = NormalizeName(familyName);
        return new FilteredElementCollector(doc)
            .OfClass(typeof(Family))
            .Cast<Family>()
            .FirstOrDefault(f =>
                f.Name == familyName
                || NormalizeName(f.Name) == normalizedExpect);
    }

    private static string NormalizeName(string name) =>
        name.Replace(" ", string.Empty).Replace("-", string.Empty);

    /// <summary>
    /// keyInConfig ví dụ: "column-square", "beam-rectangular"
    /// familyName = tên Family trong Revit (gần đúng cũng được nhờ normalize)
    /// </summary>
    public static Family? GetFamilyIfExistedOrloadNew(Document doc, string familyName, string keyInConfig)
    {
        Family? familyloaded = FindFamilyByName(doc, familyName);
        if (familyloaded != null)
        {
            AppLog.Information("Family already in project: {0}", familyloaded.Name);
            return familyloaded;
        }

        if (!Constant.FamilyFiles.TryGetValue(keyInConfig, out string? relativePath) || string.IsNullOrEmpty(relativePath))
        {
            TaskDialog.Show("Error", $"Không tìm thấy key family trong config: {keyInConfig}");
            throw new InvalidOperationException($"Missing family key: {keyInConfig}");
        }

        string fullPathToLoadFamily = RevitUtil.resolveFamilyPath(relativePath);
        AppLog.Information("Load family path: {0} (expect name '{1}')", fullPathToLoadFamily, familyName);

        if (!CheckFilePathExist(fullPathToLoadFamily))
        {
            TaskDialog.Show("Error",
                $"Không tìm thấy file family:\n{fullPathToLoadFamily}\n\n" +
                "Hãy copy file .rfa vào public\\families\\ rồi build lại.");
            throw new InvalidOperationException($"Family file not found: {fullPathToLoadFamily}");
        }

        if (!TryLoadFamily(doc, fullPathToLoadFamily, out familyloaded))
        {
            string fileStem = Path.GetFileNameWithoutExtension(fullPathToLoadFamily);
            familyloaded = FindFamilyByName(doc, familyName)
                           ?? FindFamilyByName(doc, fileStem);
        }

        if (familyloaded == null)
        {
            string existing = string.Join(", ",
                new FilteredElementCollector(doc)
                    .OfClass(typeof(Family))
                    .Cast<Family>()
                    .Select(f => f.Name)
                    .OrderBy(n => n)
                    .Take(30));
            AppLog.Error(
                "LoadFamily failed. path={0} expect='{1}' familiesInDoc=[{2}]",
                fullPathToLoadFamily, familyName, existing);
            TaskDialog.Show("Error",
                $"Không load được family:\n{fullPathToLoadFamily}\n\n" +
                $"Tên expect: {familyName}\n" +
                $"Families trong project: {existing}");
            throw new InvalidOperationException($"Cannot load family: {fullPathToLoadFamily}");
        }

        AppLog.Information("Family loaded OK: '{0}' (expect was '{1}')", familyloaded.Name, familyName);
        return familyloaded;
    }
}
