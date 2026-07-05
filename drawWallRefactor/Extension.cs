
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Simpleform.drawWallRefactor;

public static class Extension
{
    // 1. Lấy danh sách các kiểu tường có sẵn trong file
    public static List<WallType> getListTypeWallBuiltIn(this Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Walls)
            .WhereElementIsElementType() 
            .Cast<WallType>()
            .ToList();
    }
    
    // 2. Lấy ID của Level đầu tiên tìm thấy (Đã sửa để chống crash)
    public static ElementId? getFirstLevelInView(this Document doc) 
    {
        Element level = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .FirstOrDefault();
            
        return level?.Id; // Trả về null nếu dự án lỗi không có level nào
    }

    // 3. Lấy kiểu chữ mặc định của dự án
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
    
    // 4. Lấy kiểu tường cơ bản (Basic Wall) đầu tiên mặc định
    public static WallType? GetWallTypeDefault(this Document doc)
    {
       return new FilteredElementCollector(doc)
            .OfClass(typeof(WallType))
            .Cast<WallType>()
            .FirstOrDefault(w => w.Kind == WallKind.Basic);
    }

    // 5. Hàm lọc Generic "Thần thánh" (Đã đảo OfClass lên đầu để tăng tốc độ lọc)
    public static T? GetFirstItemOrCondition<T>(
        this Document doc,
        Func<T, bool>? condition = null,
        BuiltInCategory? category = null,
        bool isType = false) where T : Element
    {
        // Khởi tạo collector và lọc Class ngay lập tức ở tầng gốc C++ để đạt tốc độ cao nhất
        var collector = new FilteredElementCollector(doc).OfClass(typeof(T));
        
        // Lọc theo Category nếu được truyền vào
        if (category.HasValue) 
        {
            collector.OfCategory(category.Value);
        }
        
        // Phân biệt Type (FamilySymbol, WallType...) hay Instance (Cột, Tường cụ thể)
        if (isType)
        {
            collector.WhereElementIsElementType();
        }
        else
        {
            collector.WhereElementIsNotElementType();
        }
        
        // Xử lý điều kiện Lambda delegate của bạn một cách mượt mà
        condition ??= (x => true);
        
        return collector
            .Cast<T>()
            .FirstOrDefault(condition);
    }
}
