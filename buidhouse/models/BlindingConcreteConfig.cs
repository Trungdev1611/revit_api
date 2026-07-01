namespace Simpleform.buidhouse.models;

public class BlindingConcreteConfig
{
	public string FloortypeName { get; set; } = "Bê tông lót 100mm";
	public double Thickness { get; set; } = 100/ 304.8; // 100mm to feet
	public double OffsetFromFoundationLevel { get; set; } = -100 / 304.8; // Độ rộng đua ra ngoài mép móng
	public string LevelName { get; set; }


}