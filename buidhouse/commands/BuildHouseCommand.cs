using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Simpleform.buidhouse.services;
using Simpleform.drawWallRefactor;

namespace Simpleform.buidhouse.commands;

//nơi tạo commands
[Transaction(TransactionMode.Manual)]
public class BuildHouseCommand :BaseClass
{
    protected override Result ExcuteInside(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            TaskDialog.Show("Thông báo", "bạn hãy chọn một điểm tâm của ngôi nhà trên màn hình");
            XYZ centerPoint = base.uidoc.Selection.PickPoint();
            if (centerPoint == null)
            {
                message = "Bạn đã hủy lệnh chọn điểm";
                return Result.Cancelled;
            }
            //Draw grid
            using Transaction t = new Transaction(doc,"BuildHouse");
            {
                t.Start();
                GridService gridService = new GridService(centerPoint, doc);
                gridService.createGridLines();
                t.Commit();
            }
            
            
            return Result.Succeeded;
        }
        
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw(e);
        }
    
    }
}