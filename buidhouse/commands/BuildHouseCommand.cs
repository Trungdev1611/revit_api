using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Simpleform.buidhouse.models;
using Simpleform.buidhouse.services;
using Simpleform.drawWallRefactor;

namespace Simpleform.buidhouse.commands;

/// <summary>
/// Entry point add-in Quick House Builder.
/// Kế thừa BaseClass để tái sử dụng uiapp/uidoc/doc và wrapper Transaction.
/// </summary>
[Transaction(TransactionMode.Manual)] // Transaction do command tự Start/Commit
public class BuildHouseCommand : BaseClass
{
    protected override Result ExcuteInside(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            // Bước 1: User chọn tâm nhà trên mặt bằng (ESC = null → Cancelled)
            TaskDialog.Show("Thông báo", "bạn hãy chọn một điểm tâm của ngôi nhà trên màn hình");
            XYZ centerPoint = base.uidoc.Selection.PickPoint();
            if (centerPoint == null)
            {
                message = "Bạn đã hủy lệnh chọn điểm";
                return Result.Cancelled;
            }

            // Bước 2: Mọi thay đổi model phải nằm trong Transaction
            using Transaction t = new Transaction(doc, "BuildHouse");
            {
                t.Start();

                // --- Grid: 4 trục định vị ô nhà 5x6m ---
                GridService gridService = new GridService(centerPoint, doc);
                gridService.createGrids();

                // --- Level: tạo hoặc cập nhật cao độ 3 tầng (mm → feet trong LevelService) ---
                List<LevelConfig> levelConfigs = new List<LevelConfig> {
                    new LevelConfig("Level 1", 0),
                    new LevelConfig("Level 2", 3600),
                    new LevelConfig("Level 3", 6900),
                };
                LevelService.CreateOrUpdateLevel(doc, levelConfigs);

                // --- Bê tông lót: tìm/tạo FloorType, vẽ sàn theo biên offset 100mm ---
                BlindingConcreteConfig blindingConcreteConfig = new BlindingConcreteConfig();
                FloorService floorService = new FloorService(doc);
                ElementId floorTypeId = floorService.getFloorTypeIdOrCreateNew(blindingConcreteConfig);
                if (floorTypeId == null)
                {
                    message = "Không tìm thấy kiểu sàn nào trong dự án!";
                    return Result.Failed;
                }

                Level level1 = doc.GetFirstItemOrCondition<Level>(level => level.Name == "Level 1");
                ElementId levelId = level1.Id;

                // CurveLoop khép kín (4 cạnh), không dùng line grid
                CurveLoop footingLoop = gridService.createFootprintLoop(blindingConcreteConfig.EdgeExtensionMm);
                Floor floor = floorService.createBlindingConcrete(
                    new List<CurveLoop> { footingLoop },
                    blindingConcreteConfig,
                    floorTypeId,
                    levelId);

                // Hạ sàn 150mm so với Level 1 (offset âm trong feet)
                bool isSuccessOffset = floorService.setOffsetFromInInitialPosition(
                    floor,
                    blindingConcreteConfig.offsetFromLevelTarget);
                if (!isSuccessOffset)
                {
                    message = "Không thể set offset từ level1 -150mm!";
                    return Result.Failed;
                }

                t.Commit();
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
