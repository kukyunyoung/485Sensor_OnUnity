namespace MiniIMU
{

	internal class Modbus
	{
		public enum Function
		{
			FuncW = 6,
			FuncR = 3
		}

		public delegate sbyte SendMessageHandler(byte[] byteSend);

		private byte[] auchCRCHi = new byte[256]
		{
		0, 193, 129, 64, 1, 192, 128, 65, 1, 192,
		128, 65, 0, 193, 129, 64, 1, 192, 128, 65,
		0, 193, 129, 64, 0, 193, 129, 64, 1, 192,
		128, 65, 1, 192, 128, 65, 0, 193, 129, 64,
		0, 193, 129, 64, 1, 192, 128, 65, 0, 193,
		129, 64, 1, 192, 128, 65, 1, 192, 128, 65,
		0, 193, 129, 64, 1, 192, 128, 65, 0, 193,
		129, 64, 0, 193, 129, 64, 1, 192, 128, 65,
		0, 193, 129, 64, 1, 192, 128, 65, 1, 192,
		128, 65, 0, 193, 129, 64, 0, 193, 129, 64,
		1, 192, 128, 65, 1, 192, 128, 65, 0, 193,
		129, 64, 1, 192, 128, 65, 0, 193, 129, 64,
		0, 193, 129, 64, 1, 192, 128, 65, 1, 192,
		128, 65, 0, 193, 129, 64, 0, 193, 129, 64,
		1, 192, 128, 65, 0, 193, 129, 64, 1, 192,
		128, 65, 1, 192, 128, 65, 0, 193, 129, 64,
		0, 193, 129, 64, 1, 192, 128, 65, 1, 192,
		128, 65, 0, 193, 129, 64, 1, 192, 128, 65,
		0, 193, 129, 64, 0, 193, 129, 64, 1, 192,
		128, 65, 0, 193, 129, 64, 1, 192, 128, 65,
		1, 192, 128, 65, 0, 193, 129, 64, 1, 192,
		128, 65, 0, 193, 129, 64, 0, 193, 129, 64,
		1, 192, 128, 65, 1, 192, 128, 65, 0, 193,
		129, 64, 0, 193, 129, 64, 1, 192, 128, 65,
		0, 193, 129, 64, 1, 192, 128, 65, 1, 192,
		128, 65, 0, 193, 129, 64
		};

		private byte[] auchCRCLo = new byte[256]
		{
		0, 192, 193, 1, 195, 3, 2, 194, 198, 6,
		7, 199, 5, 197, 196, 4, 204, 12, 13, 205,
		15, 207, 206, 14, 10, 202, 203, 11, 201, 9,
		8, 200, 216, 24, 25, 217, 27, 219, 218, 26,
		30, 222, 223, 31, 221, 29, 28, 220, 20, 212,
		213, 21, 215, 23, 22, 214, 210, 18, 19, 211,
		17, 209, 208, 16, 240, 48, 49, 241, 51, 243,
		242, 50, 54, 246, 247, 55, 245, 53, 52, 244,
		60, 252, 253, 61, 255, 63, 62, 254, 250, 58,
		59, 251, 57, 249, 248, 56, 40, 232, 233, 41,
		235, 43, 42, 234, 238, 46, 47, 239, 45, 237,
		236, 44, 228, 36, 37, 229, 39, 231, 230, 38,
		34, 226, 227, 35, 225, 33, 32, 224, 160, 96,
		97, 161, 99, 163, 162, 98, 102, 166, 167, 103,
		165, 101, 100, 164, 108, 172, 173, 109, 175, 111,
		110, 174, 170, 106, 107, 171, 105, 169, 168, 104,
		120, 184, 185, 121, 187, 123, 122, 186, 190, 126,
		127, 191, 125, 189, 188, 124, 180, 116, 117, 181,
		119, 183, 182, 118, 114, 178, 179, 115, 177, 113,
		112, 176, 80, 144, 145, 81, 147, 83, 82, 146,
		150, 86, 87, 151, 85, 149, 148, 84, 156, 92,
		93, 157, 95, 159, 158, 94, 90, 154, 155, 91,
		153, 89, 88, 152, 136, 72, 73, 137, 75, 139,
		138, 74, 78, 142, 143, 79, 141, 77, 76, 140,
		68, 132, 133, 69, 135, 71, 70, 134, 130, 66,
		67, 131, 65, 129, 128, 64
		};

		public event SendMessageHandler WriteMessage;

		private ushort CRC16(byte[] puchMsg, ushort usDataLen)
		{
			byte b = byte.MaxValue;
			byte b2 = byte.MaxValue;
			for (int i = 0; i < usDataLen; i++)
			{
				ushort num = (byte)(b ^ puchMsg[i]);
				b = (byte)(b2 ^ auchCRCHi[num]);
				b2 = auchCRCLo[num];
			}
			return (ushort)((b << 8) | b2);
		}


		public void ModbusReadReg(byte Addr, ushort usReg, ushort usRegNum)
		{
			byte[] array = new byte[8]
			{
			Addr,
			3,
			(byte)(usReg >> 8),
			(byte)(usReg & 0xFFu),
			(byte)(usRegNum >> 8),
			(byte)(usRegNum & 0xFFu),
			0,
			0
			};
			ushort num = CRC16(array, 6);
			array[6] = (byte)(num >> 8);
			array[7] = (byte)(num & 0xFFu);
			if (this.WriteMessage != null)
			{
				this.WriteMessage(array);
			}
			string bytes = string.Empty;
			for (int i = 0; i < 8; i++)
			{
				bytes += array[i].ToString("X2") + " ";
			}
		}


		public byte[] get_ModbusReadReg(byte Addr, ushort usReg, ushort usRegNum, byte ReadCommand = 0)
		{
			byte[] array = new byte[8] { Addr, 0, 0, 0, 0, 0, 0, 0 };
			if (ReadCommand == 0)
			{
				array[1] = 3;
			}
			else
			{
				array[1] = ReadCommand;
			}
			array[2] = (byte)(usReg >> 8);
			array[3] = (byte)(usReg & 0xFFu);
			array[4] = (byte)(usRegNum >> 8);
			array[5] = (byte)(usRegNum & 0xFFu);
			ushort num = CRC16(array, 6);
			array[6] = (byte)(num >> 8);
			array[7] = (byte)(num & 0xFFu);
			return array;
		}

		public void ModbusWriteReg(byte Addr, ushort usReg, short ucData)
		{
			byte[] array = new byte[8]
			{
			Addr,
			6,
			(byte)(usReg >> 8),
			(byte)(usReg & 0xFFu),
			(byte)(ucData >> 8),
			(byte)((uint)ucData & 0xFFu),
			0,
			0
			};
			ushort num = CRC16(array, 6);
			array[6] = (byte)(num >> 8);
			array[7] = (byte)(num & 0xFFu);
			if (this.WriteMessage != null)
			{
				this.WriteMessage(array);
			}
		}

		public byte[] get_ModbusWriteReg(byte Addr, ushort usReg, short ucData, byte WriteCommand = 0)
		{
			byte[] array = new byte[8] { Addr, 0, 0, 0, 0, 0, 0, 0 };
			if (WriteCommand == 0)
			{
				array[1] = 6;
			}
			else
			{
				array[1] = WriteCommand;
			}
			array[2] = (byte)(usReg >> 8);
			array[3] = (byte)(usReg & 0xFFu);
			array[4] = (byte)(ucData >> 8);
			array[5] = (byte)((uint)ucData & 0xFFu);
			ushort num = CRC16(array, 6);
			array[6] = (byte)(num >> 8);
			array[7] = (byte)(num & 0xFFu);
			return array;
		}

		public bool ModbusMsgCheck(byte[] byteTemp)
		{
			ushort num = (ushort)(byteTemp[2] + 3);
			ushort num2 = CRC16(byteTemp, num);
			ushort num3 = (ushort)((byteTemp[num] << 8) | byteTemp[num + 1]);
			if (num2 == num3)
			{
				return true;
			}
			return false;
		}
	}
}