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

        GridService gridService = new GridService(centerPoint, _doc);
        gridService.createGrids();

        CreateLevels();

        if (!CreateBlindingFloor(gridService, out message, out Level level1))
        {
            return false;
        }

        if (!CreateFloor(gridService, level1.Id, out message))
        {
            return false;
        }

        CreateColumns(gridService, level1);

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
        for(int i  = 0; i< columns.Count; i++)
        {
            for(int j = 1; j<columns.Count; j++)
            {
                Element colA = columns[i];
                Element colB = columns[j];

                //lấy vị trí tâm cột A, tâm cột B
                LocationPoint locA = colA.Location as LocationPoint;
                LocationPoint locB = colB.Location as LocationPoint;
                
                if(locA == null || locB == null) continue;

                XYZ pointA = locA.Point;
                XYZ pointB = locB.Point;
                // 1. Tính toán Vector hướng từ cột A sang cột B và chuẩn hóa nó (độ dài = 1)
                XYZ direction = (pointB = pointA).Normalize();

                //2. Lấy kích thước (độ rộng/độ dày) của cột A và cột B từ parameter của Type
                //Trong revit, cột kết cấu thường là bxh (trừ khi người dùng tạo có thể có tên khác)
                ElementType colTypeA = doc.GetElement(colA.GetTypeId()) as ElementType;
                ElementType colTypeB = doc.GetElement(colB.GetTypeId()) as ElementType;

                //Lấy giá trị b và h từ Family Parameter (Feet)
                double widthA = colTypeA.LookupParameter("b").AsDouble();
                double widthB = colTypeB.LookupParameter("b").AsDouble();

                double heightA = colTypeA.LookupParameter("h").AsDouble();
                double heightB = colTypeB.LookupParameter("h").AsDouble();

                // 3. Xác định xem hướng vẽ tường đang đi theo chiều nào của cột để lấy nửa độ rộng cho đúng
                double offsetA = 0;
                double offsetB = 0;
                // Nếu tường chạy dọc theo trục X (thẳng hàng ngang) -> Dùng nửa chiều rộng "b"
                if(Math.Abs(direction.Y) < 0.1)
                {
                    offsetA = widthA / 2; //vì lấy tọa độ tim cột nên cần offset nửa cột để ra mép - chuẩn xác thay vì để tường và cột tự join nhau
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
                double BaseOffsetWall = colA.LookupParameter("Base Offset").AsDouble();

                WallService wallService = new WallService(_doc);
                wallService.CreateWall(wallConfig, wallStartPoint, wallEndPoint)
                
            }
        }
  
       

    }

    private void CreateBeamFromService()
    {
        
    }
}
