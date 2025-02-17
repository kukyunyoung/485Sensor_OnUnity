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
using UnityEditor.PackageManager;
using System.Threading;
using TMPro;
using UnityEngine.UI;

public class ModbugReaderSoundSensor : MonoBehaviour
{
    public TMP_Dropdown cbPort;
    public TMP_Dropdown cbBaud;
    public Button btnOpenPort;
    public Button btnClosePort;
    public TMP_Text statusText;

    private SerialPort serialPort;
    private Modbus modbus = new Modbus();
    [SerializeField] float checktime;

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
    double noiseInDb;

    public int datalength = 1;

    void Start()
    {
        modbus.WriteMessage += SendMessage;
        RefreshComPort();
        btnOpenPort.onClick.AddListener(OpenPort);
        btnClosePort.onClick.AddListener(ClosePort);
    }

    public int slave = 1;
    public bool bRead = false;

    public int addr = 0;
    public bool aRead = false;

    private void Update()
    {
        if (slave == 248 || addr == 65535)
        {
            Debug.Log("탐색종료");
            slave++;
            return;
        }
        if (slave > 248 || addr > 65535) return;
        if (checktime < Time.time)
        {
            if (serialPort == null || serialPort.IsOpen == false) return;

            checktime = Time.time + .1f;
            // modbus.ModbusReadReg(0x01, 0x0000, 1);
            byte slaveaddr = (byte)slave;
            ushort regaddr = (ushort)addr;
            Debug.Log("<Color=green>addr: " + slaveaddr + "</Color>" + " <Color=red>regaddr: " + regaddr + "</Color>");
            // modbus.ModbusReadReg(slaveaddr, regaddr, 1);
            // 통신가능 버스 읽기
            //modbus.ModbusReadReg(0xFA, 0x0066, 0x0001);
            // 보드레이트 읽기
            modbus.ModbusReadReg(0x01, 0x0067, 0x0001);

            if (bRead)
                slave++;
            if (aRead)
                addr++;
            SendModbusQueue(this, new EventArgs());
            CopeSerialData();

            statusText.text = "db: " + noiseInDb;

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
                    ParseModbusData(array);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Error: " + e.Message);
                bListening = false;
            }
        }
    }

    public void ParseModbusData(byte[] response)
    {
        // 응답의 길이를 확인 (최소 7바이트: 주소, 기능 코드, 데이터 길이, 데이터, CRC)
        if (response.Length < 7)
        {
            throw new ArgumentException("응답 데이터가 충분하지 않습니다.");
        }

        // 응답 데이터에서 중요한 부분 추출
        // 데이터는 응답 배열의 3번째 인덱스부터 시작 (데이터 길이는 이미 2바이트로 가정)
        ushort noiseRawValue = (ushort)(response[3] << 8 | response[4]);
        string bytes = string.Empty;
        for (int i = 0; i < 8; i++)
        {
            bytes += response[i].ToString("X2") + " ";
        }
        UnityEngine.Debug.Log("받은 데이터: " + bytes);

        // 16진수 데이터를 10진수로 변환
        int noiseValue = noiseRawValue;

        // 센서 해상도 0.1을 적용하여 dB 값으로 변환
        double noiseInDb = noiseValue / 10.0;

    }

    private static double ParseSensorData(byte[] response, int startIndex)
    {
        // 16비트 정수 값을 읽고 변환 (가속도/각속도/자기장)
        ushort rawData = (ushort)(response[startIndex] << 8 | response[startIndex + 1]);
        return rawData; // 가속도와 각속도의 변환 공식
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
                // UnityEngine.Debug.Log(string.Format("地址={0},指令={1}", array[0].ToString("X2"), text));
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
                // UsbLibrary.UsbHidPort usbHidPort = usb;
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
