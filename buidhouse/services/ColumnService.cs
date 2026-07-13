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
            column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).Set(config.BaseOffset / 304.8);
            column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).Set(config.TopOffset / 304.8);
        }

        return column;
    }

    public FamilySymbol GetOrCreateFamilySymbol(Document doc, ColumnConfig columnConfig)
    {
        string symbolName = columnConfig.SymbolName;
        string shape = columnConfig.TypeColumn.ToString().ToLower(); // square | rectangular
        string familyKey = $"column-{shape}";

        Func<Family, bool> isExistFamilyName = f => f.Name == columnConfig.FamilyName;
        Func<FamilySymbol, bool> isExistSymbolName = f => f.Name == symbolName;

        FamilySymbol familySymbol = doc.GetFirstItemOrCondition<FamilySymbol>(
            f => isExistFamilyName(f.Family) && isExistSymbolName(f),
            isType: true
        );

        if (familySymbol != null)
        {
            return familySymbol;
        }

        Family? familyloaded = FamilyUtilClass.GetFamilyIfExistedOrloadNew(doc, columnConfig.FamilyName, familyKey);
        if (familyloaded == null)
        {
            return null;
        }

        var symbolIds = familyloaded.GetFamilySymbolIds();
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
