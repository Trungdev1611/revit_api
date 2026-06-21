using Autodesk.Revit.DB;

namespace Simpleform.drawWallRefactor;

public  class BuildWall
{
    private readonly Document _doc;
    private readonly ElementId _levelId;
    private readonly ElementId _textTypeId;
    private readonly double _increaseAmount;
    private double _gapBetweenWalls = UnitUtils.ConvertToInternalUnits(3000, UnitTypeId.Millimeters);
    private double _lengthInit = UnitUtils.ConvertToInternalUnits(2000, UnitTypeId.Millimeters);



    public BuildWall(Document doc, double increaseAmount)
    {
        _doc = doc;
        //get first level in view
        _levelId = doc.getFirstLevelInView();
        //get default text to write
        _textTypeId = doc.getTextDefault();
        _increaseAmount = increaseAmount;
    }


    public XYZ buildParallelWall(XYZ pickStart, WallType wallTypeItem, int orderWall)
    {
                XYZ coordinateStart = new XYZ(pickStart.X, pickStart.Y + orderWall * _gapBetweenWalls, pickStart.Z);
                XYZ coordinateEnd = new XYZ(pickStart.X + _lengthInit, pickStart.Y + orderWall * _gapBetweenWalls,
                    pickStart.Z);
                Line wallLine = Line.CreateBound(coordinateStart, coordinateEnd);
                Wall newWall = Wall.Create(_doc, wallLine, _levelId, false);
                newWall.WallType = wallTypeItem;
                
                this._lengthInit +=  _increaseAmount;
                return coordinateStart;
    }

    public void buildTextDescriptionWall(XYZ coordinateStart, double lengthItemMax,WallType itemTypeWall   )
    {
        XYZ coordinateStartText = new XYZ(coordinateStart.X + lengthItemMax, coordinateStart.Y, coordinateStart.Z);
        TextNote.Create(
            _doc, _doc.ActiveView.Id, coordinateStartText, itemTypeWall.Name, _textTypeId
        );
    }
}