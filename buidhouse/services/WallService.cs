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

    public Wall CreateWall(WallConfig wallconfig, XYZ startPoint, XYZ endpoint, double BaseOffset = 0)
    {
        WallType wallType = GetOrCreateWallType(wallconfig.ThicknessWall);

        Curve line = Line.CreateBound(startPoint, endpoint);

        Level? level = new FilteredElementCollector(_doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .FirstOrDefault(x => x.Name.Equals(wallconfig.LevelName, StringComparison.OrdinalIgnoreCase));

        if (level == null)
        {
            throw new InvalidOperationException(
                $"Không tìm thấy level tương ứng: '{wallconfig.LevelName}'.");
        }

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
}
