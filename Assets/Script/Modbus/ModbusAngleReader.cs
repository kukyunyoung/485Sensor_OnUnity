using System;
using System.IO.Ports;
using UnityEngine;

public class ModbusAngleReader : MonoBehaviour
{
    private SerialPort serialPort;

    void Start()
    {
        serialPort = new SerialPort("COM5", 9600);
        serialPort.Parity = Parity.None;
        serialPort.DataBits = 8;
        serialPort.StopBits = StopBits.One;
        serialPort.Handshake = Handshake.None;
        serialPort.ReadTimeout = 2000;
        serialPort.WriteTimeout = 1000;

        try
        {
            serialPort.Open();
            serialPort.DiscardInBuffer();
            serialPort.DiscardOutBuffer();
            Debug.Log("Serial port opened");

            // Modbus 요청 보내기 (0x3D: Roll, 0x3E: Pitch, 0x3F: Yaw)
            byte[] request = CreateModbusRequest(0x03, 0x3D, 0x0003);
            serialPort.Write(request, 0, request.Length);

            byte[] response = new byte[11];
            int bytesRead = serialPort.Read(response, 0, response.Length);

            if (bytesRead > 0)
            {
                float roll = ConvertToAngle(response[3], response[4]);
                float pitch = ConvertToAngle(response[5], response[6]);
                float yaw = ConvertToAngle(response[7], response[8]);

                transform.rotation = Quaternion.Euler(pitch, yaw, roll);
                Debug.Log($"Roll: {roll}, Pitch: {pitch}, Yaw: {yaw}");
            }
            else
            {
                Debug.LogWarning("No data received");
            }
        }
        catch (TimeoutException)
        {
            Debug.LogError("Read timed out.");
        }
        catch (Exception e)
        {
            Debug.LogError("Error: " + e.Message);
        }
        finally
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
                Debug.Log("Serial port closed");
            }
        }
    }

    private byte[] CreateModbusRequest(byte functionCode, ushort startAddress, ushort numberOfRegisters)
    {
        byte[] array = new byte[8];
        array[0] = 0x01; // 장치 주소
        array[1] = functionCode;
        array[2] = (byte)(startAddress >> 8);
        array[3] = (byte)(startAddress & 0xFF);
        array[4] = (byte)(numberOfRegisters >> 8);
        array[5] = (byte)(numberOfRegisters & 0xFF);

        ushort crc = CalculateCRC(array, 6);
        array[6] = (byte)(crc & 0xFF);
        array[7] = (byte)(crc >> 8);

        return array;
    }

    private float ConvertToAngle(byte highByte, byte lowByte)
    {
        short value = (short)((highByte << 8) | lowByte);
        return value / 32768.0f * 180.0f;
    }

    private ushort CalculateCRC(byte[] data, int length)
    {
        ushort crc = 0xFFFF;
        for (int pos = 0; pos < length; pos++)
        {
            crc ^= (ushort)data[pos];
            for (int i = 8; i != 0; i--)
            {
                if ((crc & 0x0001) != 0)
                {
                    crc >>= 1;
                    crc ^= 0xA001;
                }
                else
                {
                    crc >>= 1;
                }
            }
        }
        return crc;
    }

    void OnApplicationQuit()
    {
        if (serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }
}
