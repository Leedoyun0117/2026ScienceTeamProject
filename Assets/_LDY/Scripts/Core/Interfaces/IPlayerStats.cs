using System;

public interface IPlayerStats
{
    int PowerLimit { get; }
    float IncomeMultiplier { get; }
    float TickInterval { get; }
    int ClickIncome { get; }
    event Action OnStatsChanged;
}
