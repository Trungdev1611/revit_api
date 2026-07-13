using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BuildHouse.Models;
using Simpleform.buidhouse.models;
using Simpleform.buidhouse.services;
using Simpleform.buidhouse.utils;
using Simpleform.drawWallRefactor;

namespace Simpleform.buidhouse.commands;

[Transaction(TransactionMode.Manual)]
public class BuildHouseCommand :BaseClass
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
            using Transaction t = new Transaction(doc,"BuildHouse");
            {
                t.Start();

                GridService gridService = new GridService(centerPoint, doc);
                gridService.createGrids();

            //create grid levels
                List<LevelConfig> levelConfigs = new List<LevelConfig> {
                    new LevelConfig("Level 1", 0),
                    new LevelConfig("Level 2", 3600),
                    new LevelConfig("Level 3", 6900),
                };
                LevelService.CreateOrUpdateLevel(doc, levelConfigs);

                //create blinding floor

                BlindingConcreteConfig blindingConcreteConfig = new BlindingConcreteConfig();
                FloorService floorService = new FloorService(doc);
                ElementId floorTypeId = floorService.getFloorTypeIdOrCreateNew(blindingConcreteConfig);
                if(floorTypeId == null) {
                    message = "Không tìm thấy kiểu sàn nào trong dự án!";
                    return Result.Failed;
                }

                //get level1
                Level level1 = doc.GetFirstItemOrCondition<Level>(Level => Level.Name == "Level 1");
                ElementId levelId = level1.Id;
                CurveLoop footingLoop = gridService.createFootprintLoop(blindingConcreteConfig.EdgeExtensionMm);
                Floor floor = floorService.createFloor(new List<CurveLoop> { footingLoop }, floorTypeId, levelId);
                
                //offset floor to offsetFromLevelTarget = -150mm
                bool isSuccessOffset = floorService.setOffsetFromInInitialPosition(floor, blindingConcreteConfig.offsetFromLevelTarget);
                if(!isSuccessOffset) {
                    message = "Không thể set offset từ level1 -150mm!";
                    return Result.Failed;
                }

                //create floor
                FloorConfig floorConfig = new FloorConfig("Betong san tang1", 100, "Level 1");
                CurveLoop floorLoop = gridService.createFootprintLoop(0); //không có edge extension
                floorService.createFloor(new List<CurveLoop> { floorLoop }, floorTypeId, levelId);
                
                Level level2 = doc.GetFirstItemOrCondition<Level>(Level => Level.Name == "Level 2");
                ElementId level2Id = level2.Id;
                //create columns
                var columnConfigs = new List<ColumnConfig> {
                    new ColumnConfig("220x220", levelId, level2Id),
                    new ColumnConfig("220x220", levelId, level2Id),
                    new ColumnConfig("220x220", levelId, level2Id),
                    new ColumnConfig("220x220", levelId, level2Id),
                 
                };
                ColumnService columnService = new ColumnService();
                
                //lấy tọa độ 4 điểm của cột
                double halfColumnWidth = (220) / 2; //cột 220x220
                (double left, double right, double bottom, double top)  = gridService.getFootprintBounds(-halfColumnWidth);

                //tọa độ cột
                XYZ col1Xyz = new XYZ(left, bottom, level1.Elevation);
                XYZ col2Xyz = new XYZ(right, bottom , level1.Elevation);
                XYZ col3Xyz = new XYZ(left , top , level1.Elevation);
                XYZ col4Xyz = new XYZ(right, top, level1.Elevation);

                columnService.CreateColumn(doc, columnConfigs[0], col1Xyz);
                columnService.CreateColumn(doc, columnConfigs[1], col2Xyz);
                columnService.CreateColumn(doc, columnConfigs[2], col3Xyz);
                columnService.CreateColumn(doc, columnConfigs[3], col4Xyz);
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
