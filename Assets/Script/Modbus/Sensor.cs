using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using MiniIMU.Public;

namespace MiniIMU
{

	public class Sensor
	{
		public delegate void StatusDisplayHandler(string str);

		public delegate sbyte SendMessageHandler(byte[] byteSend);

		private delegate void CopeSensorDataHandle(byte[] byteTemp);

		private delegate void DecodeDataHandler(byte[] byteTemp, ushort usLength);

		private delegate void CopeDataHandle(byte[] byteTemp, ushort usLength);

		public SensorType Type;

		public double[] a = new double[3];

		public double[] w = new double[3];

		public double[] h = new double[3];

		public double[] Angle = new double[3];

		public double[] q = new double[4];

		public double T;

		public short[] aRaw = new short[3];

		public double[] wRaw = new double[3];

		public double[] hRaw = new double[3];

		public double[] AngleRaw = new double[3];

		public double[] aOffset = new double[3];

		public double[] wOffset = new double[3];

		public double[] hOffset = new double[3];

		public double[] hRange = new double[3];

		public double[] Port = new double[4];

		public int iBaud = 9600;

		private Modbus clsModbus;

		private byte byteReadStartIndex;

		private double PDOP;

		private double HDOP;

		private double VDOP;

		private double Temperature;

		private double Pressure;

		private double Altitude;

		private double GroundVelocity;

		private double GPSYaw;

		private double GPSHeight;

		private long Longitude;

		private long Latitude;

		private long SV;

		public double AccScale = 0.00048828125;

		public double GyroScale = 0.06103515625;

		private byte byteLastNo;

		private byte byteLastMsg;

		private double TimeElapse;

		private short sStartCnt;

		private ushort usWriteMsg;

		public short sRightPack;

		private short[] ChipTime = new short[7];

		private double[] LastAcc = new double[3];

		private double[] StaticAcc = new double[3];

		private double[] Velocity = new double[3];

		private byte[] RxBuffer = new byte[2000];

		private ushort usRxCnt;

		private ushort usTotalLength = 11;

		private byte[] byteHead = new byte[2] { 85, 170 };

		private float fMaxPeriod = 0.1f;

		private DateTime TimeStart = DateTime.Now;

		private double[] LastTime = new double[5];

		private short[] sRegData = new short[500];

		private FileStream fsAccelerate;

		private StreamWriter swAccelerate;

		private string strFileName;

		private bool bRecord;

		public WorkState State = WorkState.ReadSensor;

		private byte byteSearchDeviceNo;

		private byte byteChipID;

		private byte[] ucRxBuffer = new byte[1000];

		private byte byteReadID;

		private Queue ModbusSendQueue = new Queue();

		private int ModbusReadIndex;

		private Queue RegReadQueue = new Queue();

		private string strLastTab = "";

		public ArrayList DeviceList = new ArrayList();

		public ArrayList SelectedDevice = new ArrayList();

		private IContainer components;

		public event StatusDisplayHandler StatusDisplay;

		public event SendMessageHandler SendData;


		public sbyte WriteReg(byte Addr, short sData)
		{
			if (Addr == 2)
			{
				if ((sData & 0x800) == 2048)
				{
					sData |= 0x100;
					// strLastTab = Chart.GetTabIndex();
					// Chart.SetTabIndex("tpRaw");
					// Chart.bHex = false;
				}
				else
				{
					// if (Chart.GetTabIndex() == "tpRaw")
					// {
					// 	Chart.SetTabIndex(strLastTab);
					// }
					// Chart.bHex = true;
				}
			}
			if (Type == SensorType.Modbus)
			{
				clsModbus.ModbusWriteReg(byteChipID, Addr, sData);
				return 0;
			}
			byte[] array = new byte[5] { 255, 170, Addr, 0, 0 };
			BitConverter.GetBytes(sData).CopyTo(array, 3);
			return this.SendData(array);
		}


		public sbyte SendBytes(byte[] sBytes)
		{
			return this.SendData(sBytes);
		}

		public void FormDevelopShow()
		{
			// if (formDevelop != null && !formDevelop.IsDisposed)
			// {
			// 	formDevelop.TopMost = true;
			// 	formDevelop.TopMost = false;
			// }
			// else
			// {
			// 	formDevelop = new MiniIMU.Other.Develop();
			// 	formDevelop.Show();
			// }
		}

		public void ReadReg(byte addr)
		{
			if (Type == SensorType.Modbus)
			{
				State = WorkState.Read;
				clsModbus.ModbusReadReg(byteChipID, addr, 4);
			}
			else
			{
				RegReadQueue.Enqueue(addr);
				RegReadQueue.Enqueue(byte.MaxValue);
			}
		}

		private void timerReadReg_Tick(object sender, EventArgs e)
		{
			if (Common.bolStartingUpdate)
			{
				RegReadQueue.Clear();
			}
			else if (RegReadQueue.Count > 0)
			{
				byte b = (byte)RegReadQueue.Dequeue();
				if (b != byte.MaxValue)
				{
					byteReadStartIndex = b;
				}
				WriteReg(39, b);
			}
		}

		public sbyte SendCmd(byte byteCmd)
		{
			return SendMessage(new byte[3] { 255, 170, byteCmd });
		}

