using System;
using System.Collections.Generic;

public class PurchaseService : IPurchaseService
{
    private readonly IWallet wallet;
    private readonly IPowerGrid grid;
    private readonly HashSet<ItemData> owned = new HashSet<ItemData>();

    public event Action<ItemData> OnItemPurchased;

    public PurchaseService(IWallet wallet, IPowerGrid grid)
    {
        this.wallet = wallet;
        this.grid = grid;
    }

    public bool IsOwned(ItemData item) => owned.Contains(item);

    public bool TryPurchase(ItemData item)
    {
        if (item == null || owned.Contains(item)) return false;
        if (!wallet.TrySpend(item.Price)) return false;

        owned.Add(item);
        grid.Activate(item);
        OnItemPurchased?.Invoke(item);
        return true;
    }
}
