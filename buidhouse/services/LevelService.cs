using Autodesk.Revit.DB;
using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;

namespace Simpleform.buidhouse.services;

public static class LevelService
{

    public static Level CreateNewLevel(Document doc, double elevationMm, string levelName)
    {   
        Level newLevel = Level.Create(doc, RevitUtil.convertToMeter(elevationMm));
        newLevel.Name = levelName;
        return newLevel;
    }

    public static void CreateOrUpdateLevel(Document doc, List<LevelConfig> levelConfigs) {
        Dictionary<string, Level> existingLevels = new FilteredElementCollector(doc)
        .OfClass(typeof(Level))
        .Cast<Level>()
        .ToDictionary(level => level.Name, level => level);

        foreach (LevelConfig levelConfig in levelConfigs)
        {
            if(existingLevels.ContainsKey(levelConfig.Name))
            {
                existingLevels[levelConfig.Name].Elevation =  RevitUtil.convertToMeter(levelConfig.Elevation);
            } else {
                CreateNewLevel(doc, levelConfig.Elevation, levelConfig.Name);
            }
        }

    }
}