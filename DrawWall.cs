using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Simpleform;

[Transaction(TransactionMode.Manual)]
public class DrawWall : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        // 1. Lấy thông tin môi trường làm việc hiện tại (Document)
        UIDocument uiDoc = commandData.Application.ActiveUIDocument;
        Document doc = uiDoc.Document;

        // 2. Thu thập tất cả các loại tường (WallType) có trong dự án
        IList<Element> wallTypes = new FilteredElementCollector(doc)
            .OfClass(typeof(WallType))
            .ToElements();

        if (wallTypes.Count == 0)
        {
            TaskDialog.Show("Thông báo", "Không tìm thấy loại tường (Wall Type) nào trong file này!");
            return Result.Failed;
        }

        // 3. Yêu cầu người dùng click chọn 1 điểm mốc trên màn hình để bắt đầu xếp danh sách
        TaskDialog.Show("Hướng dẫn", "Vui lòng click chọn 1 điểm trên mặt bằng để làm gốc bắt đầu vẽ!");
        XYZ pointStart;
        try
        {
            pointStart = uiDoc.Selection.PickPoint();
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            // Xử lý trường hợp người dùng bấm ESC hoặc hủy lệnh không muốn chọn điểm nữa
            return Result.Cancelled;
        }

        // 4. Thiết lập các thông số khoảng cách hình học (Bắt buộc phải đổi mm sang Feet)
        // Mỗi đoạn tường vẽ ra sẽ dài 3000 mm (3 mét)
        double wallLengthInFeet = UnitUtils.ConvertToInternalUnits(3000, UnitTypeId.Millimeters);
        // Khoảng cách dãn cách giữa các bức tường xếp theo hàng dọc là 1500 mm (1.5 mét)
        double gapInFeet = UnitUtils.ConvertToInternalUnits(1500, UnitTypeId.Millimeters);

        // Lấy ID tầng (Level) đang mở hiện tại và ID của View hiện tại
        ElementId activeLevelId = doc.ActiveView.GenLevel.Id;
        ElementId activeViewId = doc.ActiveView.Id;

        // Khởi tạo tọa độ chạy của trục Y, ban đầu bằng đúng tọa độ Y của điểm người dùng click
        double currentY = pointStart.Y;

        // 5. Mở một Transaction (Giao dịch) để xin phép Revit ghi dữ liệu/dựng hình vào file
        using (Transaction trans = new Transaction(doc, "Tự động vẽ danh sách các loại tường"))
        {
            trans.Start();

            foreach (Element elem in wallTypes)
            {
                // Ép kiểu dữ liệu từ Element chung về dạng WallType cụ thể
                WallType wType = elem as WallType;
                if (wType == null) continue;

                // Tính toán tọa độ điểm Đầu (Start) và điểm Cuối (End) của bức tường hiện tại
                // Trục X đầu sẽ giữ nguyên, trục X cuối dịch sang phải bằng chiều dài bức tường
                // Trục Y liên tục trừ bớt đi (currentY) để hàng tường xếp dịch xuống phía dưới màn hình
                XYZ currentStartPoint = new XYZ(pointStart.X, currentY, pointStart.Z);
                XYZ currentEndPoint = new XYZ(pointStart.X + wallLengthInFeet, currentY, pointStart.Z);

                // Tạo đường thẳng cơ sở (Line) nối từ điểm đầu đến điểm cuối
                Line wallLine = Line.CreateBound(currentStartPoint, currentEndPoint);

                // Ra lệnh cho Revit dựng bức tường mới dựa trên đường thẳng vừa tạo
                // Biến 'false' ở cuối xác định đây là tường kiến trúc thông thường
                Wall newWall = Wall.Create(doc, wallLine, activeLevelId, false);
                
                // Đổi chủng loại (WallType) của bức tường vừa dựng về đúng loại đang chạy trong vòng lặp
                newWall.WallType = wType;

                // --- ĐOẠN VÈ TEXT NOTE GHI CHÚ TÊN LOẠI TƯỜNG ---
                // Tra Parameter hệ thống để lấy chuỗi ký tự tên của WallType đó
                Parameter nameParam = wType.get_Parameter(BuiltInParameter.SYMBOL_NAME_PARAM);
                string wallTypeName = (nameParam != null) ? nameParam.AsString() : "Không rõ tên";

                // Vị trí đặt chữ cách điểm cuối bức tường sang bên phải một khoảng bằng 'gapInFeet'
                XYZ textPoint = new XYZ(currentEndPoint.X + gapInFeet, currentY, pointStart.Z);

                // Lấy ID kiểu chữ mặc định của hệ thống để vẽ chữ
                ElementId defaultTextTypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
                
                // Tạo Text Note hiển thị tên loại tường lên bản vẽ
                TextNote.Create(doc, activeViewId, textPoint, wallTypeName, defaultTextTypeId);

                // --- CẬP NHẬT TỌA ĐỘ TRỤC Y CHO BƯỚC TIẾP THEO ---
                // Trừ bớt tọa độ Y đi một khoảng cách cố định để bức tường tiếp theo không đè lên bức tường trước
                currentY -= gapInFeet;
            }

            // Đóng giao dịch, hoàn thành việc vẽ và lưu lại vào Revit
            trans.Commit();
        }

        // Hiện bảng thông báo hoàn thành lệnh Add-in
        TaskDialog.Show("Thành công", $"Đã tự động vẽ thành công {wallTypes.Count} loại tường vào bản vẽ!");
        return Result.Succeeded;
    }
}