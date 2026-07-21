using Autodesk.Revit.DB;
using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.services;

public class GridService
{
    private readonly Document _doc;
    private readonly XYZ _center;

    private GridConfig _gridConfig;

    // private readonly double halfW = RevitUtil.ConvertToFeet(5000) / 2;
    // private readonly double halfL = RevitUtil.ConvertToFeet(6000) / 2;
    // private readonly double gridLineExtension = RevitUtil.ConvertToFeet(2000);

    public GridService(XYZ userclickPoint, Document doc, GridConfig gridConfig)
    {
        _center = userclickPoint;
        _doc = doc;
        this._gridConfig = gridConfig;
    }

    public void CreateGrids()
    {
        var (left, right, bottom, top) = getFootprintBounds();
        var gridLineExtensionInFeet = _gridConfig.gridLineExtension;
        CreateGrid(
            new XYZ(left, bottom - gridLineExtensionInFeet, _center.Z),
            new XYZ(left, top + gridLineExtensionInFeet, _center.Z),
            "1");
        CreateGrid(
            new XYZ(right, bottom - gridLineExtensionInFeet, _center.Z),
            new XYZ(right, top + gridLineExtensionInFeet, _center.Z),
            "2");
        CreateGrid(
            new XYZ(left - gridLineExtensionInFeet, bottom, _center.Z),
            new XYZ(right + gridLineExtensionInFeet, bottom, _center.Z),
            "A");
        CreateGrid(
            new XYZ(left - gridLineExtensionInFeet, top, _center.Z),
            new XYZ(right + gridLineExtensionInFeet, top, _center.Z),
            "B");
    }

    /// <param name="edgeExtension">Mở rộng mép footprint — Revit internal units (feet).</param>
    public CurveLoop createFootprintLoop(double edgeExtension = 0)
    {
        var (left, right, bottom, top) = getFootprintBounds(edgeExtension);
        return RevitUtil.CreateRectangleLoop(left, right, bottom, top, _center.Z);
    }

    /// <param name="edgeExtension">Mở rộng mép footprint — Revit internal units (feet).</param>
    public (double left, double right, double bottom, double top) getFootprintBounds(double edgeExtension = 0)
    {
        return (
            _center.X - _gridConfig.WidthHouse/2 - edgeExtension,
            _center.X + _gridConfig.WidthHouse/2 + edgeExtension,
            _center.Y - _gridConfig.LengthHouse/2 - edgeExtension,
            _center.Y + _gridConfig.LengthHouse/2 + edgeExtension
        );
    }

    private void CreateGrid(XYZ start, XYZ end, string name)
    {
        Line line = Line.CreateBound(start, end);
        Grid grid = Grid.Create(_doc, line);
        grid.Name = name;
    }
}