		public Sensor(SensorType tempType)
		{
			InitializeComponent();
			Type = tempType;
			// Chart.TopLevel = false;
			// Chart.FormBorderStyle = FormBorderStyle.None;
			// panel1.Controls.Add(Chart);
			// panel1.Controls[0].Dock = DockStyle.Fill;
			// Chart.Show();
			// if (Common.ModulType == SensorType.JY61 && !Common.bolMode)
			// {
			// 	Chart.DataDisplayInit(2, 4);
			// }
			// else
			// {
			// 	Chart.DataDisplayInit(3, 4);
			// }
			// Chart.WriteReg += WriteReg;
			// Chart.ReadReg += ReadReg;
			if (Common.LanguageType == 0)
			{
				if (Common.ModulType == SensorType.JY61 && !Common.bolMode)
				{
					// Chart.DataDisp.SetDiaplayLabel("时间", "系统时间:\r\n片上时间:\r\n\r\n相对时间:", 0, 0);
					// Chart.DataDisp.SetDiaplayLabel("加速度", "X:\r\nY:\r\nZ:\r\n|a|:", 0, 1);
					// Chart.DataDisp.SetDiaplayLabel("角速度", "X:\r\nY:\r\nZ:\r\n|w|:", 0, 2);
					// Chart.DataDisp.SetDiaplayLabel("角度", "X:\r\nY:\r\nZ:\r\n温度:", 0, 3);
					return;
				}
				// Chart.DataDisp.SetDiaplayLabel("时间", "系统时间:\r\n片上时间:\r\n\r\n相对时间:", 0, 0);
				// Chart.DataDisp.SetDiaplayLabel("加速度", "X:\r\nY:\r\nZ:\r\n|a|:", 0, 1);
				// Chart.DataDisp.SetDiaplayLabel("角速度", "X:\r\nY:\r\nZ:\r\n|w|:", 0, 2);
				// Chart.DataDisp.SetDiaplayLabel("磁场", "X:\r\nY:\r\nZ:\r\n|H|:", 0, 3);
				// Chart.DataDisp.SetDiaplayLabel("端口", "D0:\r\nD1:\r\nD2:\r\nD3:", 1, 0);
				// Chart.DataDisp.SetDiaplayLabel("气压", "温度:\r\n气压:\r\n高度:\r\n", 1, 1);
				// Chart.DataDisp.SetDiaplayLabel("角度", "X:\r\nY:\r\nZ:\r\nT:", 1, 2);
				// Chart.DataDisp.SetDiaplayLabel("四元数", "q0:\r\nq1:\r\nq2:\r\nq3:", 1, 3);
				// Chart.DataDisp.SetDiaplayLabel("GPS", "经度:\r\n纬度:\r\nGPS高度:\r\nGPS航向:\r\nGPS地速:", 2, 0);
				// Chart.DataDisp.SetDiaplayLabel("GPS", "卫星数:\r\n位置精度:\r\n水平精度:\r\n垂直精度", 2, 1);
			}
			else if (Common.ModulType == SensorType.JY61)
			{
				// Chart.DataDisp.SetDiaplayLabel("Time", "System:\r\nChip:\r\n\r\nRelative:", 0, 0);
				// Chart.DataDisp.SetDiaplayLabel("Acceleration", "X:\r\nY:\r\nZ:\r\nT:", 0, 1);
				// Chart.DataDisp.SetDiaplayLabel("AngleVelocity", "X:\r\nY:\r\nZ:\r\nT:", 0, 2);
				// Chart.DataDisp.SetDiaplayLabel("Angle", "X:\r\nY:\r\nZ:\r\nTemprature:", 0, 3);
			}
			else
			{
				// Chart.DataDisp.SetDiaplayLabel("Time", "System:\r\nChip:\r\n\r\nRelative:", 0, 0);
				// Chart.DataDisp.SetDiaplayLabel("Acceleration", "X:\r\nY:\r\nZ:\r\nT:", 0, 1);
				// Chart.DataDisp.SetDiaplayLabel("AngleVelocity", "X:\r\nY:\r\nZ:\r\nT:", 0, 2);
				// Chart.DataDisp.SetDiaplayLabel("Magnitude", "X:\r\nY:\r\nZ:\r\n|H|:", 0, 3);
				// Chart.DataDisp.SetDiaplayLabel("Port", "D0:\r\nD1:\r\nD2:\r\nD3:", 1, 0);
				// Chart.DataDisp.SetDiaplayLabel("Pressure", "Temprature:\r\nPressure:\r\nHeight:\r\n", 1, 1);
				// Chart.DataDisp.SetDiaplayLabel("Angle", "X:\r\nY:\r\nZ:\r\nT:", 1, 2);
				// Chart.DataDisp.SetDiaplayLabel("q", "q0:\r\nq1:\r\nq2:\r\nq3:", 1, 3);
				// Chart.DataDisp.SetDiaplayLabel("GPS", "Longitude:\r\nLatitude:\r\nGPS H:\r\nGPS Yaw:\r\nGPS V:", 2, 0);
				// Chart.DataDisp.SetDiaplayLabel("GPS", "Satellite Num:\r\nPDOP:\r\nHDOP:\r\nVDOP", 2, 1);
			}
		}

