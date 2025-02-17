using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class SaveData
{
    public List<string> data = new List<string>();
}

public class MakeSample : MonoBehaviour
{
    [Header("SavePath : Assets/ 뒷부분부터 입력")]
    public string savePath;
    public int sampleLength=10;
    public int filename=0;

    public Graph graph;
    public RecordMic recordMic;
    
    AudioSource sampleClip;
    float startTime;
    bool isRecording = false;
    string path;
    List<float> volumeList;
    int listSize;

    void Start()
    {
        sampleClip = GetComponent<AudioSource>();
        listSize = (int)(sampleLength / graph.volumeUpdateTime);
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.F9))
        {
            if(isRecording) return;
            isRecording = true;
            StartRecording();
        }
        if(Input.GetKeyDown(KeyCode.F10))
        {
            Microphone.End(null);
            isRecording = false;
            StopAllCoroutines();
        }
    }

    void StartRecording()
    {
        if(Microphone.IsRecording(null)) return;

        sampleClip.clip = Microphone.Start(null, true, sampleLength, 44100);
        startTime = Time.time;

        sampleClip.loop = true;
        while(!(Microphone.GetPosition(null) > 0)){} // 마이크 입력이 시작될 때까지 대기
        sampleClip.Play();
        StartCoroutine(MakeList());
    }

    void StopRecording()
    {
        Microphone.End(null);
        sampleClip.Stop();
    }

    void SaveRecording()
    {
        if(Microphone.IsRecording(null)) return;

        SaveData saveData = new SaveData();

        for(int i=0; i<listSize; i++)
        {
            saveData.data.Add(volumeList[i].ToString());
        }

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(Application.dataPath+"/"+savePath +"/"+ filename + ".json", json);

        path = Application.dataPath+"/"+savePath +"/"+ filename++ + ".wav";
        SavWav.Save(path, sampleClip.clip);
    }

    IEnumerator MakeList()
    {
        volumeList = new List<float>(listSize);

        for(int i=0; i<listSize; i++)
        {
            float volume = GetAverageVolume()>recordMic.cutValue ? GetAverageVolume() : 0;
            volumeList.Add(volume);

            yield return new WaitForSeconds(graph.volumeUpdateTime);
        }

        StopRecording();
        SaveRecording();
        StartRecording();
    }

    float GetAverageVolume()
    {
        float[] data = new float[recordMic.sampleWindow];
        float total = 0;

        // 오디오 소스에서 현재 샘플을 가져옴
        sampleClip.GetOutputData(data, 0);

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
