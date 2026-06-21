using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Simpleform.drawWallRefactor;

public static class Extension
{
    public static List<WallType> getListTypeWallBuiltIn(this Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Walls)
            .WhereElementIsElementType() // <--- Lấy các Type ẩn trong file
            .Cast<WallType>()
            .ToList();
    }
    
    public static ElementId getFirstLevelInView (this Document doc) {
        return new FilteredElementCollector (doc)
            .OfClass(typeof(Level)).FirstElementId();
    }

    public static ElementId getTextDefault(this Document doc)
    {
        ElementId defaultTextTypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
            
        TextNoteType defaultType = doc.GetElement(defaultTextTypeId) as TextNoteType;

        if (defaultType != null)
        {
            // 3. Lấy tên của nó ra dạng chuỗi (String)
            string defaultTextName = defaultType.Name;

            // 4. Bắn cái bảng thông báo lên màn hình Revit để xem tên
            TaskDialog.Show("Tên Kiểu Chữ Mặc Định", $"Kiểu chữ mặc định hiện tại là: {defaultTextName}");
            return defaultTextTypeId;
        }
        throw new InvalidOperationException("Không tìm thấy kiểu chữ mặc định nào trong dự án!");
    }
    
     public static WallType GetWallTypeDefault(this Document doc)
    {
       return  new FilteredElementCollector(doc)
            .OfClass(typeof(WallType))
            .Cast<WallType>()
            .FirstOrDefault(w => w.Kind == WallKind.Basic);
    }

    public static T? GetFirstItemOrCondition<T>(
        this Document doc,
        Func<T, bool> condition = null, 
        BuiltInCategory? category = null, 
        bool isType = false) where T : Element
    {
        var collector = new FilteredElementCollector(doc);
        // Lọc theo Category nếu có
        if (category.HasValue) collector.OfCategory(category.Value);
        
        // Lọc theo loại (Type) hoặc thực thể (Instance)
        if (isType)
            collector.WhereElementIsElementType();
        else
            collector.WhereElementIsNotElementType();
        
        condition = condition ?? (x => true);
        // Lọc theo Class và kiểm tra điều kiện
        return collector
            .OfClass(typeof(T))
            .Cast<T>()
            .FirstOrDefault(condition);

    }
}