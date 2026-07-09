using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Simpleform;

[Transaction(TransactionMode.Manual)]
public class Class1: IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        // 1. Lấy dữ liệu Document hiện tại của Revit
        UIApplication uiApplication = commandData.Application;
        UIDocument uiDocument = uiApplication.ActiveUIDocument;
        Document doc = uiDocument.Document;
        
        // CÁCH A: Lấy toàn bộ Tường bao gồm cả Instance (đã vẽ) và Type (định nghĩa trong Family)
        FilteredElementCollector filteredElementCollector = new FilteredElementCollector(doc);
        var categoryWall = filteredElementCollector.OfCategory(BuiltInCategory.OST_Walls);

        // 2. Tạo một biến chuỗi (string) để chứa nội dung văn bản sẽ hiển thị
        string infodetail = "";
        
        // 3. Dùng vòng lặp foreach để duyệt qua từng bức tường trong danh sách
        foreach (var  e in categoryWall)
        {
            //ep kiểu
            Wall bucTuong = e as Wall;
            
            // Lấy ra tên của Loại tường (Wall Type) đó
            string wallname = bucTuong.WallType.Name;
            
            // Lấy ra ID của bức tường đó trong Revit
            string idWall = bucTuong.Id.ToString();
            
            // Cộng dồn thông tin vào chuỗi: "Tên loại tường - [ID]"
            infodetail += $"Wallname: {wallname} - Id: ({idWall})";
        }
        // 4. Hiển thị tổng số lượng và chi tiết từng loại lên màn hình
        int tongSoLuong = categoryWall.GetElementCount();
        TaskDialog.Show("Thông báo", $"Tổng số lượng tường: {tongSoLuong}");
        return Result.Succeeded;
    }
}