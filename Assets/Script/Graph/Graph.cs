using System.Collections;
using System.Collections.Generic;
using ChartAndGraph;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Graph : MonoBehaviour
{
    public int queueSize;

    [Header("Grad Reference")]
    public ModbusReader modbusReader;
    public float gradUpdateTime;
    protected WaitForSeconds gradWfs;
    public UploadTest uploadTest;

    [Header("Volume Reference")]
    public RecordMic recordMic;
    public float volumeUpdateTime;
    public int sampleLength;
    protected WaitForSeconds volumeWfs;

    void Start()
    {
        modbusReader = modbusReader.GetComponent<ModbusReader>();
        gradWfs = new WaitForSeconds(gradUpdateTime);
        volumeWfs = new WaitForSeconds(volumeUpdateTime);
    }

    // 32~0 사이의 값을 보정하기 위해 중간값인 16보다 크면 32를 빼 보정을 함
    // 16보다 크면 대부분 28~32값이므로 -4 ~ 0 의 값을 가질수 있음
    public float Revise(double value)
    {
        if(value > 16)
        {
            value -= 32;
        }
        return (float)value;
    }
}
