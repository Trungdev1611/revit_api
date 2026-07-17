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
    private List<(XYZ startPoint, XYZ endPoint)> listSegment = new();

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
        AppLog.Information("Levels created/updated");

        Level level1 = RequireLevel(levels, "Level 1");
        Level level2 = RequireLevel(levels, "Level 2");

        if (!CreateBlindingFloor(gridService, level1, out message))
        {
            AppLog.Warning("CreateBlindingFloor failed: {0}", message);
            return false;
        }

        if (!CreateFloor(gridService, level1.Id, out message))
        {
            AppLog.Warning("CreateFloor failed: {0}", message);
            return false;
        }

        List<Element> columns = CreateColumns(gridService, level1, level2);
        AppLog.Information("Columns step done: {0} columns", columns.Count);
        _doc.Regenerate();
        var wallConfig = new WallConfig(220);
        CreateWallFromService(_doc, wallConfig, level1, level2, columns);


        var beamConfig = new BeamConfig(220, 400);
        CreateBeamFromService(_doc, beamConfig, level2); //level2 là đỉnh dầm truyền vào vì dầm luôn ăn theo sàn
        AppLog.Information("Beam finished");
        AppLog.Information("HouseBuilder.Build finished");
        return true;
    }

    //create grid levels — giữ map tên→Level để các bước sau reuse
    private Dictionary<string, Level> CreateLevels()
    {
        List<LevelConfig> levelConfigs = new List<LevelConfig> {
            new LevelConfig("Level 1", 0),
            new LevelConfig("Level 2", 3600),
            new LevelConfig("Level 3", 6900),
        };
        return LevelService.CreateOrUpdateLevel(_doc, levelConfigs);
    }

    private static Level RequireLevel(Dictionary<string, Level> levels, string name)
    {
        if (!levels.TryGetValue(name, out Level? level))
        {
            throw new InvalidOperationException($"Không tìm thấy level '{name}'.");
        }
        return level;
    }

    //create blinding floor
    private bool CreateBlindingFloor(
        GridService gridService,
        Level level1,
        out string message)
    {
        message = string.Empty;

        BlindingConcreteConfig blindingConcreteConfig = new BlindingConcreteConfig();
        ElementId blindingFloorTypeId = _floorService.getFloorTypeIdOrCreateNew(blindingConcreteConfig);
        if (blindingFloorTypeId == null)
        {
            message = "Không tìm thấy kiểu sàn nào trong dự án!";
            return false;
        }

        CurveLoop footingLoop = gridService.createFootprintLoop(blindingConcreteConfig.EdgeExtensionMm);
        Floor floor = _floorService.createFloor(
            new List<CurveLoop> { footingLoop },
            blindingFloorTypeId,
            level1.Id);

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

    //create columns — trả về đúng cột vừa tạo (không phụ thuộc Active View)
    private List<Element> CreateColumns(GridService gridService, Level bottomLevel, Level topLevel)
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

        double halfColumnWidth = 220 / 2; //cột 220x220
        (double left, double right, double bottom, double top) = gridService.getFootprintBounds(-halfColumnWidth);

        XYZ col1Xyz = new XYZ(left, bottom, bottomLevel.Elevation);
        XYZ col2Xyz = new XYZ(right, bottom, bottomLevel.Elevation);
        XYZ col3Xyz = new XYZ(left, top, bottomLevel.Elevation);
        XYZ col4Xyz = new XYZ(right, top, bottomLevel.Elevation);

        XYZ[] locations = { col1Xyz, col2Xyz, col3Xyz, col4Xyz };
        var created = new List<Element>();
        for (int i = 0; i < columnConfigs.Count; i++)
        {
            FamilyInstance? column = columnService.CreateColumn(_doc, columnConfigs[i], locations[i]);
            if (column != null)
            {
                created.Add(column);
            }
        }

        if (created.Count < 2)
        {
            throw new InvalidOperationException(
                $"Chỉ tạo được {created.Count} cột (cần >= 2). Kiểm tra load family cột.");
        }

        return created;
    }

    private List<Wall> CreateWallFromService(
        Document doc,
        WallConfig wallConfig,
        Level baseLevel,
        Level topLevel,
        List<Element> columns)
    {
        if (columns == null || columns.Count < 2)
        {
            throw new InvalidOperationException("Không đủ cột để vẽ tường.");
        }

        WallService wallService = new WallService(doc);
        List<Wall> listWall = wallService.CreateWallFromColumns(
            columns,
            wallConfig,
            baseLevel,
            topLevel,
            out List<(XYZ startPoint, XYZ endPoint)> segments);
        this.listSegment = segments;
        AppLog.Information("Walls done: {0} walls, {1} segments for beams",
            listWall.Count, this.listSegment.Count);
        return listWall;
    }

    private void CreateBeamFromService(Document doc, BeamConfig beamConfig, Level level)
    {
        if (this.listSegment == null || this.listSegment.Count < 1)
        {
            throw new InvalidOperationException("Không đủ segment tường để tạo dầm");
        }

        BeamService beamService = new BeamService(doc);
        int created = 0;
        for (int i = 0; i < listSegment.Count; i++)
        {
            FamilyInstance? beam = beamService.CreateBeam(
                beamConfig,
                level,
                listSegment[i].startPoint,
                listSegment[i].endPoint);
            if (beam != null)
            {
                created++;
            }
        }

        AppLog.Information("Beam finished: created {0}/{1} at level '{2}' elev={3:F3}",
            created, listSegment.Count, level.Name, level.Elevation);

        if (created == 0)
        {
            throw new InvalidOperationException("Không tạo được dầm nào.");
        }
    }
}

