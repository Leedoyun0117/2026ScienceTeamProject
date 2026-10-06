using System;

public interface IPurchaseService
{
    event Action<ItemData> OnItemPurchased;
    bool IsOwned(ItemData item);
    bool TryPurchase(ItemData item);
}
