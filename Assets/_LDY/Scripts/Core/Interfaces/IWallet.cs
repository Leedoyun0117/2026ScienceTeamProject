using System;

public interface IWallet
{
    int Money { get; }
    event Action<int> OnMoneyChanged;
    bool CanAfford(int amount);
    bool TrySpend(int amount);
    void Add(int amount);
}
