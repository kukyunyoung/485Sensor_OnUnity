using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetSampleSound : MonoBehaviour
{
    AudioSource audioSource;
    void OnEnable()
    {
        audioSource = GetComponent<AudioSource>();
        int random = Random.Range(0,7);
        audioSource.clip = Resources.Load($"SampleSound/{random}") as AudioClip;
    }
}
