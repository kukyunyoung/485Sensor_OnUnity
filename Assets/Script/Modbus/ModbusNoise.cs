using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiniIMU.Public;
using Unity.VisualScripting;
using UnityEngine;
using UsbLibrary;

namespace ModBusRTU
{
    public class ModBusNoise : IDisposable
    {
        static object lockSend = new object();
        public SerialPort _serialPort;
        private UsbHidPort usb;
        private Queue ModbusSendQueue = new Queue();
        private byte byteReadID;

        private static readonly ushort[] CrcTable = {
            0X0000, 0XC0C1, 0XC181, 0X0140, 0XC301, 0X03C0, 0X0280, 0XC241,
            0XC601, 0X06C0, 0X0780, 0XC741, 0X0500, 0XC5C1, 0XC481, 0X0440,
            0XCC01, 0X0CC0, 0X0D80, 0XCD41, 0X0F00, 0XCFC1, 0XCE81, 0X0E40,
            0X0A00, 0XCAC1, 0XCB81, 0X0B40, 0XC901, 0X09C0, 0X0880, 0XC841,
            0XD801, 0X18C0, 0X1980, 0XD941, 0X1B00, 0XDBC1, 0XDA81, 0X1A40,
            0X1E00, 0XDEC1, 0XDF81, 0X1F40, 0XDD01, 0X1DC0, 0X1C80, 0XDC41,
            0X1400, 0XD4C1, 0XD581, 0X1540, 0XD701, 0X17C0, 0X1680, 0XD641,
            0XD201, 0X12C0, 0X1380, 0XD341, 0X1100, 0XD1C1, 0XD081, 0X1040,
            0XF001, 0X30C0, 0X3180, 0XF141, 0X3300, 0XF3C1, 0XF281, 0X3240,
            0X3600, 0XF6C1, 0XF781, 0X3740, 0XF501, 0X35C0, 0X3480, 0XF441,
            0X3C00, 0XFCC1, 0XFD81, 0X3D40, 0XFF01, 0X3FC0, 0X3E80, 0XFE41,
            0XFA01, 0X3AC0, 0X3B80, 0XFB41, 0X3900, 0XF9C1, 0XF881, 0X3840,
            0X2800, 0XE8C1, 0XE981, 0X2940, 0XEB01, 0X2BC0, 0X2A80, 0XEA41,
            0XEE01, 0X2EC0, 0X2F80, 0XEF41, 0X2D00, 0XEDC1, 0XEC81, 0X2C40,
            0XE401, 0X24C0, 0X2580, 0XE541, 0X2700, 0XE7C1, 0XE681, 0X2640,
            0X2200, 0XE2C1, 0XE381, 0X2340, 0XE101, 0X21C0, 0X2080, 0XE041,
            0XA001, 0X60C0, 0X6180, 0XA141, 0X6300, 0XA3C1, 0XA281, 0X6240,
            0X6600, 0XA6C1, 0XA781, 0X6740, 0XA501, 0X65C0, 0X6480, 0XA441,
            0X6C00, 0XACC1, 0XAD81, 0X6D40, 0XAF01, 0X6FC0, 0X6E80, 0XAE41,
            0XAA01, 0X6AC0, 0X6B80, 0XAB41, 0X6900, 0XA9C1, 0XA881, 0X6840,
            0X7800, 0XB8C1, 0XB981, 0X7940, 0XBB01, 0X7BC0, 0X7A80, 0XBA41,
            0XBE01, 0X7EC0, 0X7F80, 0XBF41, 0X7D00, 0XBDC1, 0XBC81, 0X7C40,
            0XB401, 0X74C0, 0X7580, 0XB541, 0X7700, 0XB7C1, 0XB681, 0X7640,
            0X7200, 0XB2C1, 0XB381, 0X7340, 0XB101, 0X71C0, 0X7080, 0XB041,
            0X5000, 0X90C1, 0X9181, 0X5140, 0X9301, 0X53C0, 0X5280, 0X9241,
            0X9601, 0X56C0, 0X5780, 0X9741, 0X5500, 0X95C1, 0X9481, 0X5440,
            0X9C01, 0X5CC0, 0X5D80, 0X9D41, 0X5F00, 0X9FC1, 0X9E81, 0X5E40,
            0X5A00, 0X9AC1, 0X9B81, 0X5B40, 0X9901, 0X59C0, 0X5880, 0X9841,
            0X8801, 0X48C0, 0X4980, 0X8941, 0X4B00, 0X8BC1, 0X8A81, 0X4A40,
            0X4E00, 0X8EC1, 0X8F81, 0X4F40, 0X8D01, 0X4DC0, 0X4C80, 0X8C41,
            0X4400, 0X84C1, 0X8581, 0X4540, 0X8701, 0X47C0, 0X4680, 0X8641,
            0X8201, 0X42C0, 0X4380, 0X8341, 0X4100, 0X81C1, 0X8081, 0X4040 };