		private void DisplayRefresh(object sender, EventArgs e)
		{
			// Chart.InstrumentRefresh(Angle);
			// if (!(Chart.GetTabIndex() != "tpData"))
			// {
			// 	if (Common.ModulType == SensorType.JY61 && !Common.bolMode)
			// 	{
			// 		Chart.DataDisp.SetDiaplayText(DateTime.Now.ToLongTimeString() + "\r\n" + ChipTime[0] + "-" + ChipTime[1] + "-" + ChipTime[2] + "\r\n" + ChipTime[3] + ":" + ChipTime[4] + ":" + ChipTime[5] + "." + ChipTime[6] + "\r\n" + TimeElapse.ToString("f3"), 0, 0);
			// 		Chart.DataDisp.SetDiaplayText(a[0].ToString("f4") + " g\r\n" + a[1].ToString("f4") + " g\r\n" + a[2].ToString("f4") + " g\r\n" + Math.Sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]).ToString("f4") + " g", 0, 1);
			// 		Chart.DataDisp.SetDiaplayText(w[0].ToString("f4") + " °/s\r\n" + w[1].ToString("f4") + " °/s\r\n" + w[2].ToString("f4") + " °/s\r\n" + Math.Sqrt(w[0] * w[0] + w[1] * w[1] + w[2] * w[2]).ToString("f4") + " °/s", 0, 2);
			// 		Chart.DataDisp.SetDiaplayText(Angle[0].ToString("f3") + " °\r\n" + Angle[1].ToString("f3") + " °\r\n" + Angle[2].ToString("f3") + " °\r\n" + Temperature.ToString("f2") + " ℃", 0, 3);
			// 		return;
			// 	}
			// 	Chart.DataDisp.SetDiaplayText(DateTime.Now.ToLongTimeString() + "\r\n" + ChipTime[0] + "-" + ChipTime[1] + "-" + ChipTime[2] + "\r\n" + ChipTime[3] + ":" + ChipTime[4] + ":" + ChipTime[5] + "." + ChipTime[6] + "\r\n" + TimeElapse.ToString("f3"), 0, 0);
			// 	Chart.DataDisp.SetDiaplayText(a[0].ToString("f4") + " g\r\n" + a[1].ToString("f4") + " g\r\n" + a[2].ToString("f4") + " g\r\n" + Math.Sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]).ToString("f4") + " g", 0, 1);
			// 	Chart.DataDisp.SetDiaplayText(w[0].ToString("f4") + " °/s\r\n" + w[1].ToString("f4") + " °/s\r\n" + w[2].ToString("f4") + " °/s\r\n" + Math.Sqrt(w[0] * w[0] + w[1] * w[1] + w[2] * w[2]).ToString("f4") + " °/s", 0, 2);
			// 	Chart.DataDisp.SetDiaplayText(h[0].ToString("f0") + "\r\n" + h[1].ToString("f0") + "\r\n" + h[2].ToString("f0") + "\r\n" + Math.Sqrt(h[0] * h[0] + h[1] * h[1] + h[2] * h[2]).ToString("f0"), 0, 3);
			// 	Chart.DataDisp.SetDiaplayText(Port[0].ToString("f0") + "\r\n" + Port[1].ToString("f0") + "\r\n" + Port[2].ToString("f0") + "\r\n" + Port[3].ToString("f0"), 1, 0);
			// 	Chart.DataDisp.SetDiaplayText(Temperature.ToString("f2") + " ℃\r\n" + Pressure.ToString("f0") + " Pa\r\n" + Altitude.ToString("f2") + " m\r\n", 1, 1);
			// 	Chart.DataDisp.SetDiaplayText(Angle[0].ToString("f3") + " °\r\n" + Angle[1].ToString("f3") + " °\r\n" + Angle[2].ToString("f3") + " °\r\n" + Temperature.ToString("f2") + " ℃", 1, 2);
			// 	Chart.DataDisp.SetDiaplayText(q[0].ToString("f5") + "\r\n" + q[1].ToString("f5") + "\r\n" + q[2].ToString("f5") + "\r\n" + q[3].ToString("f5"), 1, 3);
			// 	Chart.DataDisp.SetDiaplayText((Longitude / 10000000).ToString("f0") + "°" + ((double)(Longitude % 10000000) / 100000.0).ToString("f5") + "'\r\n" + (Latitude / 10000000).ToString("f0") + "°" + ((double)(Latitude % 10000000) / 100000.0).ToString("f5") + "'\r\n" + GPSHeight.ToString("f1") + " m\r\n" + GPSYaw.ToString("f1") + " °\r\n" + GroundVelocity.ToString("f3") + " km/h", 2, 0);
			// 	Chart.DataDisp.SetDiaplayText(SV + "\r\n" + PDOP.ToString("f2") + "\r\n" + HDOP.ToString("f2") + "\r\n" + VDOP.ToString("f2"), 2, 1);
			// }
		}

		private double[] QuatToInc(double[] q)
		{
			return new double[3]
			{
			Math.Asin(2.0 * (q[3] * q[2] + q[0] * q[1])) / Math.PI * 180.0,
			Math.Asin(2.0 * (q[0] * q[2] - q[1] * q[3])) / Math.PI * 180.0,
			Math.Atan2(2.0 * (q[1] * q[2] + q[0] * q[3]), q[0] * q[0] + q[1] * q[1] - q[2] * q[2] - q[3] * q[3]) / Math.PI * 180.0
			};
		}

		private void DataRecord(byte byteNo, byte ADDR)
		{
			if (!bRecord)
			{
				return;
			}
			if (fsAccelerate.CanWrite)
			{
				if (byteLastNo >= byteNo)
				{
					if (sStartCnt < 5)
					{
						sStartCnt++;
					}
					if (sStartCnt == 4)
					{
						swAccelerate.WriteLine("StartTime: " + TimeStart.ToString("yyyy-MM-dd ") + TimeStart.ToString("HH:mm:ss.fff"));
						swAccelerate.Write("address");
						swAccelerate.Write("\tTime(s)");
						if ((usWriteMsg & 1) > 0)
						{
							byteLastMsg = 80;
							swAccelerate.Write("\tChipTime");
						}
						if ((usWriteMsg & 2) > 0)
						{
							byteLastMsg = 81;
							swAccelerate.Write("\tax(g)\tay(g)\taz(g)");
						}
						if ((usWriteMsg & 4) > 0)
						{
							byteLastMsg = 82;
							swAccelerate.Write("\twx(deg/s)\twy(deg/s)\twz(deg/s)");
						}
						if ((usWriteMsg & 8) > 0)
						{
							byteLastMsg = 83;
							swAccelerate.Write("\tAngleX(deg)\tAngleY(deg)\tAngleZ(deg)\tT(°)");
						}
						if ((usWriteMsg & 0x10) > 0)
						{
							byteLastMsg = 84;
							swAccelerate.Write("\thx\thy\thz");
						}
						if ((usWriteMsg & 0x20) > 0)
						{
							byteLastMsg = 85;
							swAccelerate.Write("\tD0\tD1\tD2\tD3");
						}
						if ((usWriteMsg & 0x40) > 0)
						{
							byteLastMsg = 86;
							swAccelerate.Write("\tPressure(Pa)\tAltitude(m)");
						}
						if ((usWriteMsg & 0x80) > 0)
						{
							byteLastMsg = 87;
							swAccelerate.Write("\tLon(deg)\tLat(deg)");
						}
						if ((usWriteMsg & 0x100) > 0)
						{
							byteLastMsg = 88;
							swAccelerate.Write("\tGPSHeight(m)\tGPSYaw(deg)\tGPSV(km/h)");
						}
						if ((usWriteMsg & 0x200) > 0)
						{
							byteLastMsg = 89;
							swAccelerate.Write("\tq0\tq1\tq2\tq3");
						}
						if ((usWriteMsg & 0x400) > 0)
						{
							byteLastMsg = 90;
							swAccelerate.Write("\tSV\tPDOP\tHDOP\tVDOP");
						}
					}
					if (sStartCnt > 4)
					{
						swAccelerate.WriteLine(" ");
						TimeElapse = (DateTime.Now - TimeStart).TotalMilliseconds / 1000.0;
						swAccelerate.Write("0x" + ADDR.ToString("X2"));
						swAccelerate.Write("\t" + DateTime.Now.ToString("HH:mm:ss.fff"));
					}
				}
				if (sStartCnt < 4)
				{
					usWriteMsg |= (ushort)(1 << (byteNo & 0xF));
				}
				if ((sStartCnt > 4) & (byteLastMsg == byteNo))
				{
					if ((usWriteMsg & 1) > 0)
					{
						swAccelerate.Write("\t" + DateTime.Now.ToString("HH:mm:ss.fff"));
					}
					if ((usWriteMsg & 2) > 0)
					{
						swAccelerate.Write("\t" + a[0].ToString("f4") + "\t" + a[1].ToString("f4") + "\t" + a[2].ToString("f4"));
					}
					if ((usWriteMsg & 4) > 0)
					{
						swAccelerate.Write("\t" + w[0].ToString("f4") + "\t" + w[1].ToString("f4") + "\t" + w[2].ToString("f4"));
					}
					if ((usWriteMsg & 8) > 0)
					{
						swAccelerate.Write("\t" + Angle[0].ToString("f4") + "\t" + Angle[1].ToString("f4") + "\t" + Angle[2].ToString("f4") + "\t" + Temperature.ToString("f4"));
					}
					if ((usWriteMsg & 0x10) > 0)
					{
						swAccelerate.Write("\t" + h[0].ToString("f0") + "\t" + h[1].ToString("f0") + "\t" + h[2].ToString("f0"));
					}
					if ((usWriteMsg & 0x20) > 0)
					{
						swAccelerate.Write("\t" + Port[0].ToString("f0") + "\t" + Port[1].ToString("f0") + "\t" + Port[2].ToString("f0") + "\t" + Port[3].ToString("f0"));
					}
					if ((usWriteMsg & 0x40) > 0)
					{
						swAccelerate.Write("\t" + Pressure.ToString("f0") + "\t" + Altitude.ToString("f2"));
					}
					if ((usWriteMsg & 0x80) > 0)
					{
						swAccelerate.Write("\t" + ((double)(Longitude / 10000000) + (double)(Longitude % 10000000) / 100000.0 / 60.0).ToString("f8") + "\t" + ((double)(Latitude / 10000000) + (double)(Latitude % 10000000) / 100000.0 / 60.0).ToString("f8"));
					}
					if ((usWriteMsg & 0x100) > 0)
					{
						swAccelerate.Write("\t" + GPSHeight.ToString("f1") + "\t" + GPSYaw.ToString("f1") + "\t" + GroundVelocity.ToString("f3"));
					}
					if ((usWriteMsg & 0x200) > 0)
					{
						swAccelerate.Write("\t" + q[0].ToString("f5") + "\t" + q[1].ToString("f5") + "\t" + q[2].ToString("f5") + "\t" + q[3].ToString("f5"));
					}
					if ((usWriteMsg & 0x400) > 0)
					{
						swAccelerate.Write("\t" + SV + "\t" + PDOP.ToString("f2") + "\t" + HDOP.ToString("f2") + "\t" + VDOP.ToString("f2"));
					}
				}
			}
			else
			{
				sStartCnt = 0;
				usWriteMsg = 0;
			}
			byteLastNo = byteNo;
		}

