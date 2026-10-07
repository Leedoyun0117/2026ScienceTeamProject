using System;
using UnityEngine;

public class IncomeForecast : IIncomeForecast
{
    private readonly IIncomeCalculator calculator;
    private readonly IPlayerStats stats;

    public int TickIncome { get; private set; }
    public float PerSecondIncome { get; private set; }
    public event Action<int> OnTickIncomeChanged;
    public event Action<float> OnPerSecondIncomeChanged;

    public IncomeForecast(IIncomeCalculator calculator, IPowerGrid grid, IPlayerStats stats)
    {
        this.calculator = calculator;
        this.stats = stats;
        TickIncome = calculator.CalculateTickIncome();
        PerSecondIncome = ToPerSecond(TickIncome);

        grid.OnActiveItemsChanged += Recalculate;
        grid.OnBlackoutChanged += _ => Recalculate();
        stats.OnStatsChanged += Recalculate;
    }

    private void Recalculate()
    {
        int income = calculator.CalculateTickIncome();
        float perSecond = ToPerSecond(income);

        if (income != TickIncome)
        {
            TickIncome = income;
            OnTickIncomeChanged?.Invoke(TickIncome);
        }

        if (!Mathf.Approximately(perSecond, PerSecondIncome))
        {
            PerSecondIncome = perSecond;
            OnPerSecondIncomeChanged?.Invoke(PerSecondIncome);
        }
    }

    private float ToPerSecond(int tickIncome) => tickIncome / stats.TickInterval;
}
