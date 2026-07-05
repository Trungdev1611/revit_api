using Simpleform.buidhouse.Interface;

namespace Simpleform.buidhouse.models;

public class BlindingConcreteConfig : IFloor
{
	public string FloortypeName { get; set; } = "Bê tông lót 100mm";
	public double Thickness { get; set; } = 100 / 304.8; // độ dày sàn (feet)
	public double EdgeExtensionMm { get; set; } = 100; // đua ra ngoài mép nhà (mm)
	public string LevelName { get; set; }

	public double offsetFromLevelTarget { get; set; } = -150 / 304.8; // độ lệch so với mốc level (feet)
}