		private bool CheckHead(byte[] byteData, byte[] byteHeadTemp, int byteHeadLength)
		{
			for (byte b = 0; b < byteHeadLength; b++)
			{
				if (byteData[b] != byteHeadTemp[b])
				{
					return false;
				}
			}
			return true;
		}

		private bool CheckHead(byte[] byteIn, byte[] byteTemplet, ushort usLength)
		{
			for (int i = 0; i < usLength; i++)
			{
				if (byteIn[i] != byteTemplet[i])
				{
					return false;
				}
			}
			return true;
		}

		private byte GetSum(byte[] byteTemp, ushort usLength)
		{
			short num = 0;
			for (int i = 0; i < usLength; i++)
			{
				num += byteTemp[i];
			}
			return (byte)((uint)num & 0xFFu);
		}

		private void ByteCopy(byte[] byteFrom, byte[] byteTo, ushort usFromIndex, ushort usToIndex, ushort usLength)
		{
			for (int i = 0; i < usLength; i++)
			{
				byteTo[usToIndex + i] = byteFrom[usFromIndex + i];
			}
		}

		private void UpdateReg(byte addr, short sData, byte ADDR)
		{
			// if (formConfig != null && !Common.frmCloseLabel[1])
			// {
			// 	formConfig.UpdateReg(addr, sData);
			// }
			// if (Chart != null)
			// {
			// 	Chart.UpdateReg(addr, sData);
			// }
			// if (formFactoryTest != null)
			// {
			// 	formFactoryTest.UpdateReg(addr, sData);
			// }
			sRegData[addr] = sData;
			switch ((Reg)addr)
			{
				case Reg.YYMM:
					ChipTime[0] = (short)(2000 + (sData & 0xFF));
					ChipTime[1] = (short)((sData >> 8) & 0xFF);
					break;
				case Reg.DDHH:
					ChipTime[2] = (short)(sData & 0xFF);
					ChipTime[3] = (short)((sData >> 8) & 0xFF);
					break;
				case Reg.MMSS:
					ChipTime[4] = (short)(sData & 0xFF);
					ChipTime[5] = (short)((sData >> 8) & 0xFF);
					break;
				case Reg.MS:
					ChipTime[6] = sData;
					DataRecord(80, ADDR);
					break;
				case Reg.AX:
					a[0] = (double)sData * AccScale;
					break;
				case Reg.AY:
					a[1] = (double)sData * AccScale;
					break;
				case Reg.AZ:
					a[2] = (double)sData * AccScale;
					DataRecord(81, ADDR);
					break;
				case Reg.GX:
					w[0] = (double)sData * GyroScale;
					break;
				case Reg.GY:
					w[1] = (double)sData * GyroScale;
					break;
				case Reg.GZ:
					w[2] = (double)sData * GyroScale;
					DataRecord(82, ADDR);
					break;
				case Reg.HX:
					h[0] = sData;
					break;
				case Reg.HY:
					h[1] = sData;
					break;
				case Reg.HZ:
					h[2] = sData;
					DataRecord(84, ADDR);
					break;
				case Reg.Roll:
					Angle[0] = (double)sData / 32768.0 * 180.0;
					break;
				case Reg.Pitch:
					Angle[1] = (double)sData / 32768.0 * 180.0;
					break;
				case Reg.Yaw:
					Angle[2] = (double)sData / 32768.0 * 180.0;
					DataRecord(83, ADDR);
					break;
				case Reg.TEMP:
					if (Type == SensorType.JY61)
					{
						Temperature = (double)sData / 32768.0 * 96.38 + 36.53;
					}
					else
					{
						Temperature = (double)sData / 100.0;
					}
					break;
				case Reg.D0Status:
					Port[0] = sData;
					break;
				case Reg.D1Status:
					Port[1] = sData;
					break;
				case Reg.D2Status:
					Port[2] = sData;
					break;
				case Reg.D3Status:
					Port[3] = sData;
					DataRecord(85, ADDR);
					break;
				case Reg.PressureH:
					{
						byte[] bytes = BitConverter.GetBytes(sRegData[69]);
						byte[] bytes2 = BitConverter.GetBytes(sRegData[70]);
						byte[] value = bytes.Concat(bytes2).ToArray();
						Pressure = BitConverter.ToInt32(value, 0);
						break;
					}
				case Reg.HeightH:
					{
						byte[] bytes = BitConverter.GetBytes(sRegData[71]);
						byte[] bytes2 = BitConverter.GetBytes(sRegData[72]);
						byte[] value = bytes.Concat(bytes2).ToArray();
						Altitude = (double)BitConverter.ToInt32(value, 0) / 100.0;
						DataRecord(86, ADDR);
						break;
					}
				case Reg.LonH:
					{
						byte[] bytes = BitConverter.GetBytes(sRegData[73]);
						byte[] bytes2 = BitConverter.GetBytes(sRegData[74]);
						byte[] value = bytes.Concat(bytes2).ToArray();
						Longitude = BitConverter.ToInt32(value, 0);
						break;
					}
				case Reg.LatH:
					{
						byte[] bytes = BitConverter.GetBytes(sRegData[75]);
						byte[] bytes2 = BitConverter.GetBytes(sRegData[76]);
						byte[] value = bytes.Concat(bytes2).ToArray();
						Latitude = BitConverter.ToInt32(value, 0);
						DataRecord(87, ADDR);
						break;
					}
				case Reg.GPSHeight:
					GPSHeight = (double)sData / 10.0;
					break;
				case Reg.GPSYAW:
					GPSYaw = (double)sData / 100.0;
					break;
				case Reg.GPSVH:
					{
						byte[] bytes = BitConverter.GetBytes(sRegData[79]);
						byte[] bytes2 = BitConverter.GetBytes(sRegData[80]);
						byte[] value = bytes.Concat(bytes2).ToArray();
						GroundVelocity = (double)BitConverter.ToInt32(value, 0) / 1000.0;
						DataRecord(88, ADDR);
						break;
					}
				case Reg.q0:
					q[0] = (double)sData / 32768.0;
					break;
				case Reg.q1:
					q[1] = (double)sData / 32768.0;
					break;
				case Reg.q2:
					q[2] = (double)sData / 32768.0;
					break;
				case Reg.q3:
					q[3] = (double)sData / 32768.0;
					DataRecord(89, ADDR);
					break;
				case Reg.SVNUM:
					SV = sData;
					break;
				case Reg.PDOP:
					PDOP = (double)sData / 100.0;
					break;
				case Reg.HDOP:
					HDOP = (double)sData / 100.0;
					break;
				case Reg.VDOP:
					VDOP = (double)sData / 100.0;
					DataRecord(90, ADDR);
					break;
				case Reg.VERSION:
					// if (formAbout != null)
					// {
					// 	formAbout.SetVersion(sData.ToString());
					// }
					break;
				case Reg.RSV8:
				case Reg.PressureL:
				case Reg.HeightL:
				case Reg.LonL:
				case Reg.LatL:
				case Reg.GPSVL:
					break;
			}
		}

