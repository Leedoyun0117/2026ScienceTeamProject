using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class LDY_UISlideIn : MonoBehaviour
{
    public enum FromDirection { Top, Bottom, Left, Right }

    [SerializeField] private FromDirection fromDirection = FromDirection.Top;
    [SerializeField] private float distance = 200f;
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private bool fadeAlso = true;
    [SerializeField] private bool playOnEnable = true;

    private RectTransform rt;
    private CanvasGroup canvasGroup;
    private Vector2 targetPos;
    private Coroutine playing;
    private bool initialized;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (initialized) return;
        initialized = true;

        rt = (RectTransform)transform;
        targetPos = rt.anchoredPosition;

        if (fadeAlso)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        Init();
        if (playOnEnable) Play();
    }

    public void Play()
    {
        if (!gameObject.activeInHierarchy) return;
        if (playing != null) StopCoroutine(playing);
        playing = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        Vector2 startPos = targetPos + GetOffset();
        rt.anchoredPosition = startPos;
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 3f); 
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, k);
            if (canvasGroup != null) canvasGroup.alpha = k;
            yield return null;
        }

        rt.anchoredPosition = targetPos;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        playing = null;
    }

    private Vector2 GetOffset()
    {
        switch (fromDirection)
        {
            case FromDirection.Top: return new Vector2(0f, distance);
            case FromDirection.Bottom: return new Vector2(0f, -distance);
            case FromDirection.Left: return new Vector2(-distance, 0f);
            case FromDirection.Right: return new Vector2(distance, 0f);
            default: return Vector2.zero;
        }
    }
}