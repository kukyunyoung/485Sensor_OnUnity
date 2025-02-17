using System.Collections;
using System.Collections.Generic;
using ChartAndGraph;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonMgr : MonoBehaviour
{
    public TextMeshProUGUI uploadResultText;
    
    [Header("Graph Type References")]
    public GameObject showGradGraph;
    public GameObject showRecGraph;

    [Header("Button references")]
    public Button connectBtn;
    public Button disconnectBtn;
    public Button showGradBtn;
    public Button recBtn;

    void Start()
    {
        showGradBtn.onClick.AddListener(() => ShowGrad());
        recBtn.onClick.AddListener(() => ShowRec());
    }

    void ShowGrad()
    {
        connectBtn.gameObject.SetActive(!connectBtn.gameObject.activeSelf);
        disconnectBtn.gameObject.SetActive(!disconnectBtn.gameObject.activeSelf);
        recBtn.gameObject.SetActive(!recBtn.gameObject.activeSelf);
        showGradGraph.SetActive(!showGradGraph.activeSelf);
        uploadResultText.text = "";
    }

    void ShowRec()
    {
        connectBtn.gameObject.SetActive(!connectBtn.gameObject.activeSelf);
        disconnectBtn.gameObject.SetActive(!disconnectBtn.gameObject.activeSelf);
        showGradBtn.gameObject.SetActive(!recBtn.gameObject.activeSelf);
        showRecGraph.SetActive(!showRecGraph.activeSelf);
        uploadResultText.text = "";
    }
}
