using Autodesk.Revit.DB;
using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.services;

public class GridService
{
    private readonly Document _doc;
    private readonly XYZ _center; // Đổi tên cho rõ nghĩa: Tâm ô đất
    
    // Quy đổi đơn vị ngay từ đầu
    private readonly double halfW = RevitUtil.convertToMeter(5000) / 2;
    private readonly double halfL = RevitUtil.convertToMeter(6000) / 2;
    private readonly double ext = RevitUtil.convertToMeter(2000);

    public GridService(XYZ userclickPoint, Document doc)
    {
        _center = userclickPoint;
        _doc = doc;
    }

    public CurveLoop createGridLines()
    {
        // Xác định các mốc tọa độ của 4 cạnh (Trái, Phải, Dưới, Trên)
        double left   = _center.X - halfW;
        double right  = _center.X + halfW;
        double bottom = _center.Y - halfL;
        double top    = _center.Y + halfL;

        // Vẽ 2 Trục Dọc (Chạy từ Dưới lên Trên, có cộng thêm đoạn nhô ra 'ext')
        Line line1 = createGrid(new XYZ(left, bottom - ext, _center.Z),  new XYZ(left, top + ext, _center.Z),  "1");
        Line line2 = createGrid(new XYZ(right, bottom - ext, _center.Z), new XYZ(right, top + ext, _center.Z), "2");

        // Vẽ 2 Trục Ngang (Chạy từ Trái sang Phải, có cộng thêm đoạn nhô ra 'ext')
        Line line3 = createGrid(new XYZ(left - ext, bottom, _center.Z),  new XYZ(right + ext, bottom, _center.Z),  "A");
        Line line4 = createGrid(new XYZ(left - ext, top, _center.Z),     new XYZ(right + ext, top, _center.Z),     "B");
        CurveLoop profile = new CurveLoop();
        profile.Append(line1);
        profile.Append(line2);
        profile.Append(line3);
        profile.Append(line4);
        return profile;
    }

    // Hàm phụ trợ nhỏ gọn để vừa tạo vừa đặt tên luôn, đỡ lặp code
    private Line createGrid(XYZ start, XYZ end, string name)
    {
        Line line = Line.CreateBound(start, end);
        Grid grid = Grid.Create(_doc, line);
        grid.Name = name;
        return line;
    }
}