		private void CopeNormalData(byte[] byteTemp, ushort usLength)
		{
			// Chart.RawDataDisplay(byteTemp, usLength);
			ByteCopy(byteTemp, RxBuffer, 0, usRxCnt, usLength);
			usRxCnt += usLength;
			while (usRxCnt >= usTotalLength)
			{
				if (!CheckHead(RxBuffer, new byte[1] { 85 }, 1))
				{
					ByteCopy(RxBuffer, RxBuffer, 1, 0, usRxCnt);
					usRxCnt--;
					continue;
				}
				if (GetSum(RxBuffer, (ushort)(usTotalLength - 1)) == RxBuffer[usTotalLength - 1])
				{
					CopeSensorData(RxBuffer);
				}
				ByteCopy(RxBuffer, RxBuffer, usTotalLength, 0, (ushort)(usRxCnt - usTotalLength));
				usRxCnt -= usTotalLength;
			}
		}

		private void CopeSensorData(byte[] byteTemp)
		{
			if (Common.bolStartRecord && byteTemp[1] != 95)
			{
				Common.RecordBytes.AddRange(byteTemp);
			}
			TimeElapse = (DateTime.Now - TimeStart).TotalMilliseconds / 1000.0;
			switch (byteTemp[1])
			{
				case 80:
					UpdateReg(48, BitConverter.ToInt16(byteTemp, 2), 80);
					UpdateReg(49, BitConverter.ToInt16(byteTemp, 4), 80);
					UpdateReg(50, BitConverter.ToInt16(byteTemp, 6), 80);
					UpdateReg(51, BitConverter.ToInt16(byteTemp, 8), 80);
					break;
				case 81:
					UpdateReg(52, BitConverter.ToInt16(byteTemp, 2), 80);
					UpdateReg(53, BitConverter.ToInt16(byteTemp, 4), 80);
					UpdateReg(54, BitConverter.ToInt16(byteTemp, 6), 80);
					UpdateReg(64, BitConverter.ToInt16(byteTemp, 8), 80);
					// if (Chart.GetTabIndex() == "tpAcc")
					// {
					// 	Chart.DataUpdate(0, TimeElapse, a);
					// }
					break;
				case 82:
					UpdateReg(55, BitConverter.ToInt16(byteTemp, 2), 80);
					UpdateReg(56, BitConverter.ToInt16(byteTemp, 4), 80);
					UpdateReg(57, BitConverter.ToInt16(byteTemp, 6), 80);
					// if (Chart.GetTabIndex() == "tpGyro")
					// {
					// 	Chart.DataUpdate(1, TimeElapse, w);
					// }
					break;
				case 83:
					UpdateReg(61, BitConverter.ToInt16(byteTemp, 2), 80);
					UpdateReg(62, BitConverter.ToInt16(byteTemp, 4), 80);
					UpdateReg(63, BitConverter.ToInt16(byteTemp, 6), 80);
					Common.Version_Number = BitConverter.ToInt16(byteTemp, 8).ToString();
					// if (Form3D != null)
					// {
					// 	Form3D.Refresh3D(Angle[0], Angle[1], Angle[2]);
					// }
					// if (Chart.GetTabIndex() == "tpAngle")
					// {
					// 	Chart.DataUpdate(2, TimeElapse, Angle);
					// }
					break;
				case 84:
					UpdateReg(58, BitConverter.ToInt16(byteTemp, 2), 80);
					UpdateReg(59, BitConverter.ToInt16(byteTemp, 4), 80);
					UpdateReg(60, BitConverter.ToInt16(byteTemp, 6), 80);
					// if (Chart.GetTabIndex() == "tpMag")
					// {
					// 	Chart.DataUpdate(3, TimeElapse, h);
					// }
					break;
				case 85:
					UpdateReg(65, BitConverter.ToInt16(byteTemp, 2), 80);
					UpdateReg(66, BitConverter.ToInt16(byteTemp, 4), 80);
					UpdateReg(67, BitConverter.ToInt16(byteTemp, 6), 80);
					UpdateReg(68, BitConverter.ToInt16(byteTemp, 8), 80);
					break;
				case 86:
					UpdateReg(69, BitConverter.ToInt16(byteTemp, 2), 80);
					UpdateReg(70, BitConverter.ToInt16(byteTemp, 4), 80);
					UpdateReg(71, BitConverter.ToInt16(byteTemp, 6), 80);
					UpdateReg(72, BitConverter.ToInt16(byteTemp, 8), 80);
					break;
				case 87:
					UpdateReg(73, BitConverter.ToInt16(byteTemp, 2), 80);
					UpdateReg(74, BitConverter.ToInt16(byteTemp, 4), 80);
					UpdateReg(75, BitConverter.ToInt16(byteTemp, 6), 80);
					UpdateReg(76, BitConverter.ToInt16(byteTemp, 8), 80);
					break;
				case 88:
					UpdateReg(77, BitConverter.ToInt16(byteTemp, 2), 80);
					UpdateReg(78, BitConverter.ToInt16(byteTemp, 4), 80);
					UpdateReg(79, BitConverter.ToInt16(byteTemp, 6), 80);
					UpdateReg(80, BitConverter.ToInt16(byteTemp, 8), 80);
					break;
				case 89:
					UpdateReg(81, BitConverter.ToInt16(byteTemp, 2), 80);
					UpdateReg(82, BitConverter.ToInt16(byteTemp, 4), 80);
					UpdateReg(83, BitConverter.ToInt16(byteTemp, 6), 80);
					UpdateReg(84, BitConverter.ToInt16(byteTemp, 8), 80);
					break;
				case 90:
					UpdateReg(85, BitConverter.ToInt16(byteTemp, 2), 80);
					UpdateReg(86, BitConverter.ToInt16(byteTemp, 4), 80);
					UpdateReg(87, BitConverter.ToInt16(byteTemp, 6), 80);
					UpdateReg(88, BitConverter.ToInt16(byteTemp, 8), 80);
					break;
				case 95:
					UpdateReg(byteReadStartIndex, BitConverter.ToInt16(byteTemp, 2), 80);
					UpdateReg((byte)(byteReadStartIndex + 1), BitConverter.ToInt16(byteTemp, 4), 80);
					UpdateReg((byte)(byteReadStartIndex + 2), BitConverter.ToInt16(byteTemp, 6), 80);
					UpdateReg((byte)(byteReadStartIndex + 3), BitConverter.ToInt16(byteTemp, 8), 80);
					Console.WriteLine("kkkkkk");
					break;
				case 91:
				case 92:
				case 93:
				case 94:
					break;
			}
		}

