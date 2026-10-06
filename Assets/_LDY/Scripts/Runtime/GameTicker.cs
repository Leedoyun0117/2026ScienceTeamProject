using System;
using UnityEngine;

public class GameTicker : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float tickInterval = 1f;

    private float elapsed;

    public event Action OnTick;

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
