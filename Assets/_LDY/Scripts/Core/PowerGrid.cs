using System;
using System.Collections.Generic;

public class PowerGrid : IPowerGrid
{
    private readonly HashSet<ItemData> activeItems = new HashSet<ItemData>();

    public int CurrentPower { get; private set; }
    public int Limit { get; }
    public bool IsBlackout { get; private set; }
    public IReadOnlyCollection<ItemData> ActiveItems => activeItems;

    public event Action<int, int> OnPowerChanged;
    public event Action<bool> OnBlackoutChanged;
    public event Action OnActiveItemsChanged;

    public PowerGrid(int limit)
    {
        Limit = limit;
    }

    public bool IsActive(ItemData item) => activeItems.Contains(item);

    public void Activate(ItemData item)
    {
        if (activeItems.Add(item)) Refresh();
    }

    public void Deactivate(ItemData item)
    {
        if (activeItems.Remove(item)) Refresh();
    }

    private void Refresh()
    {
        CurrentPower = 0;
        foreach (var item in activeItems) CurrentPower += item.PowerConsumption;

        OnPowerChanged?.Invoke(CurrentPower, Limit);

        bool blackout = CurrentPower > Limit;
        if (blackout != IsBlackout)
        {
            IsBlackout = blackout;
            OnBlackoutChanged?.Invoke(IsBlackout);
        }

        OnActiveItemsChanged?.Invoke();
    }
}
