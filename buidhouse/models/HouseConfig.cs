using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.Revit.DB;
using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.models;

/// <summary>
/// Single source of truth cho số liệu nhà. Đọc một lần (Default hoặc house.json), truyền xuống service.
/// Đơn vị kích thước / cao độ level: mm. ElementId Revit không nằm ở đây.
/// </summary>
public class HouseConfig
{
    public List<LevelConfig> Levels { get; set; } = new();

    /// <summary>Level sàn / base tường-cột (thường Level 1).</summary>
    public string BaseLevelName { get; set; } = "Level 1";

    /// <summary>Level đỉnh cột/tường và reference dầm (thường Level 2).</summary>
    public string StructuralTopLevelName { get; set; } = "Level 2";

    public ColumnSectionConfig Column { get; set; } = new();
    public WallSectionConfig Wall { get; set; } = new();
    public BeamSectionConfig Beam { get; set; } = new();
    public FloorSectionConfig Floor { get; set; } = new();
    public BlindingSectionConfig Blinding { get; set; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static HouseConfig CreateDefault() => new()
    {
        Levels = new List<LevelConfig>
        {
            new("Level 1", 0),
            new("Level 2", 3600),
            new("Level 3", 6900),
        },
        BaseLevelName = "Level 1",
        StructuralTopLevelName = "Level 2",
        Column = new ColumnSectionConfig
        {
            WidthMm = 220,
            FamilyName = "Concrete Square",
            Shape = ColumnShape.square,
            Count = 4,
        },
        Wall = new WallSectionConfig
        {
            ThicknessMm = 220,
            IsStructural = true,
        },
        Beam = new BeamSectionConfig
        {
            WidthMm = 220,
            HeightMm = 400,
            Mark = "220x400-Mark",
            Comment = "comment test1111",
        },
        Floor = new FloorSectionConfig
        {
            TypeName = "Betong san tang 1",
            ThicknessMm = 100,
            LevelName = "Level 1",
        },
        Blinding = new BlindingSectionConfig
        {
            TypeName = "Bê tông lót 100mm",
            ThicknessMm = 100,
            EdgeExtensionMm = 100,
            OffsetFromLevelMm = -150,
            LevelName = "Level 1",
        },
    };

    /// <summary>
    /// Đọc house.json cạnh DLL nếu có; lỗi / thiếu file → Default.
    /// </summary>
    public static HouseConfig LoadOrDefault(string? jsonPath = null)
    {
        string path = jsonPath ?? ResolveDefaultJsonPath();
        try
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                HouseConfig? loaded = JsonSerializer.Deserialize<HouseConfig>(json, JsonOptions);
                if (loaded != null)
                {
                    loaded.Normalize();
                    AppLog.Information("HouseConfig loaded from {0}", path);

                    AppLog.Information("HouseConfig loaded: {0}", loaded);
                    return loaded;
                }
            }
            else
            {
                AppLog.Information("HouseConfig file not found ({0}), using Default", path);
            }
        }
        catch (Exception ex)
        {
            AppLog.Warning("HouseConfig load failed ({0}), using Default: {1}", path, ex.Message);
        }

        return CreateDefault();
    }

    // json path là đường dẫn tới file house.json: 
    public static string ResolveDefaultJsonPath()
    {
        string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        return Path.Combine(dir ?? ".", "house.json");
    }

    /// <summary>Đảm bảo list/object con không null sau deserialize.</summary>
    public void Normalize()
    {
        Levels ??= new List<LevelConfig>();
        Column ??= new ColumnSectionConfig();
        Wall ??= new WallSectionConfig();
        Beam ??= new BeamSectionConfig();
        Floor ??= new FloorSectionConfig();
        Blinding ??= new BlindingSectionConfig();

        if (string.IsNullOrWhiteSpace(BaseLevelName))
            BaseLevelName = "Level 1";
        if (string.IsNullOrWhiteSpace(StructuralTopLevelName))
            StructuralTopLevelName = "Level 2";
        if (Levels.Count == 0)
            Levels = CreateDefault().Levels;
    }

    public WallConfig ToWallConfig() =>
        new(Wall.ThicknessMm, Wall.IsStructural);

    public BeamConfig ToBeamConfig() =>
        new(Beam.WidthMm, Beam.HeightMm, Beam.Mark, Beam.Comment);

    public BlindingConcreteConfig ToBlindingConfig()
    {
        return new BlindingConcreteConfig
        {
            FloortypeName = Blinding.TypeName,
            Thickness = RevitUtil.convertToMeter(Blinding.ThicknessMm),
            EdgeExtensionMm = Blinding.EdgeExtensionMm,
            offsetFromLevelTarget = RevitUtil.convertToMeter(Blinding.OffsetFromLevelMm),
            LevelName = string.IsNullOrWhiteSpace(Blinding.LevelName)
                ? BaseLevelName
                : Blinding.LevelName,
        };
    }

    public BuildHouse.Models.FloorConfig ToFloorConfig()
    {
        string levelName = string.IsNullOrWhiteSpace(Floor.LevelName)
            ? BaseLevelName
            : Floor.LevelName;
        return new BuildHouse.Models.FloorConfig(
            Floor.TypeName,
            RevitUtil.convertToMeter(Floor.ThicknessMm),
            levelName);
    }

    public ColumnConfig ToColumnConfig(ElementId baseLevelId, ElementId topLevelId) =>
        new(
            Column.SymbolName,
            baseLevelId,
            topLevelId,
            Column.BaseOffsetMm,
            Column.TopOffsetMm,
            Column.Shape,
            Column.FamilyName);
}

/// <summary>Kích thước cột (mm). SymbolName sinh từ Width×Height.</summary>
public class ColumnSectionConfig
{
    public double WidthMm { get; set; } = 220;
    /// <summary>Null hoặc = Width → cột vuông.</summary>
    public double? HeightMm { get; set; }
    public string FamilyName { get; set; } = "Concrete Square";
    public ColumnShape Shape { get; set; } = ColumnShape.square;
    public int Count { get; set; } = 4;
    public double BaseOffsetMm { get; set; }
    public double TopOffsetMm { get; set; }

    [JsonIgnore]
    public string SymbolName
    {
        get
        {
            double h = HeightMm ?? WidthMm;
            return $"{WidthMm:0}x{h:0}";
        }
    }

    [JsonIgnore]
    public double HalfWidthMm => WidthMm / 2.0;
}

public class WallSectionConfig
{
    public double ThicknessMm { get; set; } = 220;
    public bool IsStructural { get; set; } = true;
}

public class BeamSectionConfig
{
    public double WidthMm { get; set; } = 220;
    public double HeightMm { get; set; } = 400;
    public string Mark { get; set; } = "";
    public string Comment { get; set; } = "";
}

public class FloorSectionConfig
{
    public string TypeName { get; set; } = "Betong san tang 1";
    public double ThicknessMm { get; set; } = 100;
    public string LevelName { get; set; } = "Level 1";
}

public class BlindingSectionConfig
{
    public string TypeName { get; set; } = "Bê tông lót 100mm";
    public double ThicknessMm { get; set; } = 100;
    public double EdgeExtensionMm { get; set; } = 100;
    public double OffsetFromLevelMm { get; set; } = -150;
    public string LevelName { get; set; } = "Level 1";
}
