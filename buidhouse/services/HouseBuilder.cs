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

        Dictionary<string, Level> levels = CreateLevels();
        Level level1 = levels["Level 1"];
        Level level2 = levels["Level 2"];
        AppLog.Information("Levels created/updated");

        if (!CreateBlindingFloor(gridService, out message, level1))
        {
            AppLog.Warning("CreateBlindingFloor failed: {0}", message);
            return false;
        }

        if (!CreateFloor(gridService, level1.Id, out message))
        {
            AppLog.Warning("CreateFloor failed: {0}", message);
            return false;
        }

        CreateColumns(gridService, level1, level2);
        AppLog.Information("Columns step done");
        _doc.Regenerate();

        var wallConfig = new WallConfig(220);
        CreateWallFromService(_doc, wallConfig, level1);
        AppLog.Information("HouseBuilder.Build finished");

        return true;
    }

    //create grid levels
    private Dictionary<string, Level> CreateLevels()
    {
        List<LevelConfig> levelConfigs = new List<LevelConfig> {
            new LevelConfig("Level 1", 0),
            new LevelConfig("Level 2", 3600),
            new LevelConfig("Level 3", 6900),
        };
        return LevelService.CreateOrUpdateLevel(_doc, levelConfigs);
    }

    //create blinding floor
    private bool CreateBlindingFloor(
        GridService gridService,
        out string message,
        Level level
        )
    {
        message = string.Empty;
 

        BlindingConcreteConfig blindingConcreteConfig = new BlindingConcreteConfig();
        ElementId blindingFloorTypeId = _floorService.getFloorTypeIdOrCreateNew(blindingConcreteConfig);
        if (blindingFloorTypeId == null)
        {
            message = "Không tìm thấy kiểu sàn nào trong dự án!";
            return false;
        }

        ElementId levelId = level.Id;
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
    private void CreateColumns(GridService gridService, Level bottomLevel, Level topLevel)
    {
        ElementId levelId = bottomLevel.Id;
        ElementId level2Id = topLevel.Id;

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
        XYZ col1Xyz = new XYZ(left, bottom, bottomLevel.Elevation);
        XYZ col2Xyz = new XYZ(right, bottom, bottomLevel.Elevation);
        XYZ col3Xyz = new XYZ(left, top, bottomLevel.Elevation);
        XYZ col4Xyz = new XYZ(right, top, bottomLevel.Elevation);

        columnService.CreateColumn(_doc, columnConfigs[0], col1Xyz);
        columnService.CreateColumn(_doc, columnConfigs[1], col2Xyz);
        columnService.CreateColumn(_doc, columnConfigs[2], col3Xyz);
        columnService.CreateColumn(_doc, columnConfigs[3], col4Xyz);
    }

    private void CreateWallFromService(Document doc, WallConfig wallConfig, Level level)
    {

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
        WallService wallService = new WallService(_doc);

        wallService.CreateWallFromColumns(columns, wallConfig, level);
  
       

    }

    private void CreateBeamFromService()
    {
        
    }
}
