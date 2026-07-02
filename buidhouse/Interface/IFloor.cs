namespace Simpleform.buidhouse.Interface;

public interface IFloor
{
    string FloortypeName { get; }
    double Thickness { get; }
    double OffsetFromFoundationLevel { get; }
    string LevelName { get; }
}