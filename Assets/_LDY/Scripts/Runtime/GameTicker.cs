using System;
using UnityEngine;

public class GameTicker : MonoBehaviour
{
    private IPlayerStats stats;
    private float tickInterval = 1f;
    private float elapsed;

    public event Action OnTick;

    public void Bind(IPlayerStats stats)
    {
        this.stats = stats;
        stats.OnStatsChanged += ReadInterval;
        ReadInterval();
    }

    private void OnDestroy()
    {
        if (stats != null) stats.OnStatsChanged -= ReadInterval;
    }

    private void ReadInterval() => tickInterval = Mathf.Max(0.01f, stats.TickInterval);

    private void Update()
    {
        elapsed += Time.deltaTime;
        while (elapsed >= tickInterval)
        {
            elapsed -= tickInterval;
            OnTick?.Invoke();
        }
    }
}
