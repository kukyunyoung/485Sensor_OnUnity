using System.Collections;
using System.Collections.Generic;
using ChartAndGraph;
using Unity.VisualScripting;
using UnityEngine;

public class DrawBar : MonoBehaviour
{
    public CanvasBarChart barChart;
    public Graph graph;

    ModbusReader modbusReader;
    WaitForSeconds wfs;
    float updateTime;

    void Start()
    {
        Init();

        StartCoroutine(Draw());
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
        barChart = GetComponent<CanvasBarChart>();
        graph = transform.parent.parent.GetComponent<Graph>();
        modbusReader = graph.modbusReader;
        updateTime = graph.gradUpdateTime;
        wfs = new WaitForSeconds(updateTime);
    }

    public IEnumerator Draw()
    {
        while(true)
        {
            // barChart.DataSource.SetValue("x", "가속도", graph.Revise(modbusReader.ax));
            // barChart.DataSource.SetValue("y", "가속도", graph.Revise(modbusReader.ay));
            // barChart.DataSource.SetValue("z", "가속도", graph.Revise(modbusReader.az));

            barChart.DataSource.SetValue("x", "각속도", graph.Revise(modbusReader.gx));
            barChart.DataSource.SetValue("y", "각속도", graph.Revise(modbusReader.gy));
            barChart.DataSource.SetValue("z", "각속도", graph.Revise(modbusReader.gz));

            yield return wfs;
        }
    }
}
