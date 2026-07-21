using Autodesk.Revit.DB;
using BuildHouse.Models;
using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.services;

public class HouseBuilder
{
    private readonly Document _doc;
    private readonly FloorService _floorService;
    private readonly HouseConfig _config;
    private List<(XYZ startPoint, XYZ endPoint)> listSegment = new();

    public HouseBuilder(Document doc, HouseConfig? config = null)
    {
        _doc = doc;
        _floorService = new FloorService(doc);
        _config = config ?? HouseConfig.CreateDefault();
        _config.Normalize();
    }

    public bool Build(XYZ centerPoint, out string message)
    {
        AppLog.Information("HouseBuilder.Build start");


        GridService gridService = new GridService(centerPoint, _doc, _config.Grid.ConvertToGridConfig());
        gridService.CreateGrids();
        AppLog.Information("Grids created");

        Dictionary<string, Level> levels = CreateLevels();
        AppLog.Information("Levels created/updated");

        Level level1 = RequireLevel(levels, _config.BaseLevelName);
        Level level2 = RequireLevel(levels, _config.StructuralTopLevelName);

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

        CreateWallFromService(_doc, _config.Wall.ToConfig(), level1, level2, columns);

        CreateBeamFromService(_doc, _config.Beam.ToConfig(), level2);
        AppLog.Information("Beam finished");
        AppLog.Information("HouseBuilder.Build finished");
        return true;
    }

    private Dictionary<string, Level> CreateLevels()
    {
        return LevelService.CreateOrUpdateLevel(_doc, _config.Levels);
    }

    private static Level RequireLevel(Dictionary<string, Level> levels, string name)
    {
        if (!levels.TryGetValue(name, out Level? level))
        {
            throw new InvalidOperationException($"Không tìm thấy level '{name}'.");
        }
        return level;
    }

    private bool CreateBlindingFloor(
        GridService gridService,
        Level level1,
        out string message)
    {
        message = string.Empty;

        BlindingConcreteConfig blindingConcreteConfig = _config.Blinding.ToConfig(_config.BaseLevelName);
        ElementId blindingFloorTypeId = _floorService.getFloorTypeIdOrCreateNew(blindingConcreteConfig);
        if (blindingFloorTypeId == null)
        {
            message = "Không tìm thấy kiểu sàn nào trong dự án!";
            return false;
        }

        CurveLoop footingLoop = gridService.createFootprintLoop(blindingConcreteConfig.EdgeExtension);
        Floor floor = _floorService.createFloor(
            new List<CurveLoop> { footingLoop },
            blindingFloorTypeId,
            level1.Id);

        bool isSuccessOffset = _floorService.setOffsetFromInInitialPosition(
            floor,
            blindingConcreteConfig.offsetFromLevelTarget);
        if (!isSuccessOffset)
        {
            message = $"Không thể set offset từ {_config.BaseLevelName} {blindingConcreteConfig.offsetFromLevelTarget}!";
            return false;
        }

        return true;
    }

    private bool CreateFloor(GridService gridService, ElementId levelId, out string message)
    {
        message = string.Empty;

        FloorConfig floorConfig = _config.Floor.ToConfig(_config.BaseLevelName);

        ElementId floorTypeId = _floorService.getFloorTypeIdOrCreateNew(floorConfig);
        if (floorTypeId == null)
        {
            message = "Không tìm thấy kiểu sàn tầng 1 trong dự án!";
            return false;
        }

        CurveLoop floorLoop = gridService.createFootprintLoop(0);
        _floorService.createFloor(new List<CurveLoop> { floorLoop }, floorTypeId, levelId);
        return true;
    }

    private List<Element> CreateColumns(GridService gridService, Level bottomLevel, Level topLevel)
    {
        ElementId levelId = bottomLevel.Id;
        ElementId level2Id = topLevel.Id;

        ColumnService columnService = new ColumnService();

        // HalfWidthMm → feet trước khi đưa vào footprint (GridService nhận internal)
        double halfColumnWidth = RevitUtil.ConvertToFeet(_config.Column.HalfWidthMm);
        (double left, double right, double bottom, double top) = gridService.getFootprintBounds(-halfColumnWidth);

        XYZ[] locations =
        {
            new XYZ(left, bottom, bottomLevel.Elevation),
            new XYZ(right, bottom, bottomLevel.Elevation),
            new XYZ(left, top, bottomLevel.Elevation),
            new XYZ(right, top, bottomLevel.Elevation),
        };

        var created = new List<Element>();
        for (int i = 0; i < locations.Length; i++)
        {
            ColumnConfig columnConfig = _config.Column.ToConfig(levelId, level2Id);
            FamilyInstance? column = columnService.CreateColumn(_doc, columnConfig, locations[i]);
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
