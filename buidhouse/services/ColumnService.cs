using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BuildHouse.Utils;
using Simpleform.buidhouse.models;
using Simpleform.drawWallRefactor;

namespace Simpleform.buidhouse.services;

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
            // BaseOffset/TopOffset đã là internal feet (convert ở ColumnSectionConfig.ToConfig)
            column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM)
                .Set(config.BaseOffset);
            column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM)
                .Set(config.TopOffset);
        }

        return column;
    }

    public FamilySymbol GetOrCreateFamilySymbol(Document doc, ColumnConfig columnConfig)
    {
        string symbolName = columnConfig.SymbolName;
        string shape = columnConfig.TypeColumn.ToString().ToLower();
        string familyKey = $"column-{shape}";

        Family? family = FamilyUtilClass.GetFamilyIfExistedOrloadNew(
            doc,
            columnConfig.FamilyName,
            familyKey);
        if (family == null)
        {
            return null;
        }

        // Tìm type theo Family thật (sau load), không hard-code tên cũ
        FamilySymbol? familySymbol = doc.GetFirstItemOrCondition<FamilySymbol>(
            f => f.Family.Id == family.Id && f.Name == symbolName,
            isType: true);

        if (familySymbol == null)
        {
            ElementId firstSymbolId = family.GetFamilySymbolIds().FirstOrDefault();
            if (firstSymbolId == null || firstSymbolId == ElementId.InvalidElementId)
            {
                TaskDialog.Show("Error", "Family này không chứa Type nào để nhân bản.");
                return null;
            }

            FamilySymbol? defaultSymbol = doc.GetElement(firstSymbolId) as FamilySymbol;
            if (defaultSymbol == null)
            {
                return null;
            }

            if (!defaultSymbol.IsActive)
            {
                defaultSymbol.Activate();
                doc.Regenerate();
            }

            // Type có thể đã tồn tại (cột 2..4) — lấy lại thay vì Duplicate lần nữa
            familySymbol = doc.GetFirstItemOrCondition<FamilySymbol>(
                f => f.Family.Id == family.Id && f.Name == symbolName,
                isType: true);

            if (familySymbol == null)
            {
                familySymbol = defaultSymbol.Duplicate(symbolName) as FamilySymbol;
            }

            if (familySymbol == null)
            {
                TaskDialog.Show("Error", $"Cannot create column type: {symbolName}");
                return null;
            }

            // Width/Height đã là internal feet (convert ở ColumnSectionConfig.ToConfig)
            if (columnConfig.Width > 0 && columnConfig.Height > 0)
            {
                SetParameterIfExists(familySymbol, "b", columnConfig.Width);
                SetParameterIfExists(familySymbol, "h", columnConfig.Height);
            }
        }

        if (!familySymbol.IsActive)
        {
            familySymbol.Activate();
            doc.Regenerate();
        }

        return familySymbol;
    }

    private static void SetParameterIfExists(Element element, string parameterName, double valueInternal)
    {
        Parameter? parameter = element.LookupParameter(parameterName);
        if (parameter == null || parameter.IsReadOnly)
        {
            return;
        }

        parameter.Set(valueInternal);
    }
}
