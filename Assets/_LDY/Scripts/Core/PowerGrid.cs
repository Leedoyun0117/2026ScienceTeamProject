using System;
using System.Collections.Generic;

public class PowerGrid : IPowerGrid
{
    private readonly IPlayerStats stats;
    private readonly HashSet<ItemData> activeItems = new HashSet<ItemData>();

    public int CurrentPower { get; private set; }
    public int Limit => stats.PowerLimit;
    public bool IsBlackout { get; private set; }
    public IReadOnlyCollection<ItemData> ActiveItems => activeItems;

    public event Action<int, int> OnPowerChanged;
    public event Action<bool> OnBlackoutChanged;
    public event Action OnActiveItemsChanged;

    public PowerGrid(IPlayerStats stats)
    {
        this.stats = stats;
        stats.OnStatsChanged += Recalculate;
    }

    public bool IsActive(ItemData item) => activeItems.Contains(item);

    public bool WouldExceedLimit(ItemData item) =>
        !activeItems.Contains(item) && CurrentPower + item.PowerConsumption > Limit;

    public void Activate(ItemData item)
    {
        if (!activeItems.Add(item)) return;
        Recalculate();
        OnActiveItemsChanged?.Invoke();
    }

    public void Deactivate(ItemData item)
    {
        if (!activeItems.Remove(item)) return;
        Recalculate();
        OnActiveItemsChanged?.Invoke();
    }

    private void Recalculate()
    {
        CurrentPower = 0;
        foreach (var item in activeItems) CurrentPower += item.PowerConsumption;

        OnPowerChanged?.Invoke(CurrentPower, Limit);

        bool blackout = CurrentPower > Limit;
        if (blackout == IsBlackout) return;

        IsBlackout = blackout;
        OnBlackoutChanged?.Invoke(IsBlackout);
    }
}
