using System.IO.Ports;
using System;
using UnityEngine;
using MiniIMU;
using System.Collections.Generic;
using System.Collections;
using MiniIMU.Public;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Debug = UnityEngine.Debug;
using UsbLibrary;
using System.Threading;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;

public class ModbusReader : MonoBehaviour
{
    public TMP_Dropdown cbPort;
    public TMP_Dropdown cbBaud;
    public Button btnOpenPort;
    public Button btnClosePort;
    public TMP_Text statusText;

    private SerialPort serialPort;
    private Modbus modbus = new Modbus();
    float checktime;

    private Queue ModbusSendQueue = new Queue();
    public int iBaud = 9600;
    public delegate sbyte SendMessageHandler(byte[] byteSend);
    private byte byteReadID;
    private byte byteReadStartIndex;
    private UsbHidPort usb;
    private IContainer components;

    private bool bClosing;
    private bool bListening;
    private Sensor formSensor;

    // 가속도
    public double ax;
    public double ay;
    public double az;
    // 각속도;
    public double gx;
    public double gy;
    public double gz;
    // 자기장;
    public double hx;
    public double hy;
    public double hz;
    // 각도;
    public double roll;
    public double pitch;
    public double yaw;
    // 온도;
    public double temp;
    // 공기압 데이터 파싱
    double pressure;
    // GPS 경도 및 위도 파싱
    double longitude;
    double latitude;
    // GPS 고도 및 속도 파싱
    double gpsHeight;
    double gpsSpeed;

    public double Longitude{ get { return longitude; } }

    public int datalength = 1;

    void Start()
    {
        modbus.WriteMessage += SendMessage;

        RefreshComPort();
        btnOpenPort.onClick.AddListener(OpenPort);
        btnClosePort.onClick.AddListener(ClosePort);
    }

    private void Update()
    {
        if (checktime < Time.time)
        {
            if (serialPort == null || serialPort.IsOpen == false) return;

            checktime = Time.time + .1f;
            modbus.ModbusReadReg(0x50, 0x0034, 20);  // 기울기센서
            //modbus.ModbusReadReg(0x01, 0x0000, 1); // 소음센서
            SendModbusQueue(this, new EventArgs());
            CopeSerialData();

            statusText.text = "가속도: " + ax + " " + ay + " " + az + "\n" +
                "각속도: " + gx + " " + gy + " " + gz + "\n" +
                "자기장: " + hx + " " + hy + " " + hz + "\n" +
                "각도: " + roll + " " + pitch + " " + yaw + "\n" +
                "온도: " + temp + "°C" + "\n"
                + "GPS 경도: " + longitude + " " + "위도: " + latitude + "\n"
                + "GPS 고도: " + gpsHeight + " " + "속도: " + gpsSpeed + "\n"
                + "공기압: " + pressure + "Pa";
        }
    }

    private void OnDisable()
    {
        // 시리얼 포트 닫기
        serialPort.Close();
    }

    private void CopeSerialData()
    {
        byte[] array = new byte[2000];
        if (!bClosing)
        {
            try
            {
                bListening = true;
                if (serialPort.BytesToRead != 0)
                {
                    ushort usLength = (ushort)serialPort.Read(array, 0, 1000);
                    ParseModbusData(array); // 기울기센서
                    //ParseNoiseSensorData(array); // 소음센서
                    serialPort.DiscardInBuffer();
                    serialPort.DiscardOutBuffer();
                }
            }
            catch (Exception e)
            {
                Debug.Log("Error: " + e.Message);
                bListening = false;
            }
        }
    }

    public void ParseNoiseSensorData(byte[] response)
    {
        if (response.Length < 8)
        {
            Debug.Log("응답 데이터가 충분하지 않습니다.");
            return;
        }
        byte[] data = response;
        string output = data.ToHexString();
        print($"output : {output[0]}{output[1]} {output[2]}{output[3]} {output[4]}{output[5]} {output[6]}{output[7]} {output[8]}{output[9]} {output[10]}{output[11]} {output[12]}{output[13]} {output[14]}{output[15]}");
    }

    public void ParseModbusData(byte[] response)
    {
        if (response.Length < 43)
        {
            Debug.Log("응답 데이터가 충분하지 않습니다.");
            return;
        }

        // 가속도 데이터 파싱
        ax = ParseSensorData(response, 3);
        ay = ParseSensorData(response, 5);
        az = ParseSensorData(response, 7);

        // 각속도 데이터 파싱
        gx = ParseSensorData(response, 9);
        gy = ParseSensorData(response, 11);
        gz = ParseSensorData(response, 13);

        // 자기장 데이터 파싱
        hx = ParseSensorData(response, 15);
        hy = ParseSensorData(response, 17);
        hz = ParseSensorData(response, 19);

        // 각도 데이터 파싱 (Roll, Pitch, Yaw)
        roll = ParseAngleData(response, 21);
        pitch = ParseAngleData(response, 23);
        yaw = ParseAngleData(response, 25);

        // 온도 데이터 파싱
        temp = ParseTemperatureData(response, 27);

        // 공기압 데이터 파싱
        pressure = ParsePressureData(response, 33);

        // GPS 경도 및 위도 파싱
        longitude = ParseGPSData(response, 41);
        latitude = ParseGPSData(response, 45);

        // GPS 고도 및 속도 파싱
        gpsHeight = ParseGPSHeight(response, 49);
        gpsSpeed = ParseGPSSpeed(response, 51);

    }

