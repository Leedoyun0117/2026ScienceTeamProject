using System;

public class Wallet : IWallet
{
    public int Money { get; private set; }
    public event Action<int> OnMoneyChanged;

    public Wallet(int initialMoney)
    {
        Money = initialMoney;
    }

    public bool CanAfford(int amount) => Money >= amount;

    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount)) return false;
        Money -= amount;
        OnMoneyChanged?.Invoke(Money);
        return true;
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        Money += amount;
        OnMoneyChanged?.Invoke(Money);
    }
}
