

using Autodesk.Revit.DB;
using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.services;
public class WallService
{
    private Document _doc;
    private Dictionary<string, WallType> DictionaryWall;
    public WallService(Document doc)
    {   
        this._doc = doc;
    }
    public Wall CreateWall(WallConfig wallconfig,   XYZ startPoint, XYZ endpoint, double BaseOffset = 0)
    {
        double targetThicknessWall =  RevitUtil.convertToMeter(wallconfig.ThicknessWall) ;  

        WallType? wallTypeWithThicknessInitOrFirstDefault = new FilteredElementCollector(_doc)
        .OfClass(typeof(WallType)).Cast<WallType>()
        .FirstOrDefault(
            x=> 
            x.Kind == WallKind.Basic && Math.Abs(x.GetCompoundStructure().GetWidth() - targetThicknessWall) < 1e-6);

        Curve line = Line.CreateBound(startPoint, endpoint); //Curve

        Level level = new FilteredElementCollector(_doc).OfClass(typeof(Level)).Cast<Level>()
        .FirstOrDefault(x => x.Name == wallconfig.LevelName);

        if(level == null)
        {
            throw new DllNotFoundException("Không tìm thấy level tương ứng. Check level tồn tại hay không");
        }
        return Wall.Create(_doc, line, wallTypeWithThicknessInitOrFirstDefault.Id, level.LevelId, level.Elevation, BaseOffset, true, wallconfig.IsStructural );



    }


 
}