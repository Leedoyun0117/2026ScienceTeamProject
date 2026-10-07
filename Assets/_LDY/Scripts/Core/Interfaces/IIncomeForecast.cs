using System;

public interface IIncomeForecast
{
    int TickIncome { get; }
    float PerSecondIncome { get; }
    event Action<int> OnTickIncomeChanged;
    event Action<float> OnPerSecondIncomeChanged;
}
