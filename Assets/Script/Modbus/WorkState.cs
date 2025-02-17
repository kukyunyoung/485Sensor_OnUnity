namespace MiniIMU
{

	public enum WorkState
	{
		Idle,
		Search,
		Read,
		Write,
		ReadSensor,
		ReadAll,
		MagCali,
		AccCali,
		GyroCali,
		NOFF
	}
}