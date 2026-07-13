

using Autodesk.Revit.DB;
using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.services;
public class WallService
{
    private Document _doc;
    public WallService(Document doc)
    {   
        this._doc = doc;
    }
    public void createWall(WallConfig wallconfig, XYZ startPoint, XYZ endpoint)
    {
        double targetThicknessWall =  RevitUtil.convertToMeter(wallconfig.ThicknessWall) ;  

        WallType? wallTypeWithThicknessInitOrFirstDefault = new FilteredElementCollector(_doc)
        .OfClass(typeof(WallType)).Cast<WallType>()
        .FirstOrDefault(
            x=> 
            x.Kind == WallKind.Basic && Math.Abs(x.GetCompoundStructure().GetWidth() - targetThicknessWall) < 1e-6);

        Line line = Line.CreateBound(startPoint, endpoint);

        Level level = new FilteredElementCollector(_doc).OfClass(typeof(Level)).Cast<Level>()
        .FirstOrDefault(x => x.Name == wallconfig.LevelName);

        if(level == null)
        {
            throw new DllNotFoundException("Không tìm thấy level tương ứng. Check level tồn tại hay không");
        }
        



    }


 
}