        public int BaudRate
        {
            get { return _serialPort.BaudRate; }
            set { _serialPort.BaudRate = value; }
        }
        public Parity Parity
        {
            get { return _serialPort.Parity; }
            set { _serialPort.Parity = value; }
        }
        public StopBits StopBits
        {
            get { return _serialPort.StopBits; }
            set { _serialPort.StopBits = value; }
        }
        public int DataBits
        {
            get { return _serialPort.DataBits; }
            set { _serialPort.DataBits = value; }
        }
        public string PortName
        {
            get { return _serialPort.PortName; }
            set { _serialPort.PortName = value; }
        }
        public int ReadTimeout
        {
            get { return _serialPort.ReadTimeout; }
            set { _serialPort.ReadTimeout = value; }
        }

        private byte _slaveNo;
        public byte SlaveNo
        {
            get { return _slaveNo; }
            set { _slaveNo = value; }
        }

        List<byte> _readPacket;
        ManualResetEvent _eventReset = new ManualResetEvent(false);
        int responseLength;
        bool isWatingResponse;
        public ModBusNoise()
        {
            _serialPort = new SerialPort();
            _serialPort.PortName = SerialPort.GetPortNames()[0];
            _serialPort.BaudRate = 9600;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.ReadTimeout = 2000;
            _serialPort.WriteTimeout = 1000;
            _serialPort.DtrEnable = true; // DataTransmitReady 데이터 수신을 위함
            //_serialPort.ReceivedBytesThreshold = 10;

            SlaveNo = 1;
            responseLength = 0;
            isWatingResponse = false;

            _readPacket = new List<byte>();
            //_serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceived); // 포트에 데이터가 들어올때 자동으로 호출되는 이벤트에 델리게이트 등록
        }

        public void Open()
        {
            try
            {
                _serialPort.Open();
                _serialPort.RtsEnable = true;

                Debug.Log("Open");
            }
            catch (Exception ex)
            {
                Debug.Log("Open Error : " + ex.Message);
            }
        }

        public bool IsOpen()
        {
            return _serialPort.IsOpen;
        }

        public void Close()
        {
            Dispose();
        }

        private void DataReceived(object sender, EventArgs e)
        {
            Debug.Log("DataReceived");
            if (isWatingResponse)
            {
                Thread.Sleep(100);
                // 현재까지 도착한 데이타 모두 읽기
                Debug.Log("responseLength : "+responseLength);
                byte[] vs = new byte[responseLength];
                _serialPort.Read(vs, 0, 1);
                _readPacket.AddRange(vs);
                isWatingResponse = false;
                _eventReset.Set();
            }
        }

