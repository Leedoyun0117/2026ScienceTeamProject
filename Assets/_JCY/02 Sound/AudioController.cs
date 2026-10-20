using System;
using UnityEngine;
using UnityEngine.Audio;

public class AudioController : MonoBehaviour
{

    [SerializeField] private AudioMixer audioMixer;
    
    private void Start()
    {
        SetMasterVolume(0.3f);
        SetBgmVolume(0.3f);
        SetSfxVolume(0.3f);
    }
    
    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("Master",  Mathf.Log10(volume) * 20f);
    }
    
    public void SetBgmVolume(float volume)
    {
        audioMixer.SetFloat("BGM",Mathf.Log10(volume)* 20f);
    }
    
    public void SetSfxVolume(float volume)
    {
        audioMixer.SetFloat("SFX",Mathf.Log10(volume) * 20f);
    }
}
