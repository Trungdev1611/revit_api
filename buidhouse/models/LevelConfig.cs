namespace Simpleform.buidhouse.models;

public class LevelConfig
{
    public string Name { get; set; } //tên

    public double Elevation { get; set; } //cao độ

    public LevelConfig(string name, double elevation)
    {
        Name = name;
        Elevation = elevation;
    }
}