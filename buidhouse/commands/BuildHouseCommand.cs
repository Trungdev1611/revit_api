using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
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
            
            
            return Result.Succeeded;
        }
        
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    
    }
}