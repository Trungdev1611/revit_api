using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Simpleform.drawWallRefactor;

[Transaction(TransactionMode.Manual)]
public class Main: BaseClass
{
    private double increaseAmount = UnitUtils.ConvertToInternalUnits(1000, UnitTypeId.Millimeters);
    
    protected override Result ExcuteInside(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        List<WallType> listWallTypes = doc.getListTypeWallBuiltIn();
        if (listWallTypes.Count == 0)
        {
            message = "not found any type of wall";
            TaskDialog.Show("Error", "Không tìm thấy kiểu tường nào trong dự án!");
            return Result.Failed;
        }
        try
        {
            List<string> nameWalls = new();
            //require user pick a point in view screen
            TaskDialog.Show("Thong bao", "Chon 1 diem de ve");
            XYZ pickStart = uidoc.Selection.PickPoint("Pick Wall Point");

            var buildWall = new BuildWall(doc, increaseAmount); 
            
            using Transaction t = new Transaction(doc, "DrawWall1");
            {
                t.Start();
                var count = listWallTypes.Count;
                var lengMax = count * increaseAmount;
                
                for (int index = 0; index < count; index++)
                {
                    WallType wallTypeItem = listWallTypes[index];
                    XYZ coordinateStart = buildWall.buildParallelWall(pickStart,wallTypeItem , index);

                    buildWall.buildTextDescriptionWall(coordinateStart, lengMax,  wallTypeItem); 
                    nameWalls.Add(wallTypeItem.Name);
                }
                
                TaskDialog.Show("List name wall", string.Join(", ", nameWalls));
                t.Commit();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
        return Result.Succeeded;
    }
    
}