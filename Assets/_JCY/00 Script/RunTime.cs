using System;
using System.Collections;
using UnityEngine;

public class RunTime : MonoBehaviour
{
    // [field:SerializeField] private struct Setting  
    // {
    //     public GameObject[] _backGrounds = new GameObject[];
    //     public string name;
    //     
    //     public 
    // }
    public GameObject[] _backGrounds;
    [SerializeField] private float _timeSpeed;
    private int _currentIndex = 0;
    private PanelFadeOut fadeOut;

    // [field:SerializeField]
    // private enum _currentTime
    // {
    //     Night,
    //     sunsetNight,
    //     sunset,
    //     day
    // }
    //

    private void Awake()
    {
        fadeOut = GetComponentInChildren<PanelFadeOut>();
    }

    private void Start()
    {
        foreach (var back in _backGrounds)
        {
            back.SetActive(false);
        }
        _backGrounds[0].SetActive(true);

        StartCoroutine(MoveTime());
    }

    public IEnumerator MoveTime()
    {
        yield return new WaitForSeconds(_timeSpeed);
        _backGrounds[_currentIndex].SetActive(false);
        _currentIndex += 1;
        
        if (_currentIndex == _backGrounds.Length)
            _currentIndex = 0;
        
        _backGrounds[_currentIndex].SetActive(true);
        fadeOut.Fade();
        StartCoroutine(MoveTime());
    }
}
