using System;

public class PlayerStats : IPlayerStats, IPlayerStatsMutator
{
    public int PowerLimit { get; private set; }
    public float IncomeMultiplier { get; private set; }
    public float TickInterval { get; private set; }
    public int ClickIncome { get; private set; }

    public event Action OnStatsChanged;

    public PlayerStats(int powerLimit, float incomeMultiplier, float tickInterval, int clickIncome)
    {
        PowerLimit = powerLimit;
        IncomeMultiplier = incomeMultiplier;
        TickInterval = tickInterval;
        ClickIncome = clickIncome;
    }

    public void SetPowerLimit(int value)
    {
        if (PowerLimit == value) return;
        PowerLimit = value;
        OnStatsChanged?.Invoke();
    }

    public void SetIncomeMultiplier(float value)
    {
        if (UnityEngine.Mathf.Approximately(IncomeMultiplier, value)) return;
        IncomeMultiplier = value;
        OnStatsChanged?.Invoke();
    }

    public void SetTickInterval(float value)
    {
        if (UnityEngine.Mathf.Approximately(TickInterval, value)) return;
        TickInterval = value;
        OnStatsChanged?.Invoke();
    }

    public void SetClickIncome(int value)
    {
        if (ClickIncome == value) return;
        ClickIncome = value;
        OnStatsChanged?.Invoke();
    }
}
