using System.Collections;
using System.IO;
using ChartAndGraph;
using UnityEngine;
using UnityEngine.UI;

public class RecordMic : MonoBehaviour
{
    public Button recBtn;
    [Header("Mic Record References")]
    public AudioSource audioSource;  // 오디오 소스 (마이크 입력을 받음)
    public UploadTest uploadTest; 
    public int sampleWindow = 128;  // 샘플 데이터 크기
    public int maxRecordingTime = 3; // 녹음 시간 3초
    public float recordingStartTime; // 녹음 시작 시간
    string path;
    int count;
    bool isRecording = false;

    [Header("Mic Graph References")]
    public GraphChart inputVolumeGraph;
    public GraphChart sampleVolumeGraph;
    public GraphChart compareVolumeGraph;
    public BarChart barChart;          // UI 텍스트에 실시간 음량 표시
    public float modulate;
    public float cutValue;
    public int queueLength;
    public float updateTime;

    WaitForSeconds wfs;

    void Start()
    {
        recBtn.onClick.AddListener(() => RecOnOff());
        wfs = new WaitForSeconds(updateTime);
    }

    void RecOnOff()
    {
        if(Microphone.devices.Length == 0)
        {
            print("마이크가 없습니다.");
            return; // 마이크가 없으면 녹음 불가능
        }
        
        if(!isRecording) 
        {
            isRecording = !isRecording;
            StartRecording();
        }
        else 
        {
            isRecording = !isRecording;
            StopRecording();
        }
    }

    void FixedUpdate()
    {
        if(!isRecording) return;
        // 실시간으로 음량 표시
        float volume = GetAverageVolume()>cutValue ? GetAverageVolume() : 0;
        barChart.DataSource.SetValue("음량", "All",volume);

        // 3초가 지나면 녹음 종료
        if (Time.time - recordingStartTime >= maxRecordingTime)
        {
            StopRecording();    // 녹음 중지
            SendRecording();    // 녹음한 오디오 소스를 WAV 파일로 저장하고 AI에게 전달
            StartRecording();   // 녹음 다시 시작
        }
    }

    // 녹음 시작 함수
    void StartRecording()
    {
        // 마이크로부터 입력 시작 (기본 마이크 사용, 3초 녹음, 루프, 무한 시간)
        audioSource.clip = Microphone.Start(null, true, maxRecordingTime, 44100);
        recordingStartTime = Time.time; // 녹음 시작 시간 기록

        // 오디오 소스 재생 시작
        audioSource.loop = true; // 오디오 소스 루프
        while (!(Microphone.GetPosition(null) > 0)) {} // 마이크가 데이터를 받을 때까지 대기
        audioSource.Play();
        Debug.Log("녹음 시작");
    }

    // 평균 음량 계산
    float GetAverageVolume()
    {
        float[] data = new float[sampleWindow];
        float total = 0;

        // 오디오 소스에서 현재 샘플을 가져옴
        audioSource.GetOutputData(data, 0);

        // 음량 계산 (절댓값의 합)
        foreach (var sample in data)
        {
            total += Mathf.Abs(sample);
        }

        total *= modulate;  // 음량 조절

        // 평균 음량 반환
        return total / sampleWindow;
    }

    // 녹음 중지
    void StopRecording()
    {
        barChart.DataSource.SetValue("음량", "All",0);
        
        Microphone.End(null); // 마이크 사용 종료
        audioSource.Stop();    // 오디오 소스 정지
        Debug.Log("녹음 종료");
    }

    void SendRecording()
    {
        path = $"{Application.dataPath}/testcase/마이크소리/{++count}.wav";
        SavWav.Save(path, audioSource.clip); // 녹음한 오디오 소스를 WAV 파일로 저장
        print("complete recording");

        uploadTest.SearchSoundByPath(path); // 인공지능으로 판별

        File.Delete(path); // 사용한 파일 삭제, 카운트 증가
    }
}
