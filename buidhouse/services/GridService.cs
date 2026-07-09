using Autodesk.Revit.DB;
using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.services;

/// <summary>
/// Tính toán biên nhà và tạo Grid / CurveLoop cho sàn.
/// Nguồn tọa độ duy nhất: getFootprintBounds() — grid và sàn dùng chung.
/// </summary>
public class GridService
{
    private readonly Document _doc;
    private readonly XYZ _center;

    // Nửa rộng/dài nhà (5000x6000mm) — hardcode theo đề bài, sau gom vào HouseConfig
    private readonly double halfW = RevitUtil.convertToMeter(5000) / 2;
    private readonly double halfL = RevitUtil.convertToMeter(6000) / 2;

    // Grid nhô ra ngoài ô nhà 2000mm mỗi đầu (chỉ để hiển thị trục trên bản vẽ)
    private readonly double gridLineExtension = RevitUtil.convertToMeter(2000);

    public GridService(XYZ userclickPoint, Document doc)
    {
        _center = userclickPoint;
        _doc = doc;
    }

    /// <summary>Vẽ 4 trục Grid (1, 2, A, B). Không return CurveLoop.</summary>
    public void createGrids()
    {
        var (left, right, bottom, top) = getFootprintBounds();

        createGrid(
            new XYZ(left, bottom - gridLineExtension, _center.Z),
            new XYZ(left, top + gridLineExtension, _center.Z),
            "1");
        createGrid(
            new XYZ(right, bottom - gridLineExtension, _center.Z),
            new XYZ(right, top + gridLineExtension, _center.Z),
            "2");
        createGrid(
            new XYZ(left - gridLineExtension, bottom, _center.Z),
            new XYZ(right + gridLineExtension, bottom, _center.Z),
            "A");
        createGrid(
            new XYZ(left - gridLineExtension, top, _center.Z),
            new XYZ(right + gridLineExtension, top, _center.Z),
            "B");
    }

    /// <summary>
    /// Biên dạng sàn — hình chữ nhật khép kín.
    /// edgeExtensionMm: đua ra ngoài mép nhà (bê tông lót = 100mm).
    /// </summary>
    public CurveLoop createFootprintLoop(double edgeExtensionMm = 0)
    {
        var (left, right, bottom, top) = getFootprintBounds(edgeExtensionMm);
        return RevitUtil.createRectangleLoop(left, right, bottom, top, _center.Z);
    }

    /// <summary>Tính left/right/bottom/top quanh centerPoint ± nửa kích thước ± extension.</summary>
    private (double left, double right, double bottom, double top) getFootprintBounds(double edgeExtensionMm = 0)
    {
        double extension = RevitUtil.convertToMeter(edgeExtensionMm);
        return (
            _center.X - halfW - extension,
            _center.X + halfW + extension,
            _center.Y - halfL - extension,
            _center.Y + halfL + extension
        );
    }

    private void createGrid(XYZ start, XYZ end, string name)
    {
        Line line = Line.CreateBound(start, end);
        Grid grid = Grid.Create(_doc, line);
        grid.Name = name;
    }
}
