using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip bgm;
    [SerializeField] private AudioClip[] sfx;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PlayBGM();
    }

    public void PlayBGM()
    {
        bgmSource.loop = true;
        bgmSource.clip = bgm;
        bgmSource.Play();
        Debug.Log(bgm);
    }

    public void PlayCoin()
    {
        sfxSource.PlayOneShot(sfx[0]);
    }
    
    public void PlayPressd()
    {
        sfxSource.PlayOneShot(sfx[1]);
    }
    
    
    
}
