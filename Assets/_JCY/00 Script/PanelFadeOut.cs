using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PanelFadeOut : MonoBehaviour
{
    [SerializeField] private Image _panel;
    [SerializeField] private float _duration;

    private void Start()
    {
        _panel.DOFade(0, _duration);
    }

    public void Fade(int alpha)
    {
        //_panel.color.a = alpha;
    }
}
