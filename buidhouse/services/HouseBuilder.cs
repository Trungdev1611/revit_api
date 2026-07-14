using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BuildHouse.Models;
using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;
using Simpleform.drawWallRefactor;

namespace Simpleform.buidhouse.services;

public class HouseBuilder
{
    private readonly Document _doc;
    private readonly FloorService _floorService;

    public HouseBuilder(Document doc)
    {
        _doc = doc;
        _floorService = new FloorService(doc);
    }

    public bool Build(XYZ centerPoint, out string message)
    {
        AppLog.Information("HouseBuilder.Build start");

        GridService gridService = new GridService(centerPoint, _doc);
        gridService.createGrids();
        AppLog.Information("Grids created");

        CreateLevels();
        AppLog.Information("Levels created/updated");

        if (!CreateBlindingFloor(gridService, out message, out Level level1))
        {
            AppLog.Warning("CreateBlindingFloor failed: {0}", message);
            return false;
        }

        if (!CreateFloor(gridService, level1.Id, out message))
        {
            AppLog.Warning("CreateFloor failed: {0}", message);
            return false;
        }

        CreateColumns(gridService, level1);
        AppLog.Information("Columns step done");
        _doc.Regenerate();
        var wallConfig = new WallConfig(220, "Level 1");
        CreateWallFromService(_doc, wallConfig);
        AppLog.Information("HouseBuilder.Build finished");

        return true;
    }

    //create grid levels
    private void CreateLevels()
    {
        List<LevelConfig> levelConfigs = new List<LevelConfig> {
            new LevelConfig("Level 1", 0),
            new LevelConfig("Level 2", 3600),
            new LevelConfig("Level 3", 6900),
        };
        LevelService.CreateOrUpdateLevel(_doc, levelConfigs);
    }

    //create blinding floor
    private bool CreateBlindingFloor(
        GridService gridService,
        out string message,
        out Level level1)
    {
        message = string.Empty;
        level1 = null!;

        BlindingConcreteConfig blindingConcreteConfig = new BlindingConcreteConfig();
        ElementId blindingFloorTypeId = _floorService.getFloorTypeIdOrCreateNew(blindingConcreteConfig);
        if (blindingFloorTypeId == null)
        {
            message = "Không tìm thấy kiểu sàn nào trong dự án!";
            return false;
        }

        //get level1
        level1 = _doc.GetFirstItemOrCondition<Level>(Level => Level.Name == "Level 1");
        ElementId levelId = level1.Id;
        CurveLoop footingLoop = gridService.createFootprintLoop(blindingConcreteConfig.EdgeExtensionMm);
        Floor floor = _floorService.createFloor(new List<CurveLoop> { footingLoop }, blindingFloorTypeId, levelId);

        //offset floor to offsetFromLevelTarget = -150mm
        bool isSuccessOffset = _floorService.setOffsetFromInInitialPosition(floor, blindingConcreteConfig.offsetFromLevelTarget);
        if (!isSuccessOffset)
        {
            message = "Không thể set offset từ level1 -150mm!";
            return false;
        }

        return true;
    }

    //create floor
    private bool CreateFloor(GridService gridService, ElementId levelId, out string message)
    {
        message = string.Empty;

        FloorConfig floorConfig = new FloorConfig(
            "Betong san tang 1",
            RevitUtil.convertToMeter(100),
            "Level 1");

        ElementId floorTypeId = _floorService.getFloorTypeIdOrCreateNew(floorConfig);
        if (floorTypeId == null)
        {
            message = "Không tìm thấy kiểu sàn tầng 1 trong dự án!";
            return false;
        }

        CurveLoop floorLoop = gridService.createFootprintLoop(0); //không có edge extension
        _floorService.createFloor(new List<CurveLoop> { floorLoop }, floorTypeId, levelId);
        return true;
    }

    //create columns
    private void CreateColumns(GridService gridService, Level level1)
    {
        ElementId levelId = level1.Id;
        Level level2 = _doc.GetFirstItemOrCondition<Level>(Level => Level.Name == "Level 2");
        ElementId level2Id = level2.Id;

        var columnConfigs = new List<ColumnConfig> {
            new ColumnConfig("220x220", levelId, level2Id),
            new ColumnConfig("220x220", levelId, level2Id),
            new ColumnConfig("220x220", levelId, level2Id),
            new ColumnConfig("220x220", levelId, level2Id),

        };
        ColumnService columnService = new ColumnService();

        //lấy tọa độ 4 điểm của cột
        double halfColumnWidth = 220 / 2; //cột 220x220
        (double left, double right, double bottom, double top) = gridService.getFootprintBounds(-halfColumnWidth);

        //tọa độ cột
        XYZ col1Xyz = new XYZ(left, bottom, level1.Elevation);
        XYZ col2Xyz = new XYZ(right, bottom, level1.Elevation);
        XYZ col3Xyz = new XYZ(left, top, level1.Elevation);
        XYZ col4Xyz = new XYZ(right, top, level1.Elevation);

        columnService.CreateColumn(_doc, columnConfigs[0], col1Xyz);
        columnService.CreateColumn(_doc, columnConfigs[1], col2Xyz);
        columnService.CreateColumn(_doc, columnConfigs[2], col3Xyz);
        columnService.CreateColumn(_doc, columnConfigs[3], col4Xyz);
    }

