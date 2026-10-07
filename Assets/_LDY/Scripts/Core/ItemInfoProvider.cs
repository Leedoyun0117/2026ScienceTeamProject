using System;

public class ItemInfoProvider : IItemInfoProvider
{
    private readonly IWallet wallet;
    private readonly IPurchaseService purchase;
    private readonly IPowerGrid grid;
    private readonly IIncomeCalculator calculator;

    public event Action OnChanged;

    public ItemInfoProvider(IWallet wallet, IPurchaseService purchase, IPowerGrid grid,
        IIncomeCalculator calculator, IPlayerStats stats)
    {
        this.wallet = wallet;
        this.purchase = purchase;
        this.grid = grid;
        this.calculator = calculator;

        wallet.OnMoneyChanged += _ => RaiseChanged();
        purchase.OnItemPurchased += _ => RaiseChanged();
        grid.OnActiveItemsChanged += RaiseChanged;
        grid.OnPowerChanged += (_, __) => RaiseChanged();
        stats.OnStatsChanged += RaiseChanged;
    }

    public ItemInfo GetInfo(ItemData item)
    {
        ItemCardState state;
        if (purchase.IsOwned(item)) state = grid.IsActive(item) ? ItemCardState.On : ItemCardState.Off;
        else state = wallet.CanAfford(item.Price) ? ItemCardState.Purchasable : ItemCardState.Unaffordable;

        return new ItemInfo(state, calculator.CalculateItemIncome(item), grid.WouldExceedLimit(item));
    }

    private void RaiseChanged() => OnChanged?.Invoke();
}
