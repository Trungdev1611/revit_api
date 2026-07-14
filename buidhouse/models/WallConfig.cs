
namespace Simpleform.buidhouse.models;
public record WallConfig(
    // string WallFamilyName,// không cần thiết vì wall là systemfamily => luôn có family sẵn trong project
    double ThicknessWall , //mm
    string LevelName,
    bool IsStructural = true

);
