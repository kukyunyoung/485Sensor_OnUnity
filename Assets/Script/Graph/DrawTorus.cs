using System;
using System.Collections;
using System.Collections.Generic;
using ChartAndGraph;
using UnityEngine;

public class DrawTorus : MonoBehaviour
{
    public CanvasPieChart pieChart;
    public Graph graph;
    public List<float> value;

    ModbusReader modbusReader;
    WaitForSeconds wfs;
    int dataLen;
    float updateTime;
    float bias;

    void Start()
    {
        Init();
    }

    void OnEnable()
    {
        Init();
        StartCoroutine(Draw());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void Init()
    {
        pieChart = GetComponent<CanvasPieChart>();
        graph = transform.parent.parent.GetComponent<Graph>();
        modbusReader = graph.modbusReader;
        updateTime = graph.gradUpdateTime;
        wfs = new WaitForSeconds(updateTime);
        dataLen = (int)((float)graph.uploadTest.captureTime / updateTime);
        bias = 0.01f;
    }

    public IEnumerator Draw()
    {
        while(true)
        {
            value = new List<float> {0, 0, 0};

            for (int i = 0; i < dataLen; i++)
            {
                value[0] += CalcRatio(modbusReader.gx)+bias;
                value[1] += CalcRatio(modbusReader.gy)+bias;
                value[2] += CalcRatio(modbusReader.gz)+bias;
                yield return wfs;
            }

            pieChart.DataSource.StartBatch();

            // pieChart.DataSource.SetValue("x", value[0]);
            // pieChart.DataSource.SetValue("y", value[1]);
            // pieChart.DataSource.SetValue("z", value[2]);

            pieChart.DataSource.SlideValue("x", value[0], 0.5f);
            pieChart.DataSource.SlideValue("y", value[1], 0.5f);
            pieChart.DataSource.SlideValue("z", value[2], 0.5f);

            pieChart.DataSource.EndBatch();
        }
    }

    float CalcRatio(double value)
    {
        // Mathf.Abs : 절대값 반환
        double x =  Mathf.Abs(graph.Revise(modbusReader.gx));
        double y =  Mathf.Abs(graph.Revise(modbusReader.gy));
        double z =  Mathf.Abs(graph.Revise(modbusReader.gz));
        
        double sum = x + y + z;
        double result = Mathf.Abs(graph.Revise(value));

        if(sum == 0) return 0.03f;
        return (float)(result / sum);
    }
}