		private void CopeSensorDataBle(byte[] byteTemp)
		{
			TimeElapse = (DateTime.Now - TimeStart).TotalMilliseconds / 1000.0;
			UpdateReg(52, BitConverter.ToInt16(byteTemp, 2), 80);
			UpdateReg(53, BitConverter.ToInt16(byteTemp, 4), 80);
			UpdateReg(54, BitConverter.ToInt16(byteTemp, 6), 80);
			// if (Chart.GetTabIndex() == "tpAcc")
			// {
			// 	Chart.DataUpdate(0, TimeElapse, a);
			// }
			UpdateReg(55, BitConverter.ToInt16(byteTemp, 8), 80);
			UpdateReg(56, BitConverter.ToInt16(byteTemp, 10), 80);
			UpdateReg(57, BitConverter.ToInt16(byteTemp, 12), 80);
			// if (Chart.GetTabIndex() == "tpGyro")
			// {
			// 	Chart.DataUpdate(1, TimeElapse, w);
			// }
			UpdateReg(61, BitConverter.ToInt16(byteTemp, 14), 80);
			UpdateReg(62, BitConverter.ToInt16(byteTemp, 16), 80);
			UpdateReg(63, BitConverter.ToInt16(byteTemp, 18), 80);
			// if (Form3D != null)
			// {
			// 	Form3D.Refresh3D(Angle[0], Angle[1], Angle[2]);
			// }
			// if (Chart.GetTabIndex() == "tpAngle")
			// {
			// 	Chart.DataUpdate(2, TimeElapse, Angle);
			// }
			if (byteTemp[1] == 97 && byteTemp.Length == 28)
			{
				UpdateReg(48, BitConverter.ToInt16(byteTemp, 20), 80);
				UpdateReg(49, BitConverter.ToInt16(byteTemp, 22), 80);
				UpdateReg(50, BitConverter.ToInt16(byteTemp, 24), 80);
				UpdateReg(51, BitConverter.ToInt16(byteTemp, 26), 80);
			}
		}

