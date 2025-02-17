using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class FlaskClient : MonoBehaviour
{
    private string baseUrl => isLocal ? "http://localhost:5147" : "http://13.124.245.38:5147";
    private const string questApi = "/api/quest";
    private const string runApi = "/api/run";
    private const string imgTrainingApi = "/api/imageTraining";
    private const string imgSeartchApi = "/api/imageSearch";
    private const string soundTrainingApi = "/api/soundTraining";
    private const string soundSeartchApi = "/api/soundSearch";
    public bool isLocal = false;
    Action<string> StremingAct = delegate { };
    Action<string> CompleteAct = delegate { };

    IEnumerator PostRequest(string uri, string jsonData)
    {
        var request = new UnityWebRequest(baseUrl + uri, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(request.error);
        }
        else
        {
            Debug.Log("Response: " + request.downloadHandler.text);
        }
    }

    IEnumerator GetRequest(string uri, Action<string> Callback = null)
    {
        var request = UnityWebRequest.Get(baseUrl + uri);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(request.error);
        }
        else
        {
            Debug.Log("Response: " + request.downloadHandler.text);
            Callback?.Invoke(request.downloadHandler.text);
        }
    }

    IEnumerator GetRunData(string runid, Action<string> Callback = null)
    {
        if (string.IsNullOrEmpty(runid))
        {
            yield break;
        }
        yield return GetRequest($"{runApi}?runid={runid}", Callback);
    }

    private IEnumerator StreamResponse(string question)
    {
        string url = $"{baseUrl + questApi}?quest={UnityWebRequest.EscapeURL(question)}";

        // Allocate a buffer for receiving chunks (e.g., 1 KB)
        byte[] buffer = new byte[1024];

        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
        {
            StreamingHandler handler = new StreamingHandler(buffer);
            request.downloadHandler = handler;

            handler.OnChunkReceived += HandleStreamingResponse;
            handler.OnCompleted += (string response) =>
            {
                CompleteAct(response);
            };

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {request.error}");
            }
        }
    }

    private void HandleStreamingResponse(string chunk)
    {
        // Handle and display each chunk of data as it arrives
        StremingAct(chunk);
        // Update the UI or process the chunk as needed
    }

    public IEnumerator ImageTraining(string imgpath, string label, Action<string> Callback)
    {
        string url = $"{baseUrl + imgTrainingApi}";
        // POST로 파일과 라벨을 전송
        WWWForm form = new WWWForm();
        form.AddField("label", label);
        // 이미지는 jpeg로 변환하여 전송
        form.AddBinaryData("image", System.IO.File.ReadAllBytes(imgpath), imgpath, "image/png");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {request.error}");
            }
            else
            {
                Callback?.Invoke(request.downloadHandler.text);
            }
        }
    }

    public IEnumerator ImageSearch(string imgpath, Action<string> Callback)
    {
        string url = $"{baseUrl + imgSeartchApi}";
        // POST로 파일을 전송
        WWWForm form = new WWWForm();
        form.AddBinaryData("image", System.IO.File.ReadAllBytes(imgpath), imgpath, "image/png");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {request.error}");
            }
            else
            {
                Callback?.Invoke(request.downloadHandler.text);
            }
        }
    }

    public IEnumerator SoundTraining(string soundpath, string label, Action<string> Callback)
    {
        string url = $"{baseUrl + soundTrainingApi}";
        // POST로 파일과 라벨을 전송
        WWWForm form = new WWWForm();
        form.AddField("label", label);
        // 사운드는 wav로 변환하여 전송
        form.AddBinaryData("sound", System.IO.File.ReadAllBytes(soundpath), soundpath, "audio/wav");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {request.error}");
            }
            else
            {
                Callback?.Invoke(request.downloadHandler.text);
            }
        }
    }

    public IEnumerator SoundSearch(string soundpath, Action<string> Callback)
    {
        string url = $"{baseUrl + soundSeartchApi}";
        // POST로 파일을 전송
        WWWForm form = new WWWForm();
        form.AddBinaryData("sound", System.IO.File.ReadAllBytes(soundpath), soundpath, "audio/wav");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {request.error}");
            }
            else
            {
                Callback?.Invoke(request.downloadHandler.text);
            }
        }
    }

}


public class StreamingHandler : DownloadHandlerScript
{
    private StringBuilder responseText = new StringBuilder();

    public event Action<string> OnChunkReceived;
    public event Action<string> OnCompleted;

    public StreamingHandler(byte[] buffer) : base(buffer)
    {
    }

    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        if (data == null || data.Length < 1)
        {
            return false;
        }

        // Convert received bytes to string
        string chunk = Encoding.UTF8.GetString(data, 0, dataLength);

        // Append received chunk
        responseText.Append(chunk);

        // Notify listeners about the new chunk
        OnChunkReceived?.Invoke(responseText.ToString());

        return true;
    }

    protected override void CompleteContent()
    {
        Debug.Log("Complete content received.");
        OnCompleted?.Invoke(responseText.ToString());
    }
}