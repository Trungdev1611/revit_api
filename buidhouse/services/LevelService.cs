using Autodesk.Revit.DB;
using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.services;

public static class LevelService
{
    public static Level CreateNewLevel(Document doc, double elevationMm, string levelName)
    {
        Level newLevel = Level.Create(doc, RevitUtil.ConvertToFeet(elevationMm));
        newLevel.Name = levelName;
        return newLevel;
    }

    /// <summary>
    /// Tạo/cập nhật level theo config. Trả về map tên → Level để tầng trên reuse, không quét lại.
    /// </summary>
    public static Dictionary<string, Level> CreateOrUpdateLevel(Document doc, List<LevelConfig> levelConfigs)
    {
        Dictionary<string, Level> levels = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .ToDictionary(level => level.Name, level => level, StringComparer.OrdinalIgnoreCase);
        
  

        foreach (LevelConfig levelConfig in levelConfigs)
        {
            if (levels.TryGetValue(levelConfig.Name, out Level? existing))
            {
                existing.Elevation = RevitUtil.ConvertToFeet(levelConfig.Elevation);
            }
            else
            {
                levels[levelConfig.Name] = CreateNewLevel(doc, levelConfig.Elevation, levelConfig.Name);
            }
        }

      string levelsLogString = string.Join(", ", levels.Select(kv => $"{kv.Key} ({kv.Value.Elevation} ft)"));
        AppLog.Information("Levels list:: {0}", levelsLogString);
        return levels;
    }
}