		public void DecodeData(byte[] byteTemp, ushort usLength)
		{
			// if (base.Controls[0].InvokeRequired)
			// {
			// 	try
			// 	{
			// 		Invoke(new DecodeDataHandler(DecodeData), byteTemp, usLength);
			// 		return;
			// 	}
			// 	catch (Exception)
			// 	{
			// 		return;
			// 	}
			// }
			if (Common.bPlayType == 1)
			{
				byteReadStartIndex = 0;
				// Chart.RawDataDisplay(byteTemp, usLength);
				CopeSensorDataBle(byteTemp);
				return;
			}
			if (Common.bPlayType == 2)
			{
				byteReadStartIndex = 0;
				// Chart.RawDataDisplay(byteTemp, usLength);
				CopeSensorData(byteTemp);
				return;
			}
			if (Common.bPlayType == 3)
			{
				byteReadStartIndex = 0;
				// Chart.RawDataDisplay(byteTemp, usLength);
				AnalyseModbusMsg(byteTemp);
				return;
			}
			// if (formDevelop != null && !Common.frmCloseLabel[0])
			// {
			// 	formDevelop.UpdateReg(byteTemp, usLength);
			// }
			if (Type == SensorType.Modbus)
			{
				CopeModbusData(byteTemp, usLength);
			}
			else
			{
				CopeNormalData(byteTemp, usLength);
			}
		}

		public void DataRecord(bool bRecordFlag)
		{
			bRecord = bRecordFlag;
			if (bRecord)
			{
				sStartCnt = 0;
				usWriteMsg = 0;
				fsAccelerate = new FileStream("Data.txt", FileMode.Create);
				strFileName = "Data" + DateTime.Now.ToString("yyMMddhhmmss") + ".txt";
				fsAccelerate = new FileStream(strFileName, FileMode.Create);
				swAccelerate = new StreamWriter(fsAccelerate);
			}
			else if (fsAccelerate != null)
			{
				swAccelerate.Flush();
				fsAccelerate.Close();
				// if (MessageBox.Show("Open the recorded file？", "Infomation", MessageBoxButtons.OKCancel) == DialogResult.OK)
				// {
				// 	Process.Start("notepad.exe", strFileName);
				// }
			}
		}

		private void tabSensor_DoubleClick(object sender, EventArgs e)
		{
			FormDevelopShow();
		}

		private void Sensor_Load(object sender, EventArgs e)
		{
			// base.Icon = Common.SysIco;
			clsModbus = new Modbus();
			clsModbus.WriteMessage += SendMessage;
			// Common.SensorFrm = this;
		}

		public void SearchModbusDevice()
		{
			// timerModbus.Enabled = true;
			State = WorkState.Search;
			byteSearchDeviceNo = 0;
		}

		private void ResetRxBuffer()
		{
		}

		private void ReadSensor(byte ucChipID, byte ucRegID, byte byteLength)
		{
			ResetRxBuffer();
			clsModbus.ModbusReadReg(ucChipID, ucRegID, byteLength);
		}

		private short GetShort(byte[] byteTemp, short sIndex)
		{
			Console.WriteLine("{0}={1},{2},{3}", sIndex, byteTemp[sIndex], byteTemp[sIndex + 1], (byteTemp[sIndex] << 8) | byteTemp[sIndex + 1]);
			return (short)((byteTemp[sIndex] << 8) | byteTemp[sIndex + 1]);
		}

		private void AnalyseModbusMsg(byte[] Buffer)
		{
			// UnityEngine.Debug.Log("******************" + byteReadStartIndex);
			if (Buffer.Length > 12)
			{
				string text = "";
				for (int i = 0; i < 13; i++)
				{
					text = text + Buffer[i].ToString("X2") + " ";
				}
				UnityEngine.Debug.Log("반환=" + text);
			}
			if (State != WorkState.Search)
			{
				SendModbusQueue(null, null);
			}
			if (Common.bolStartRecord && byteReadStartIndex == 0)
			{
				Common.RecordBytes.AddRange(Buffer);
			}
			for (int j = 0; j < Buffer[2] / 2; j++)
			{
				UpdateReg((byte)(byteReadStartIndex + j), GetShort(Buffer, (short)(3 + j * 2)), Buffer[0]);
			}
			if ((byteReadStartIndex == 0) & (Buffer[2] == 2))
			{
				DeviceList.Add("0x" + Buffer[0].ToString("X2"));
			}
			// if (Form3D != null)
			// {
			// 	Form3D.Refresh3D(Angle[0], Angle[1], Angle[2]);
			// }
			// if (SelectedDevice.Count > 1)
			// {
			// 	Chart.TextDisplay("0x" + Buffer[0].ToString("X2") + "\tAngleX:" + Angle[0].ToString("f3") + "\tAngleY:" + Angle[1].ToString("f3") + "\tAngleZ:" + Angle[2].ToString("f3") + "\r\n");
			// }
			// else
			// {
			// 	Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "   ");
			// 	Chart.TextDisplay(DateTime.Now.ToString("hh:mm:ss.fff") + " 0x" + Buffer[0].ToString("X2") + " " + a[0].ToString("f3") + "\t" + a[1].ToString("f3") + "\t" + a[2].ToString("f3") + "\t" + w[0].ToString("f3") + "\t" + w[1].ToString("f3") + "\t" + w[2].ToString("f3") + "\t" + h[0].ToString("f2") + "\t" + h[1].ToString("f2") + "\t" + h[2].ToString("f2") + "\t" + Angle[0].ToString("f3") + "\t" + Angle[1].ToString("f3") + "\t" + Angle[2].ToString("f3") + "\t" + ((double)(float)GetShort(Buffer, 29) / 100.0).ToString("f1") + "\r\n");
			// }
			TimeElapse = (DateTime.Now - TimeStart).TotalMilliseconds / 1000.0;
			if (TimeElapse - LastTime[0] < (double)fMaxPeriod)
			{
				return;
			}
			LastTime[0] = TimeElapse;
			if (byteChipID == Buffer[0])
			{
				// 	if (Chart.GetTabIndex() == "tpAcc")
				// 	{
				// 		Chart.DataUpdate(0, TimeElapse, a);
				// 	}
				// 	if (Chart.GetTabIndex() == "tpGyro")
				// 	{
				// 		Chart.DataUpdate(1, TimeElapse, w);
				// 	}
				// 	if (Chart.GetTabIndex() == "tpAngle")
				// 	{
				// 		Chart.DataUpdate(2, TimeElapse, Angle);
				// 	}
				// 	if (Chart.GetTabIndex() == "tpMag")
				// 	{
				// 		Chart.DataUpdate(3, TimeElapse, h);
				// 	}
				// 	Chart.InstrumentRefresh(Angle);
			}
		}

