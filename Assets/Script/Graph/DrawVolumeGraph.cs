using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using ChartAndGraph;
using UnityEngine;

public class DrawVolumeGraph : MonoBehaviour
{
    [Header("Object References")]
    [SerializeField] GraphChart graphChart;
    public Graph graph;

    RecordMic recordMic;
    public AudioSource audioSource;
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
        recordMic = graph.recordMic;
        updateTime = graph.volumeUpdateTime;
        queueSize = graph.queueSize;
        wfs = new WaitForSeconds(updateTime);
    }

    void StartDraw()
    {
        if(transform.name == "InputVolumeGraph")
        {
            StartCoroutine(Draw("Value"));
        }
        else if(transform.name == "SampleVolumeGraph")
        {
            StartCoroutine(Draw("Value", true));
        }
        else if(transform.name == "CompareVolumeGraph")
        {
            StartCoroutine(Draw("InputValue"));
            StartCoroutine(Draw("SampleValue", true));
        }
    }

    /// <summary>
    /// 그래프를 그리는 코루틴 메소드
    /// 인자로 Graph Chart 스크립트를 포함한 게임 오브젝트의 카테고리 이름을 받아서
    /// 큐 자료형으로 데이터를 관리하고, 해당 카테고리에 데이터를 추가하고, 그래프를 그린다.
    /// </summary>
    public IEnumerator Draw(string categoryName, bool hasSample = false)
    {
        if(graphChart != null)
        {
            Queue<float> dataQueue = new Queue<float>(queueSize); 
            int count = 0;
            string json = "";
            SaveData saveData = new SaveData();

            if(hasSample)
            {
                json = File.ReadAllText(Application.dataPath+"/SoundDataset/샘플사운드/"+ Random.Range(0, graph.sampleLength) + ".json");
                saveData = JsonUtility.FromJson<SaveData>(json);
            }

            // 그래프 초기화
            graphChart.DataSource.StartBatch();
            graphChart.DataSource.ClearCategory(categoryName);
            InitQueue(dataQueue, categoryName);

            audioSource = recordMic.audioSource;

            while(true)
            {
                if(count >= saveData.data.Count) count = 0;
                graphChart.DataSource.ClearCategory(categoryName);

                // 그래프 업데이트
                dataQueue.Dequeue();
                
                float volume;
                if(!hasSample) volume = GetAverageVolume()>recordMic.cutValue ? GetAverageVolume() : 0;
                else volume = float.Parse(saveData.data[count++]);
            
                dataQueue.Enqueue(volume);

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

        for(int i=0; i<queueSize; i++)
        {
            accelData.Enqueue(value);
            graphChart.DataSource.AddPointToCategory(categoryName, i, value);
        }
    }

    float GetAverageVolume()
    {
        float[] data = new float[recordMic.sampleWindow];
        float total = 0;

        // 오디오 소스에서 현재 샘플을 가져옴
        audioSource.GetOutputData(data, 0);

        // 음량 계산 (절댓값의 합)
        foreach (var sample in data)
        {
            total += Mathf.Abs(sample);
        }

        total *= recordMic.modulate;  // 음량 조절

        // 평균 음량 반환
        return total / recordMic.sampleWindow;
    }
}