        private void CopeSerialData()
        {
            byte[] array = new byte[2000];
            if (isWatingResponse)
            {
                try
                {
                    if (_serialPort.BytesToRead != 0)
                    {
                        ushort usLength = (ushort)_serialPort.Read(array, 0, 1);
                        ParseNoiseSensorData(array);
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Error: " + e.Message);
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
            Debug.Log($"output : {output[0]}{output[1]} {output[2]}{output[3]} {output[4]}{output[5]} {output[6]}{output[7]} {output[8]}{output[9]} {output[10]}{output[11]} {output[12]}{output[13]} {output[14]}{output[15]}");
        }

        public byte[] ReadBit(byte functionCode, ushort startAddress, ushort numberOfBit)
        {
            lock (lockSend)
            {
                byte[] byteStartAddress = BitConverter.GetBytes(startAddress);
                byte[] byteLength = BitConverter.GetBytes(numberOfBit);
                List<byte> packet = new List<byte>() { SlaveNo, functionCode, byteStartAddress[1], byteStartAddress[0], byteLength[1], byteLength[0] };

                packet.AddRange(BitConverter.GetBytes(ComputeCrc16(packet.ToArray())));

                _readPacket.Clear();

                // slave, functionCode, length, (byte high, byte low)n + crc1 + crc2
                responseLength = 3 + numberOfBit / 8 + 2;

                isWatingResponse = true;
                _serialPort.Write(packet.ToArray(), 0, packet.Count);
                Debug.Log(packet.ToArray().ToHexString()); // 입력비트 수신 01 03 00 00 00 01 84 0A

                _eventReset.WaitOne(500);
                _eventReset.Reset();

                DataReceived(this, new EventArgs());
                CopeSerialData();

                if(_readPacket.Count == 0)
                {
                    Debug.Log("ReadBit: 0 (Slave와 통신 안되고 있음)");
                    return new byte[1] { 0 };
                }

                var crcCheck = _readPacket.Take(_readPacket.Count - 2).ToArray();
                byte[] computedReadCrc = BitConverter.GetBytes(ComputeCrc16(crcCheck));
                if (_readPacket[_readPacket.Count - 2] != computedReadCrc[0] || _readPacket[_readPacket.Count - 1] != computedReadCrc[1])
                {
                    return new byte[1] { 0 };
                }
                else
                {
                    _readPacket.RemoveAt(0);
                    _readPacket.RemoveAt(0);
                    _readPacket.RemoveAt(0);
                    _readPacket.RemoveAt(_readPacket.Count - 1);
                    _readPacket.RemoveAt(_readPacket.Count - 1);
                    return _readPacket.ToArray();
                }
            }
        }

        public byte[] ReadWords(byte functionCode, ushort startAddress, ushort length)
        {
            lock (lockSend)
            {
                byte[] byteStartAddress = BitConverter.GetBytes(startAddress);
                byte[] byteLength = BitConverter.GetBytes(length);
                List<byte> packet = new List<byte>() { SlaveNo, functionCode, byteStartAddress[1], byteStartAddress[0], byteLength[1], byteLength[0] };

                packet.AddRange(BitConverter.GetBytes(ComputeCrc16(packet.ToArray())));

                _readPacket.Clear();

                // slave, functionCode, length, (byte high, byte low)n + crc1 + crc2
                responseLength = 3 + length * 2 + 2;

                isWatingResponse = true;
                _serialPort.Write(packet.ToArray(), 0, packet.Count);

                _eventReset.WaitOne(500);
                _eventReset.Reset();

                //byte[] readPacket = new byte[3 + length * 2 + 2];
                if (_readPacket.Count == 0)
                {
                    return new byte[1] { 0 };
                }

                var crcCheck = _readPacket.Take(_readPacket.Count - 2).ToArray();
                byte[] computedReadCrc = BitConverter.GetBytes(ComputeCrc16(crcCheck));
                if (_readPacket[_readPacket.Count - 2] != computedReadCrc[0] || _readPacket[_readPacket.Count - 1] != computedReadCrc[1])
                {
                    return new byte[1] { 0 };
                }
                else
                {
                    _readPacket.RemoveAt(0);
                    _readPacket.RemoveAt(0);
                    _readPacket.RemoveAt(0);
                    _readPacket.RemoveAt(_readPacket.Count - 1);
                    _readPacket.RemoveAt(_readPacket.Count - 1);
                    return _readPacket.ToArray();
                }
            }
        }

        private sbyte SendData(byte[] byteSend)
        {
            var sb = SendUSBMsg(3, byteSend, (byte)byteSend.Length);
            if (!_serialPort.IsOpen)
            {
                Debug.Log("Port Not Open!");
                return 0;
            }
            try
            {
                _serialPort.Write(byteSend, 0, byteSend.Length);
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

        public UInt16 ComputeCrc(byte[] data)
        {
            ushort crc = 0xFFFF;

            foreach (byte datum in data)
            {
                crc = (ushort)((crc >> 8) ^ CrcTable[(crc ^ datum) & 0xFF]);
            }

            return crc;
        }

        public UInt16 ComputeCrc16(byte[] data)
        {
            ushort crc = 0xFFFF;

            foreach (byte datum in data)
            {
                crc = (ushort)((crc >> 8) ^ CrcTable[(crc ^ datum) & 0xFF]);
            }

            return (ushort)((crc << 8) | (crc >> 8));;
        }

        #region IDisposable Support
        private bool disposedValue = false; // 중복 호출을 검색하려면

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 관리되는 상태(관리되는 개체)를 삭제합니다.
                    _serialPort.Close();
                    _serialPort.Dispose();
                }

                // TODO: 관리되지 않는 리소스(관리되지 않는 개체)를 해제하고 아래의 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.

                disposedValue = true;
            }
        }

        // TODO: 위의 Dispose(bool disposing)에 관리되지 않는 리소스를 해제하는 코드가 포함되어 있는 경우에만 종료자를 재정의합니다.
        // ~ModBus() {
        //   // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
        //   Dispose(false);
        // }

        // 삭제 가능한 패턴을 올바르게 구현하기 위해 추가된 코드입니다.
        public void Dispose()
        {
            // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
            Dispose(true);
            // TODO: 위의 종료자가 재정의된 경우 다음 코드 줄의 주석 처리를 제거합니다.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}