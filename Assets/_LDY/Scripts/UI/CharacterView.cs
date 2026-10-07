using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ReactionMotion
{
    None,
    Breathe,
    Bounce,
    Shake,
    Sink,
}

[Serializable]
public class ReactionVisual
{
    [SerializeField] private ReactionType type;
    [SerializeField] private Sprite sprite;
    [SerializeField] private Color tint = Color.white;
    [SerializeField] private ReactionMotion motion;
    [SerializeField, Min(0f)] private float duration;

    public ReactionType Type => type;
    public Sprite Sprite => sprite;
    public Color Tint => tint;
    public ReactionMotion Motion => motion;
    public float Duration => duration;
}

public class CharacterView : MonoBehaviour
{
    [SerializeField] private Image characterImage;
    [SerializeField] private ReactionVisual[] visuals;
    [SerializeField] private float motionAmplitude = 24f;

    private readonly Dictionary<ReactionType, ReactionVisual> lookup = new Dictionary<ReactionType, ReactionVisual>();
    private IReactionResolver resolver;
    private RectTransform rect;
    private Vector2 basePosition;
    private Vector3 baseScale;
    private ReactionVisual active;
    private float elapsed;

    public void Bind(IReactionResolver resolver)
    {
        this.resolver = resolver;
        rect = characterImage.rectTransform;
        basePosition = rect.anchoredPosition;
        baseScale = rect.localScale;
        foreach (var visual in visuals) lookup[visual.Type] = visual;

        resolver.OnReaction += Play;
        Play(resolver.Current);
    }

    private void OnDestroy()
    {
        if (resolver != null) resolver.OnReaction -= Play;
    }

    private void Play(ReactionType type)
    {
        if (!lookup.TryGetValue(type, out var visual)) return;

        active = visual;
        elapsed = 0f;
        characterImage.sprite = visual.Sprite;
        characterImage.color = visual.Tint;
    }

    private void Update()
    {
        if (active == null) return;

        elapsed += Time.deltaTime;
        Animate();

        if (active.Duration > 0f && elapsed >= active.Duration) resolver.Complete(active.Type);
    }

    private void Animate()
    {
        var position = basePosition;
        var scale = baseScale;

        switch (active.Motion)
        {
            case ReactionMotion.Breathe:
                scale *= 1f + 0.03f * Mathf.Sin(elapsed * 2f);
                break;
            case ReactionMotion.Bounce:
                position.y += Mathf.Abs(Mathf.Sin(elapsed * 10f)) * motionAmplitude;
                break;
            case ReactionMotion.Shake:
                position.x += Mathf.Sin(elapsed * 50f) * motionAmplitude * 0.25f;
                break;
            case ReactionMotion.Sink:
                position.y -= motionAmplitude * 0.5f;
                scale *= 0.95f;
                break;
        }

        rect.anchoredPosition = position;
        rect.localScale = scale;
    }
}