    private static double ParseSensorData(byte[] response, int startIndex)
    {
        // 16비트 정수 값을 읽고 변환 (가속도/각속도/자기장)
        ushort rawData = (ushort)(response[startIndex] << 8 | response[startIndex + 1]);
        return rawData / 32768.0 * 16.0; // 가속도와 각속도의 변환 공식
    }

    private static double ParseAngleData(byte[] response, int startIndex)
    {
        // 16비트 정수 값을 읽고 변환 (Roll, Pitch, Yaw)
        ushort rawData = (ushort)(response[startIndex] << 8 | response[startIndex + 1]);
        return rawData / 32768.0 * 180.0; // 각도의 변환 공식
    }

    private static double ParseTemperatureData(byte[] response, int startIndex)
    {
        // 온도는 100으로 나누어 섭씨로 변환
        ushort rawData = (ushort)(response[startIndex] << 8 | response[startIndex + 1]);
        return rawData / 100.0;
    }

    private static double ParsePressureData(byte[] response, int startIndex)
    {
        // 공기압 데이터 파싱 (32비트 데이터, 두 레지스터 결합)
        int pressureRaw = (response[startIndex] << 24) | (response[startIndex + 1] << 16) |
                          (response[startIndex + 2] << 8) | response[startIndex + 3];
        return pressureRaw; // Pa 단위
    }

    private static double ParseGPSData(byte[] response, int startIndex)
    {
        // GPS 경도/위도 데이터 파싱 (32비트 데이터, 두 레지스터 결합)
        int gpsRaw = (response[startIndex] << 24) | (response[startIndex + 1] << 16) |
                     (response[startIndex + 2] << 8) | response[startIndex + 3];
        return gpsRaw / 10000000.0; // GPS 좌표는 1e7로 나누어 도 단위로 변환
    }

    private static double ParseGPSHeight(byte[] response, int startIndex)
    {
        // GPS 고도 데이터 파싱 (16비트 데이터)
        ushort gpsHeightRaw = (ushort)(response[startIndex] << 8 | response[startIndex + 1]);
        return gpsHeightRaw / 10.0; // 고도는 미터 단위로 변환
    }

    private static double ParseGPSSpeed(byte[] response, int startIndex)
    {
        // GPS 속도 데이터 파싱 (32비트 데이터, 두 레지스터 결합)
        int gpsSpeedRaw = (response[startIndex] << 24) | (response[startIndex + 1] << 16) |
                          (response[startIndex + 2] << 8) | response[startIndex + 3];
        return gpsSpeedRaw / 1000.0; // 속도는 km/h 단위로 변환
    }

    public sbyte SendMessage(byte[] byteSend)
    {
        ModbusSendQueue.Enqueue(byteSend);
        return 1;
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

            if (ModbusSendQueue.Count > 0)
            {
                byte[] array = (byte[])ModbusSendQueue.Dequeue();
                this.SendData(array);
                string text = "";
                for (int i = 0; i < array.Length; i++)
                {
                    text = text + array[i].ToString("X2") + " ";
                }
                //UnityEngine.Debug.Log(string.Format("주소={0},입력={1}", array[0].ToString("X2"), text));
                byteReadID = array[0];
                if (array[1] == 3)
                {
                    byteReadStartIndex = array[3];
                }
            }
        }
        catch (Exception)
        {
        }
    }

    private sbyte SendData(byte[] byteSend)
    {
        var sb = SendUSBMsg(3, byteSend, (byte)byteSend.Length);
        if (!serialPort.IsOpen)
        {
            Debug.Log("Port Not Open!");
            return 0;
        }
        try
        {
            serialPort.Write(byteSend, 0, byteSend.Length);
            return 0;
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
            return -1;
        }
    }

    private sbyte SendUSBMsg(byte ucType, byte[] byteSend, byte ucLength)
    {
        try
        {
            if (usb.SpecifiedDevice != null)
            {
                byte[] array = new byte[67];
                array[1] = ucLength;
                array[2] = ucType;
                byteSend.CopyTo(array, 3);
                usb.SpecifiedDevice.SendData(array);
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex.ToString());
        }
        return 0;
    }



    private void RefreshComPort()
    {
        cbPort.ClearOptions();
        string[] portNames = SerialPort.GetPortNames();
        foreach (string port in portNames)
        {
            cbPort.options.Add(new TMP_Dropdown.OptionData(port));
        }

        if (cbPort.options.Count > 0)
        {
            cbPort.value = 0;
        }
    }

    private void OpenPort()
    {
        // 시리얼 포트 초기화
        serialPort = new SerialPort(cbPort.options[cbPort.value].text, int.Parse(cbBaud.options[cbBaud.value].text), Parity.None, 8, StopBits.One);
        serialPort.Handshake = Handshake.None;
        serialPort.ReadTimeout = 2000;
        serialPort.WriteTimeout = 1000;
        serialPort.DtrEnable = false;
        serialPort.Open();
        this.components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ModbusReader));
        usb = new UsbLibrary.UsbHidPort(this.components);
        formSensor = new Sensor(SensorType.Modbus);
        formSensor.SendData += SendData;
    }

    private void ClosePort()
    {
        // 시리얼 포트 닫기
        serialPort.Close();
    }

}
