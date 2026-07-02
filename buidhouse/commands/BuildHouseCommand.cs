using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Simpleform.buidhouse.models;
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
         
            using Transaction t = new Transaction(doc,"BuildHouse");
            {
                t.Start();

                //Draw grid
                GridService gridService = new GridService(centerPoint, doc);
                CurveLoop curveLoop = gridService.createGridLines();

                //Create level
                List<LevelConfig> levelConfigs = new List<LevelConfig> {
                    new LevelConfig("Level 1", 0),
                    new LevelConfig("Level 2", 3600),
                    new LevelConfig("Level 3", 6900),
                };
                LevelService.CreateOrUpdateLevel(doc, levelConfigs);

                //Create blinding concrete (bê tông lót)
                BlindingConcreteConfig blindingConcreteConfig = new BlindingConcreteConfig();
                FloorService floorService = new FloorService(doc);
                ElementId floorTypeId = floorService.getFloorTypeIdOrCreateNew(blindingConcreteConfig);
                if(floorTypeId == null) {
                    message = "Không tìm thấy kiểu sàn nào trong dự án!";
                    return Result.Failed;
                }

                Level level1 = doc.GetFirstItemOrCondition<Level>(Level => Level.Name == "Level 1");
                ElementId levelId = level1.Id;
                //Create floor
                floorService.createBlindingConcrete(new List<CurveLoop> { curveLoop }, blindingConcreteConfig, floorTypeId, levelId);
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