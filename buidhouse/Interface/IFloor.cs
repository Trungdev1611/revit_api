namespace Simpleform.buidhouse.Interface;

public interface IFloor
{
    string FloortypeName { get; }
    double Thickness { get; }
    double EdgeExtensionMm { get; }
    string LevelName { get; }
}
