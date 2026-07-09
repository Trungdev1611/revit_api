using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Simpleform.buidhouse.constant;
using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;
using Simpleform.drawWallRefactor;

namespace Simpleform.buidhouse.services;

/// <summary>
/// Tạo cột kết cấu (Loadable Family).
/// Luồng: tìm FamilySymbol → LoadFamily(.rfa) nếu cần → Duplicate type → NewFamilyInstance.
/// </summary>
public class ColumnService
{
    public FamilyInstance CreateColumn(Document doc, ColumnConfig config, XYZ location)
    {
        FamilySymbol symbol = GetOrCreateFamilySymbol(doc, config);
        if (symbol == null)
        {
            return null;
        }

        FamilyInstance column = doc.Create.NewFamilyInstance(
            location,
            symbol,
            doc.GetElement(config.BaseLevelId) as Level,
            Autodesk.Revit.DB.Structure.StructuralType.Column
        );

        if (column != null)
        {
            column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).Set(config.TopLevelId);
            column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).Set(config.BaseOffset / 304.8);
            column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).Set(config.TopOffset / 304.8);
        }

        return column;
    }

    public FamilySymbol GetOrCreateFamilySymbol(Document doc, ColumnConfig columnConfig)
    {
        string symbolName = columnConfig.SymbolName;
        string colCategory = columnConfig.TypeColumn.ToString().ToLower();

        Func<Family, bool> isExistFamilyName = f => f.Name == columnConfig.FamilyName;
        Func<FamilySymbol, bool> isExistSymbolName = f => f.Name == symbolName;

        // FamilySymbol là Type → isType: true
        FamilySymbol familySymbol = doc.GetFirstItemOrCondition<FamilySymbol>(
            f => isExistFamilyName(f.Family) && isExistSymbolName(f),
            isType: true
        );

        if (familySymbol != null)
        {
            return familySymbol;
        }

        Family structuralFamily = doc.GetFirstItemOrCondition<Family>(isExistFamilyName);

        // Load .rfa từ thư mục cạnh DLL (buildhouse_Files/families/Columns/)
        if (structuralFamily == null && Constant.ColumnFamilyFiles.TryGetValue(colCategory, out string relativePath))
        {
            string familyPath = RevitUtil.resolveFamilyPath(relativePath);
            if (File.Exists(familyPath))
            {
                bool isLoadSuccess = doc.LoadFamily(familyPath, out structuralFamily);
                if (!isLoadSuccess || structuralFamily == null)
                {
                    TaskDialog.Show("Error", $"Không load được family:\n{familyPath}");
                    return null;
                }
            }
            else
            {
                TaskDialog.Show("Error",
                    $"Không tìm thấy file family:\n{familyPath}\n\n" +
                    "Copy file .rfa vào buidhouse\\families\\Columns\\ rồi build lại.");
                return null;
            }
        }

        if (structuralFamily == null)
        {
            TaskDialog.Show("Error", $"Không tìm thấy family '{columnConfig.FamilyName}' trong project.");
            return null;
        }

        var symbolIds = structuralFamily.GetFamilySymbolIds();
        if (symbolIds == null || symbolIds.Count == 0)
        {
            TaskDialog.Show("Error", "Family này không chứa Type nào để nhân bản.");
            return null;
        }

        FamilySymbol defaultSymbol = doc.GetElement(symbolIds.First()) as FamilySymbol;
        FamilySymbol newSymbol = defaultSymbol.Duplicate(symbolName) as FamilySymbol;

        if (newSymbol != null && !newSymbol.IsActive)
        {
            newSymbol.Activate();
        }

        return newSymbol;
    }
}
