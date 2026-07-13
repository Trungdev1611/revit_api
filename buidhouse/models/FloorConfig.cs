using Simpleform.buidhouse.Interface;

namespace BuildHouse.Models;

public class FloorConfig: IFloor {
    public string FloortypeName { get; set; }
    public double Thickness { get; set; }
    public string LevelName { get; set; }
    
    public FloorConfig(string floorTypeName, double thickness, string levelName) {
        FloortypeName = floorTypeName;
        Thickness = thickness;
        LevelName = levelName;
    }

}