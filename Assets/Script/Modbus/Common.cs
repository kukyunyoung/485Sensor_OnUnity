using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;

namespace MiniIMU.Public
{

	public static class Common
	{
		public static string Version_Number = "";

		public static byte bStatu = 0;

		public static byte LanguageType = 0;

		public static bool bolMode = false;

		public static byte bPlayType = 0;

		public static bool bolStartRecord = false;

		public static List<byte> RecordBytes = new List<byte>();

		//public static Main frmMain;

		//public static Sensor SensorFrm;

		public static SerialPort spSerialPort;

		public static bool[] frmCloseLabel = new bool[3];

		public static string comName = "";

		public static int intBaud = 0;

		public static SensorType ModulType;

		public static string Modul_No;

		public static byte curAddNo;

		public static bool bolStartingUpdate = false;

		public static bool bolHid = false;

		public static Icon SysIco;

		public static short GetShort(byte[] byteTemp, short sIndex)
		{
			return (short)((byteTemp[sIndex] << 8) | byteTemp[sIndex + 1]);
		}

		public static byte GetCheckSum(byte[] byteTemp, ushort usLength)
		{
			short num = 0;
			for (int i = 0; i < usLength; i++)
			{
				num += byteTemp[i];
			}
			return (byte)((uint)num & 0xFFu);
		}
	}
}