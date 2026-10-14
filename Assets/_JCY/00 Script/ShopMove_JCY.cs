using System;
using DG.Tweening;
using UnityEngine;

public class ShopMove_JCY : MonoBehaviour
{
    [SerializeField] private bool display = true;
    [SerializeField] private float range;
    private RectTransform rtf;
    [SerializeField] private float _duration;

    private void Awake()
    {
        rtf = GetComponent<RectTransform>();
    }

    public void Move()
    {
        if (!display)
        {
            display = !display;
            rtf.DOAnchorPosY(rtf.anchoredPosition.y - range, _duration).SetEase(Ease.InOutBack);
        }
        else if (display)
        {
            display = !display;
            rtf.DOAnchorPosY(rtf.anchoredPosition.y + range, _duration).SetEase(Ease.InOutBack);
        }
    }
}
