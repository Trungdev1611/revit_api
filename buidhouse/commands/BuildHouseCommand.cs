using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Simpleform.buidhouse.services;
using Simpleform.drawWallRefactor;

namespace Simpleform.buidhouse.commands;

[Transaction(TransactionMode.Manual)]
public class BuildHouseCommand : BaseClass
{
    protected override Result ExcuteInside(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            //pick point
            TaskDialog.Show("Thông báo", "bạn hãy chọn một điểm tâm của ngôi nhà trên màn hình");
            XYZ centerPoint = base.uidoc.Selection.PickPoint();
            if (centerPoint == null)
            {
                message = "Bạn đã hủy lệnh chọn điểm";
                return Result.Cancelled;
            }

            //start transaction
            using Transaction t = new Transaction(doc, "BuildHouse");
            {
                t.Start();

                HouseBuilder houseBuilder = new HouseBuilder(doc);
                if (!houseBuilder.Build(centerPoint, out message))
                {
                    return Result.Failed;
                }

                t.Commit();
            }

            return Result.Succeeded;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw (e);
        }
    }
}
