using Autodesk.Revit.DB;
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
}