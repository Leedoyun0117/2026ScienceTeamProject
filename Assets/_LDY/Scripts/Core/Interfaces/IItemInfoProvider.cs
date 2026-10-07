using System;

public interface IItemInfoProvider
{
    event Action OnChanged;
    ItemInfo GetInfo(ItemData item);
}
