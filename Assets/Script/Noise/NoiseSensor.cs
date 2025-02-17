using System.Collections;
using System.Collections.Generic;
using ModBusRTU;
using Unity.VisualScripting;
using UnityEngine;

public class NoiseSensor : MonoBehaviour
{
    ModBusNoise modBusNoise;

    void Start()
    {
        modBusNoise = new ModBusNoise();
        print($"PortName: {modBusNoise.PortName}, BaudRate: {modBusNoise.BaudRate}, Parity: {modBusNoise.Parity}, StopBits: {modBusNoise.StopBits}");
        modBusNoise.Open();
    }

    void Update()
    {
        if (modBusNoise.IsOpen())
        {
            byte functionCode = 0x03;
            ushort startAddress = 0x0000;
            //ushort numberOfBit = (ushort)modBusNoise._serialPort.BytesToRead;
            ushort numberOfBit = 1;
            print("numberOfBit: " + numberOfBit);
            var resultBit = modBusNoise.ReadBit(functionCode, startAddress, numberOfBit);
            print(resultBit.ToHexString());
            ushort length = 16;
            var resultWord = modBusNoise.ReadWords(functionCode, startAddress, length);
            print(resultWord.ToHexString());
        }
    }
}
