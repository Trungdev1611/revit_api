using Autodesk.Revit.DB;
using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.services;

public class WallService
{
    private const double ThicknessTolerance = 1e-6;
    private readonly Document _doc;

    public WallService(Document doc)
    {
        _doc = doc;
    }

    public Wall CreateWall(WallConfig wallconfig, XYZ startPoint, XYZ endpoint, Level level, double BaseOffset = 0)
    {
        WallType wallType = GetOrCreateWallType(wallconfig.ThicknessWall);

        Curve line = Line.CreateBound(startPoint, endpoint);

        double wallHeight = GetWallHeightFromLevel(level);

        return Wall.Create(
            _doc,
            line,
            wallType.Id,
            level.Id,
            wallHeight,
            BaseOffset,
            flip: false,
            structural: wallconfig.IsStructural);
    }

    /// <summary>
    /// Tìm WallType Basic đúng thickness (mm). Không có thì Duplicate type gốc rồi set thickness.
    /// </summary>
    private WallType GetOrCreateWallType(double thicknessMm)
    {
        double targetThickness = RevitUtil.convertToMeter(thicknessMm);
        string newTypeName = $"Basic Wall {thicknessMm:0}mm";

        List<WallType> basicTypes = new FilteredElementCollector(_doc)
            .OfClass(typeof(WallType))
            .Cast<WallType>()
            .Where(x => x.Kind == WallKind.Basic && x.GetCompoundStructure() != null)
            .ToList();

        WallType? existing = basicTypes.FirstOrDefault(x =>
            Math.Abs(x.GetCompoundStructure().GetWidth() - targetThickness) < ThicknessTolerance);
        if (existing != null)
        {
            return existing;
        }

        WallType? alreadyCreated = basicTypes.FirstOrDefault(x => x.Name == newTypeName);
        if (alreadyCreated != null)
        {
            CompoundStructure existingCs = alreadyCreated.GetCompoundStructure()!;
            if (Math.Abs(existingCs.GetWidth() - targetThickness) >= ThicknessTolerance)
            {
                SetTotalThickness(existingCs, targetThickness);
                alreadyCreated.SetCompoundStructure(existingCs);
            }
            return alreadyCreated;
        }

        WallType? source = basicTypes.FirstOrDefault();
        if (source == null)
        {
            throw new InvalidOperationException(
                "Không có WallType Basic nào trong project để nhân bản.");
        }

        WallType newWallType = (WallType)source.Duplicate(newTypeName);
        CompoundStructure compound = newWallType.GetCompoundStructure()
            ?? throw new InvalidOperationException($"WallType '{newTypeName}' không có CompoundStructure.");

        SetTotalThickness(compound, targetThickness);
        newWallType.SetCompoundStructure(compound);
        AppLog.Information("Created WallType '{0}' thickness={1}mm", newTypeName, thicknessMm);
        return newWallType;
    }

    private static void SetTotalThickness(CompoundStructure compound, double targetThickness)
    {
        if (compound.LayerCount == 1)
        {
            compound.SetLayerWidth(0, targetThickness);
            return;
        }

        IList<CompoundStructureLayer> layers = compound.GetLayers();
        int structureLayerIndex = 0;
        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i].Function == MaterialFunctionAssignment.Structure)
            {
                structureLayerIndex = i;
                break;
            }
        }

        double otherLayersWidth = 0;
        for (int i = 0; i < compound.LayerCount; i++)
        {
            if (i != structureLayerIndex)
            {
                otherLayersWidth += compound.GetLayerWidth(i);
            }
        }

        compound.SetLayerWidth(structureLayerIndex, Math.Max(targetThickness - otherLayersWidth, ThicknessTolerance));
    }

    private double GetWallHeightFromLevel(Level baseLevel)
    {
        Level? nextLevel = new FilteredElementCollector(_doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .Where(l => l.Id != baseLevel.Id && l.Elevation > baseLevel.Elevation + 1e-9)
            .OrderBy(l => l.Elevation)
            .FirstOrDefault();

        if (nextLevel != null)
        {
            return nextLevel.Elevation - baseLevel.Elevation;
        }

        return RevitUtil.convertToMeter(3600);
    }

    public List<Wall> CreateWallFromColumns(List<Element> columns, WallConfig wallConfig, Level level ) {
        var listWall = new List<Wall>() {};
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
                ElementType colTypeA = _doc.GetElement(colA.GetTypeId()) as ElementType;
                ElementType colTypeB = _doc.GetElement(colB.GetTypeId()) as ElementType;
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

              
                Wall newWall = this.CreateWall(wallConfig, wallStartPoint, wallEndPoint, level, BaseOffsetWall);
                listWall.Add(newWall); 
            }
        }

        return listWall;
    }
}
