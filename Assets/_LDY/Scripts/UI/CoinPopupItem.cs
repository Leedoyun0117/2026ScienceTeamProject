using TMPro;
using UnityEngine;

public class CoinPopupItem : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField, Min(0.1f)] private float lifetime = 0.9f;
    [SerializeField] private float riseDistance = 90f;

    private RectTransform rect;
    private Vector2 start;
    private float elapsed;

    public void Play(string text, Vector2 startPosition)
    {
        rect = (RectTransform)transform;
        start = startPosition;
        rect.anchoredPosition = start;
        label.text = text;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);
        rect.anchoredPosition = start + Vector2.up * (riseDistance * t);
        label.alpha = 1f - t;

        if (t >= 1f) Destroy(gameObject);
    }
}
