using UnityEngine;

public class IncomeCalculator : IIncomeCalculator
{
    private readonly IPowerGrid grid;

    public IncomeCalculator(IPowerGrid grid)
    {
        this.grid = grid;
    }

    public int CalculateTickIncome()
    {
        if (grid.IsBlackout) return 0;

        float total = 0f;
        foreach (var item in grid.ActiveItems) total += item.BaseIncome * item.IncomeMultiplier;
        return Mathf.RoundToInt(total);
    }
}
