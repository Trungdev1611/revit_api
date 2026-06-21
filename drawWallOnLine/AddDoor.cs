using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace Simpleform.drawWallRefactor;

public static class DoorFunction
{
    // Biến hàm này thành static, truyền tất cả những gì thay đổi vào tham số
    public static FamilyInstance AddDoor(
        Document doc, 
        XYZ location, 
        Element host, 
        Level level, 
        FamilySymbol doorType)
    {
        // 1. Kiểm tra an toàn trước khi vẽ
        if (doorType == null || host == null || level == null) return null;

        // 2. Kích hoạt Type nếu chưa kích hoạt
        if (!doorType.IsActive)
        {
            doorType.Activate();
        }

        // 3. Gọi lệnh tạo của Revit và trả về đối tượng cửa vừa tạo
        return doc.Create.NewFamilyInstance(
            location,
            doorType,
            host,
            level,
            Autodesk.Revit.DB.Structure.StructuralType.NonStructural
        );
    }
}