using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UploadTest : MonoBehaviour
{
    public DrawTorus torusGraph; // 테스트 시 스크린샷 찍을때만 끄고 시각적으로는 보여주기 위함
    public GameObject graph;
    public string filePath;
    public string label;
    public int goMethod;
    public bool manualNext = false;
    public Button prtBtn;
    public int captureTime;
    public TextMeshProUGUI captureText;
    
    FlaskClient flaskClient;
    string path;
    bool isCapture;
    bool isrunning = false;

    private void Start()
    {
        flaskClient = FindObjectOfType<FlaskClient>();
        prtBtn.onClick.AddListener(() => PrintBtn());
        isCapture = false;
    }


    // Update is called once per frame
    void Update()
    {
        switch (goMethod)
        {
            case 1:
                goMethod = 0;
                StartCoroutine(Upload());
                break;
            case 2:
                goMethod = 0;
                SearchByPath(filePath);
                break;
            case 3:
                goMethod = 0;
                StartCoroutine(UploadSound());
                break;
            case 4:
                goMethod = 0;
                SearchSoundByPath(filePath);
                break;
        }
    }

    public void PrintBtn()
    {
        if(!isrunning) 
        {
            StartCoroutine(ScreenshotSearch());
            isrunning = !isrunning;
        }
        else
        { 
            StopAllCoroutines();
            isrunning = !isrunning;
        }
    }

    IEnumerator ScreenshotSearch()
    {
        while(true)
        {
            yield return new WaitForSeconds(captureTime);
            if (!graph.activeSelf)
            {
                print("Graph is not active");
                isCapture = false;
                yield break;
            }

            //if (torusGraph.gameObject.activeSelf) torusGraph.gameObject.SetActive(false); // 이미지 검색에 방해되는 요소인 torus 그래프 비활성화

            path = $"Assets/screenshot/{System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.png";
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSeconds(0.2f);

            //if (!torusGraph.gameObject.activeSelf) torusGraph.gameObject.SetActive(true);

            Search();
            isCapture = false;

            File.Delete(path); // 사용한 스크린샷 삭제
        }
    }

    IEnumerator Upload()
    {
        string basepath = Application.dataPath + "/" + filePath;
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(label))
        {
            Debug.LogError("Please set file path and label");
            yield break;
        }
        if (Directory.Exists(basepath))
        {
            //basepath아래에 하위 폴더들을 찾는 코드
            string[] dirs = Directory.GetDirectories(basepath);
            Debug.Log("dirs.Length : " + dirs.Length);
            yield return new WaitUntil(() => manualNext);
            manualNext = false;
            for (int i = 0; i < dirs.Length; i++)
            {
                string[] files = Directory.GetFiles(dirs[i], "*.png");
                bool isnext = true;
                for (int j = 0; j < files.Length; j++)
                {
                    yield return new WaitUntil(() => isnext);
                    isnext = false;
                    string directoryPath = Path.GetDirectoryName(files[j]);
                    string directoryName = Path.GetFileName(directoryPath);
                    Debug.Log("directoryName : " + directoryName);
                    StartCoroutine(flaskClient.ImageTraining(files[j], directoryName, (res) =>
                    {
                        Debug.Log(res);
                        isnext = true;
                    }));
                }
            }
        }
        else
        {
            Debug.LogError("File not found : " + basepath);
        }
    }

    IEnumerator UploadSound()
    {
        string basepath = Application.dataPath + "/" + filePath;
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(label))
        {
            Debug.LogError("Please set file path and label");
            yield break;
        }
        if (Directory.Exists(basepath))
        {
            //basepath아래에 하위 폴더들을 찾는 코드
            string[] dirs = Directory.GetDirectories(basepath);
            Debug.Log("dirs.Length : " + dirs.Length);
            yield return new WaitUntil(() => manualNext);
            manualNext = false;
            for (int i = 0; i < dirs.Length; i++)
            {
                string[] files = Directory.GetFiles(dirs[i], "*.wav");
                bool isnext = true;
                for (int j = 0; j < files.Length; j++)
                {
                    yield return new WaitUntil(() => isnext);
                    isnext = false;
                    string directoryPath = Path.GetDirectoryName(files[j]);
                    string directoryName = Path.GetFileName(directoryPath);
                    Debug.Log("directoryName : " + directoryName);
                    StartCoroutine(flaskClient.SoundTraining(files[j], directoryName, (res) =>
                    {
                        Debug.Log(res);
                        isnext = true;
                    }));
                }
            }
        }
        else
        {
            Debug.LogError("File not found : " + basepath);
        }
    }

    void Search()
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Please set file path");
            return;
        }
        if (File.Exists(path))
        {
            StartCoroutine(flaskClient.ImageSearch(path, (res) =>
            {
                List<string> result = new List<string>(); // 0~4 : 이미지 검색 결과, 5 : 흔들림 상태
                result = JsonConvert.DeserializeObject<List<string>>(res);

                CompareDict(result, "오작동", "정상작동", "작동안함");        
            }));
        }
        else
        {
            Debug.LogError("File not found : " + path);
        }
    }


    void SearchByPath(string _path)
    {
        if (string.IsNullOrEmpty(_path))
        {
            Debug.LogError("Please set file _path");
            return;
        }
        if (File.Exists(_path))
        {
            StartCoroutine(flaskClient.ImageSearch(_path, (res) =>
            {
                Debug.Log(res);
            }));
        }
        else
        {
            Debug.LogError("File not found : " + _path);
        }
    }

    public void SearchSoundByPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Please set file path");
        }
        if (File.Exists(path))
        {
            StartCoroutine(flaskClient.SoundSearch(path, (res) =>
            {
                List<string> result = JsonConvert.DeserializeObject<List<string>>(res);
                result = JsonConvert.DeserializeObject<List<string>>(res);

                CompareDict(result, "망치소리", "아이발소리", "어른발소리");

                Debug.Log(res);
            }));
        }
        else
        {
            Debug.LogError("File not found : " + path);
        }
    }


    void CompareDict(List<string> result, string key1="", string key2="", string key3="")
    {
        Dictionary<string, int> weight = new Dictionary<string, int> { { key1, 0 }, { key2, 0 }, { key3, 0 } };

        // 이미지검색 결과값과 가중치 딕셔너리를 비교하여 가중치 증가
        foreach (string value in result)
            if (weight.ContainsKey(value)) weight[value]++;

        result.Add(CalcWeight(weight, key1));

        // 결과값
        captureText.text = key1=="오작동" ? "흔들림 탐지 결과" : "소음 탐지 결과";
        captureText.text += "\n기록시간 : " + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        captureText.text += "\n상태 : "+result[result.Count - 1];
        captureText.text += $"\n{key1} : {(float)weight[key1] / (float)(result.Count - 1)} / {key2} : {(float)weight[key2] / (float)(result.Count - 1)} / {key3} : {(float)weight[key3] / (float)(result.Count - 1)}";
    }

    // 가중치 계산 결과를 토대로 가장 높은 가중치를 가진 상태를 찾아내어 result에 추가
    string CalcWeight(Dictionary<string, int> weight, string key1)
    {
        string state = key1; // 기본값
        int maxCount = weight[state];

        foreach (var entry in weight)
        {
            if (entry.Value > maxCount)
            {
                state = entry.Key;
                maxCount = entry.Value;
            }
        }

        return state;
    }

    int[] soundCount;
    /// <summary>
    /// 폴더의 이름으로 폴더안에 있는 .wav 파일을 전부 검색하고 결과값을 출력
    /// path는 'Assets/testcase/소리/어른발소리' 와 같은 형식으로 입력
    /// </summary>
    public IEnumerator SearchSoundAuto(string path)
    {
        string[] dirs = Directory.GetFiles(path, "*.wav");
        soundCount = new int[3];

        for(int i=0; i< dirs.Length; i++)
        {
            SearchSoundByPath(dirs[i]);
            yield return new WaitForSeconds(0.5f);
        }

        print("#####################################");
        print("검색한 파일 수 : " + dirs.Length);
        print("망치소리 : " + soundCount[0]);
        print("아이발소리 : " + soundCount[1]);
        print("어른발소리 : " + soundCount[2]);
        print("정확도 : " + (float)(Math.Max(soundCount[0], Math.Max(soundCount[1], soundCount[2]))) / (float)(soundCount[0]+soundCount[1]+soundCount[2]) + "%");
    }
}
