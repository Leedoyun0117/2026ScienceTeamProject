using System;
using System.Collections.Generic;

public interface IPowerGrid
{
    int CurrentPower { get; }
    int Limit { get; }
    bool IsBlackout { get; }
    IReadOnlyCollection<ItemData> ActiveItems { get; }

    event Action<int, int> OnPowerChanged;
    event Action<bool> OnBlackoutChanged;
    event Action OnActiveItemsChanged;

    bool IsActive(ItemData item);
    bool WouldExceedLimit(ItemData item);
    void Activate(ItemData item);
    void Deactivate(ItemData item);
}
