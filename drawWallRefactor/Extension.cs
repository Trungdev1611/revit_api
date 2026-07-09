
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Simpleform.drawWallRefactor;

public static class Extension
{
    public static List<WallType> getListTypeWallBuiltIn(this Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Walls)
            .WhereElementIsElementType() 
            .Cast<WallType>()
            .ToList();
    }
    
    public static ElementId? getFirstLevelInView(this Document doc) 
    {
        Element level = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .FirstOrDefault();
            
        return level?.Id;
    }

    public static ElementId getTextDefault(this Document doc)
    {
        ElementId defaultTextTypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
        TextNoteType? defaultType = doc.GetElement(defaultTextTypeId) as TextNoteType;

        if (defaultType != null)
        {
            string defaultTextName = defaultType.Name;
            TaskDialog.Show("Tên Kiểu Chữ Mặc Định", $"Kiểu chữ mặc định hiện tại là: {defaultTextName}");
            return defaultTextTypeId;
        }
        throw new InvalidOperationException("Không tìm thấy kiểu chữ mặc định nào trong dự án!");
    }
    
    public static WallType? GetWallTypeDefault(this Document doc)
    {
       return new FilteredElementCollector(doc)
            .OfClass(typeof(WallType))
            .Cast<WallType>()
            .FirstOrDefault(w => w.Kind == WallKind.Basic);
    }

    public static T? GetFirstItemOrCondition<T>(
        this Document doc,
        Func<T, bool>? condition = null,
        BuiltInCategory? category = null,
        bool isType = false) where T : Element
    {
        var collector = new FilteredElementCollector(doc).OfClass(typeof(T));
        
        if (category.HasValue) 
        {
            collector.OfCategory(category.Value);
        }
        
        if (isType)
        {
            collector.WhereElementIsElementType();
        }
        else
        {
            collector.WhereElementIsNotElementType();
        }
        
        condition ??= (x => true);
        
        return collector
            .Cast<T>()
            .FirstOrDefault(condition);
    }
}
