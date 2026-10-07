using UnityEngine;

public class IncomeCalculator : IIncomeCalculator
{
    private readonly IPowerGrid grid;
    private readonly IPlayerStats stats;

    public IncomeCalculator(IPowerGrid grid, IPlayerStats stats)
    {
        this.grid = grid;
        this.stats = stats;
    }

    public int CalculateTickIncome()
    {
        if (grid.IsBlackout) return 0;

        float total = 0f;
        foreach (var item in grid.ActiveItems) total += RawIncome(item);
        return Mathf.RoundToInt(total);
    }

    public int CalculateItemIncome(ItemData item) => Mathf.RoundToInt(RawIncome(item));

    private float RawIncome(ItemData item) => item.BaseIncome * item.IncomeMultiplier * stats.IncomeMultiplier;
}
