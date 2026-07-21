namespace Simpleform.buidhouse.models;

public class LevelConfig
{
    public string Name { get; set; } = "";

    /// <summary>Cao độ (mm).</summary>
    public double Elevation { get; set; }

    public LevelConfig()
    {
    }

    public LevelConfig(string name, double elevation)
    {
        Name = name;
        Elevation = elevation;
    }
}