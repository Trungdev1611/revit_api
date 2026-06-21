using Autodesk.Revit.DB;
using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.services;


//logic dựng grid
public  class GridService
{
    private readonly List<GridDataModel> _dataGrid;
    private readonly Document _doc;
    private readonly XYZ _userclickPoint;
    GridService(XYZ userclickPoint, Document doc)
    {
        this._dataGrid  = new List<GridDataModel>() {
            new GridDataModel({Name = "A", DistanceFromCenterPoint = 3000, IsHorizontal = true},
            new GridDataModel({Name = "B", DistanceFromCenterPoint = -3000, IsHorizontal = true},
            new GridDataModel({Name = "1", DistanceFromCenterPoint = 2500, IsHorizontal = false},
            new GridDataModel({Name = "2", DistanceFromCenterPoint = -2500, IsHorizontal = false}
        };
        this._userclickPoint = userclickPoint;
        this._doc = doc;
    }

    public void createGridLines()
    {
        foreach (var itemGrid  in _dataGrid)
        {
            double feetDistance = RevitUtil.convertToMeter(itemGrid.DistanceFromCenterPoint);
            XYZ startPoint = itemGrid.IsHorizontal == true
                ? new XYZ(
                    _userclickPoint.X + feetDistance,
                    _userclickPoint.Y, _userclickPoint.Z);
        }
    }
}