using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Markup;
using ChartAndGraph;
using UnityEngine;

public class DrawGradGraph : MonoBehaviour
{
    [Header("Object References")]
    [SerializeField] GraphChart graphChart;
    public Graph graph;

    ModbusReader modbusReader;
    WaitForSeconds wfs;
    float updateTime;
    int queueSize;


    void Start()
    {
        Init();
        StartDraw();
    }

    void OnEnable()
    {
        StartDraw();
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void Init()
    {
        graphChart = GetComponent<GraphChart>();
        graph = transform.parent.parent.GetComponent<Graph>();
        modbusReader = graph.modbusReader;
        updateTime = graph.gradUpdateTime;
        queueSize = graph.queueSize;
        wfs = new WaitForSeconds(updateTime);
    }

    void StartDraw()
    {
        // if(transform.name == "AccelerationGraph")
        // {
        //     StartCoroutine(Draw("ax", "accel"));
        //     StartCoroutine(Draw("ay", "accel"));
        //     StartCoroutine(Draw("az", "accel"));
        //     StartCoroutine(Draw())
        // }
        // else if(transform.name == "AngularVelocityGraph")
        // {
        //     StartCoroutine(Draw("gx", "angular"));
        //     StartCoroutine(Draw("gy", "angular"));
        //     StartCoroutine(Draw("gz", "angular"));
        // }
        StartCoroutine(Draw("gx", "accel"));
    }

    /// <summary>
    /// 그래프를 그리는 코루틴 메소드
    /// 인자로 Graph Chart 스크립트를 포함한 게임 오브젝트의 카테고리 이름을 받아서
    /// 큐 자료형으로 데이터를 관리하고, 해당 카테고리에 데이터를 추가하고, 그래프를 그린다.
    /// </summary>
    public IEnumerator Draw(string categoryName, string dataType)
    {
        if(graphChart != null)
        {
            Queue<float> dataQueue = new Queue<float>(queueSize);

            // 그래프 초기화
            graphChart.DataSource.StartBatch();
            graphChart.DataSource.ClearCategory(categoryName);
            InitQueue(dataQueue, categoryName);

            while(true)
            {
                float value;
                graphChart.DataSource.ClearCategory(categoryName);

                // 그래프 업데이트
                dataQueue.Dequeue();
                
                if(dataType == "accel")
                {
                    // value = categoryName == "ax" ? graph.Revise(modbusReader.ax) :
                    //         categoryName == "ay" ? graph.Revise(modbusReader.ay) :
                    //         graph.Revise(modbusReader.az);
                    value = Mathf.Abs(graph.Revise(modbusReader.gx));
                    value += Mathf.Abs(graph.Revise(modbusReader.gy));
                    value += Mathf.Abs(graph.Revise(modbusReader.gz));
                }
                // else if(dataType == "angular")
                // {
                //     value = categoryName == "gx" ? graph.Revise(modbusReader.gx) :
                //             categoryName == "gy" ? graph.Revise(modbusReader.gy) :
                //             graph.Revise(modbusReader.gz);
                // }
                else
                {
                    print("지원하지 않는 데이터 유형입니다.");
                    yield break; // 지원하지 않는 데이터 유형일 경우 루프 중지
                }

                dataQueue.Enqueue(value);

                float[] arr = dataQueue.ToArray();
                for(int i = 0; i < queueSize; i++)
                {
                    graphChart.DataSource.AddPointToCategory(categoryName, i, arr[i]);
                }

                graphChart.DataSource.EndBatch();
                yield return wfs;
            }
        }
    }

    void InitQueue(Queue<float> accelData, string categoryName)
    {
        float value=0;
        if(categoryName == "accel") 
        {
            value = Mathf.Abs(graph.Revise(modbusReader.gx));
            value += Mathf.Abs(graph.Revise(modbusReader.gy));
            value += Mathf.Abs(graph.Revise(modbusReader.gz));
        }
        // else if(graphtype == "angular") 
        // {
        //     value = graphtype=="gx" ? graph.Revise(modbusReader.gx) : 
        //             graphtype=="gy" ? graph.Revise(modbusReader.gy) : 
        //             graph.Revise(modbusReader.gz);
        // }

        for(int i=0; i<queueSize; i++)
        {
            accelData.Enqueue(value);
            graphChart.DataSource.AddPointToCategory(categoryName, i, value);
        }
    }
}
