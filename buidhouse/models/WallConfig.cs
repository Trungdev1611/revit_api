namespace Simpleform.buidhouse.models;

public record WallConfig(
    double ThicknessWall, // mm
    bool IsStructural = true
);