		private void CopeModbusData(byte[] byteTemp, ushort usLength)
		{
			ByteCopy(byteTemp, RxBuffer, 0, usRxCnt, usLength);
			usRxCnt += usLength;
			try
			{
				while (usRxCnt >= 7)
				{
					if (usRxCnt > 200)
					{
						usRxCnt = 0;
						break;
					}
					if (RxBuffer[1] == 6)
					{
						if (usRxCnt < 8)
						{
							break;
						}
						usRxCnt = 0;
					}
					else if (RxBuffer[1] == 3)
					{
						if (RxBuffer[2] < 0)
						{
							usRxCnt = 0;
							break;
						}
						if (usRxCnt < RxBuffer[2] + 5)
						{
							break;
						}
						if (clsModbus.ModbusMsgCheck(RxBuffer))
						{
							AnalyseModbusMsg(RxBuffer);
							ByteCopy(RxBuffer, RxBuffer, (ushort)(RxBuffer[2] + 5), 0, (ushort)(usRxCnt - RxBuffer[2] - 5));
							usRxCnt -= (ushort)(RxBuffer[2] + 5);
						}
						else
						{
							ByteCopy(RxBuffer, RxBuffer, 1, 0, (ushort)(usRxCnt - 1));
							usRxCnt--;
						}
					}
					else
					{
						ByteCopy(RxBuffer, RxBuffer, 1, 0, (ushort)(usRxCnt - 1));
						usRxCnt--;
					}
				}
			}
			catch (Exception e)
			{
				UnityEngine.Debug.Log(e);
			}
		}

		private void SendModbusQueue(object sender, EventArgs e)
		{
			try
			{
				if (Common.bolStartingUpdate)
				{
					ModbusSendQueue.Clear();
					return;
				}
				// sendQueueTimer.Interval = 1000000 / iBaud;
				// sendQueueTimer.Stop();
				if (ModbusSendQueue.Count > 0)
				{
					byte[] array = (byte[])ModbusSendQueue.Dequeue();
					this.SendData(array);
					string text = "";
					for (int i = 0; i < array.Length; i++)
					{
						text = text + array[i].ToString("X2") + " ";
					}
					Console.WriteLine("地址={0},指令={1}", array[0].ToString("X2"), text);
					byteReadID = array[0];
					if (array[1] == 3)
					{
						byteReadStartIndex = array[3];
					}
				}
				// sendQueueTimer.Start();
			}
			catch (Exception)
			{
			}
		}

		private void timerModbus_Tick(object sender, EventArgs e)
		{
			if (Common.bolStartingUpdate || !Common.frmCloseLabel[0])
			{
				return;
			}
			try
			{
				switch (State)
				{
					case WorkState.Search:
						// timerModbus.Interval = 300000 / iBaud + 1;
						if (byteSearchDeviceNo == byte.MaxValue)
						{
							byteSearchDeviceNo = 0;
							State = WorkState.ReadSensor;
						}
						else
						{
							ReadSensor(byteSearchDeviceNo++, 0, 1);
							SendModbusQueue(null, null);
							this.StatusDisplay("Search:0x" + byteSearchDeviceNo.ToString("X2"));
						}
						break;
					case WorkState.ReadSensor:
						// timerModbus.Interval = 1500000 / iBaud + 1;
						if (SelectedDevice.Count > 0)
						{
							if (ModbusReadIndex >= SelectedDevice.Count)
							{
								ModbusReadIndex = 0;
							}
							byte ucChipID = (byte)Convert.ToInt32((string)SelectedDevice[ModbusReadIndex], 16);
							ReadSensor(ucChipID, 48, 41);
							ModbusReadIndex++;
						}
						break;
				}
			}
			catch (Exception)
			{
			}
		}

		public void stopSearch()
		{
			byteSearchDeviceNo = 79;
			State = WorkState.NOFF;
		}

		public void DeviceSelect(string str)
		{
			try
			{
				ModbusSendQueue.Clear();
				RegReadQueue.Clear();
				byteSearchDeviceNo = 0;
				State = WorkState.ReadSensor;
				byteChipID = (byte)Convert.ToInt32(str, 16);
			}
			catch (Exception)
			{
			}
		}

		public sbyte SendMessage(byte[] byteSend)
		{
			ModbusSendQueue.Enqueue(byteSend);
			return 1;
		}

		public void StartSearchDevices()
		{
			if (State == WorkState.Search)
			{
				byteSearchDeviceNo = 0;
				State = WorkState.ReadSensor;
			}
			else
			{
				byteSearchDeviceNo = 0;
				State = WorkState.Search;
			}
			DeviceList.Clear();
		}

		public void StartReadDevices()
		{
			RegReadQueue.Enqueue(byte.MaxValue);
		}

		protected void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			// base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MiniIMU.Sensor));
			// this.panel1 = new System.Windows.Forms.Panel();
			// this.timerModbus = new System.Windows.Forms.Timer(this.components);
			// this.timerReadReg = new System.Windows.Forms.Timer(this.components);
			// this.sendQueueTimer = new System.Windows.Forms.Timer(this.components);
			// this.timer1 = new System.Windows.Forms.Timer(this.components);
			// base.SuspendLayout();
			// this.panel1.BackColor = System.Drawing.Color.Transparent;
			// resources.ApplyResources(this.panel1, "panel1");
			// this.panel1.Name = "panel1";
			// this.timerModbus.Tick += new System.EventHandler(timerModbus_Tick);
			// this.timerReadReg.Enabled = true;
			// this.timerReadReg.Interval = 120;
			// this.timerReadReg.Tick += new System.EventHandler(timerReadReg_Tick);
			// this.sendQueueTimer.Enabled = true;
			// this.sendQueueTimer.Interval = 10;
			// this.sendQueueTimer.Tick += new System.EventHandler(SendModbusQueue);
			// this.timer1.Enabled = true;
			// this.timer1.Interval = 200;
			// this.timer1.Tick += new System.EventHandler(DisplayRefresh);
			// resources.ApplyResources(this, "$this");
			// base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			// base.Controls.Add(this.panel1);
			// this.DoubleBuffered = true;
			// base.Name = "Sensor";
			// base.Load += new System.EventHandler(Sensor_Load);
			Sensor_Load(null, null);
			// base.ResumeLayout(false);
		}
	}
}