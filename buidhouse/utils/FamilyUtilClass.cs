using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Simpleform.buidhouse.constant;
using Simpleform.buidhouse.utils;
using Simpleform.drawWallRefactor;

namespace BuildHouse.Utils;

public static class FamilyUtilClass
{
    public static bool TryLoadFamily(Document doc, string absolutePath, out Family? familyloaded)
    {
        return doc.LoadFamily(absolutePath, out familyloaded) && familyloaded != null;
    }

    public static bool CheckFilePathExist(string fullPath)
    {
        return File.Exists(fullPath);
    }

    /// <summary>
    /// keyInConfig ví dụ: "column-square", "beam-rectangular"
    /// </summary>
    public static Family? GetFamilyIfExistedOrloadNew(Document doc, string familyName, string keyInConfig)
    {
        Family? familyloaded = doc.GetFirstItemOrCondition<Family>(item => item.Name == familyName);
        if (familyloaded != null)
        {
            return familyloaded;
        }

        if (!Constant.FamilyFiles.TryGetValue(keyInConfig, out string? relativePath) || string.IsNullOrEmpty(relativePath))
        {
            TaskDialog.Show("Error", $"Không tìm thấy key family trong config: {keyInConfig}");
            return null;
        }

        string fullPathToLoadFamily = RevitUtil.resolveFamilyPath(relativePath);

        if (!CheckFilePathExist(fullPathToLoadFamily))
        {
            TaskDialog.Show("Error",
                $"Không tìm thấy file family:\n{fullPathToLoadFamily}\n\n" +
                "Hãy copy file .rfa vào public\\families\\ rồi build lại.");
            return null;
        }

        if (!TryLoadFamily(doc, fullPathToLoadFamily, out familyloaded))
        {
            TaskDialog.Show("Error", $"Không load được family:\n{fullPathToLoadFamily}");
            return null;
        }

        return familyloaded;
    }
}