    private void CreateWallFromService(Document doc, WallConfig wallConfig)
    {
        // var listWall = new List<WallConfig>() {
        //     new WallConfig(220, "level1", true),
        //     new WallConfig(220, "level1", true),
        //     new WallConfig(220, "level1", true),
        //     new WallConfig(220, "level1", true),
        // };
        //  Quét toàn bộ Cột kết cấu trong View hiện hành
        List<Element> columns = new FilteredElementCollector(doc, doc.ActiveView.Id)
        .OfCategory(BuiltInCategory.OST_StructuralColumns)
        .WhereElementIsNotElementType()
        .ToList();

        if(columns.Count < 2)
        {
            TaskDialog.Show("Thông báo", "Không đủ số lượng cột trên mặt bằng này để vẽ tường");
            return ;
        }

        // --- BƯỚC 1: QUÉT CỘT VÀ VẼ TƯỜNG NỐI TÂM ---
        AppLog.Information("CreateWallFromService: {0} columns", columns.Count);
        for(int i  = 0; i< columns.Count; i++)
        {
            for(int j = i + 1; j<columns.Count; j++)
            {
                Element colA = columns[i];
                Element colB = columns[j];

                //lấy vị trí tâm cột A, tâm cột B
                LocationPoint locA = colA.Location as LocationPoint;
                LocationPoint locB = colB.Location as LocationPoint;
                
                if(locA == null || locB == null) continue;

                XYZ pointA = locA.Point;
                XYZ pointB = locB.Point;

                // Chỉ nối cột thẳng hàng ngang hoặc thẳng đứng — bỏ đường chéo
                XYZ delta = pointB - pointA;
                bool isHorizontal = Math.Abs(delta.Y) < 0.1 && Math.Abs(delta.X) > 0.1;
                bool isVertical = Math.Abs(delta.X) < 0.1 && Math.Abs(delta.Y) > 0.1;
                if (!isHorizontal && !isVertical)
                {
                    continue;
                }

                if (delta.GetLength() < 1e-9)
                {
                    AppLog.Warning("Skip wall: col {0}->{1} same point", i, j);
                    continue;
                }
                XYZ direction = delta.Normalize();

                //2. Lấy kích thước (độ rộng/độ dày) của cột A và cột B từ parameter của Type
                ElementType colTypeA = doc.GetElement(colA.GetTypeId()) as ElementType;
                ElementType colTypeB = doc.GetElement(colB.GetTypeId()) as ElementType;
                if (colTypeA == null || colTypeB == null)
                {
                    AppLog.Warning("Skip wall: missing column type for {0}->{1}", i, j);
                    continue;
                }

                Parameter? paramBA = colTypeA.LookupParameter("b");
                Parameter? paramBB = colTypeB.LookupParameter("b");
                if (paramBA == null || paramBB == null)
                {
                    AppLog.Error(
                        "Null column size param b. TypeA={0} TypeB={1}",
                        colTypeA.Name, colTypeB.Name);
                    continue;
                }

                // Cột vuông thường chỉ có b; nếu không có h thì dùng b
                Parameter? paramHA = colTypeA.LookupParameter("h") ?? paramBA;
                Parameter? paramHB = colTypeB.LookupParameter("h") ?? paramBB;

                double widthA = paramBA.AsDouble();
                double widthB = paramBB.AsDouble();
                double heightA = paramHA.AsDouble();
                double heightB = paramHB.AsDouble();

                // 3. Xác định xem hướng vẽ tường đang đi theo chiều nào của cột để lấy nửa độ rộng cho đúng
                double offsetA = 0;
                double offsetB = 0;
                // Nếu tường chạy dọc theo trục X (thẳng hàng ngang) -> Dùng nửa chiều rộng "b"
                if(Math.Abs(direction.Y) < 0.1)
                {
                    offsetA = widthA / 2;
                    offsetB = widthB / 2;
                }
                if(Math.Abs(direction.X) < 0.1)
                {
                    offsetA = heightA /2;
                    offsetB = heightB / 2;
                }
                //4. tịnh tiến điểm tâm ra điểm mép bằng toán vector
                XYZ wallStartPoint = pointA + direction * offsetA;
                XYZ wallEndPoint = pointB - direction * offsetB;
                Parameter? baseOffsetParam = colA.LookupParameter("Base Offset");
                if (baseOffsetParam == null)
                {
                    AppLog.Warning("Skip wall: missing Base Offset on column {0}", i);
                    continue;
                }
                double BaseOffsetWall = baseOffsetParam.AsDouble();

                WallService wallService = new WallService(_doc);
                wallService.CreateWall(wallConfig, wallStartPoint, wallEndPoint, BaseOffsetWall);
                
            }
        }
  
       

    }

    private void CreateBeamFromService()
    {
        
    }
}
