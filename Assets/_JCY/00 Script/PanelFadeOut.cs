using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sequence = DG.Tweening.Sequence;

public class PanelFadeOut : MonoBehaviour
{
    [SerializeField] private Image _panel;
    [SerializeField] private float _duration;

    private void Start()
    {
        _panel.DOFade(0, _duration);
    }

    public void Fade()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Append(_panel.DOFade(0.8f, 0.0001f));
            sequence.Append(_panel.DOFade(0, _duration));
    }
}
