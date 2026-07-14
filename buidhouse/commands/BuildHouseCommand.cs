using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Simpleform.buidhouse.services;
using Simpleform.buidhouse.utils;
using Simpleform.drawWallRefactor;

namespace Simpleform.buidhouse.commands;

[Transaction(TransactionMode.Manual)]
public class BuildHouseCommand : BaseClass
{
    protected override Result ExcuteInside(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        AppLog.Init();
        AppLog.Information("BuildHouseCommand start. Doc={0}", doc?.Title);

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

            AppLog.Information("Picked center=({0}, {1}, {2})", centerPoint.X, centerPoint.Y, centerPoint.Z);

            //start transaction
            using Transaction t = new Transaction(doc, "BuildHouse");
            {
                t.Start();
                AppLog.Information("Transaction started");

                HouseBuilder houseBuilder = new HouseBuilder(doc);
                if (!houseBuilder.Build(centerPoint, out message))
                {
                    AppLog.Warning("HouseBuilder.Build failed: {0}", message);
                    return Result.Failed;
                }

                t.Commit();
                AppLog.Information("Transaction committed");
            }

            return Result.Succeeded;
        }
        catch (Exception e)
        {
            AppLog.Error(e, "BuildHouseCommand crashed");
            TaskDialog.Show(
                "Error",
                $"{e.GetType().Name}: {e.Message}\n\nXem log:\n{AppLog.LogPath}\n\n{e.StackTrace}");
            return Result.Failed;
        }
    }
}
