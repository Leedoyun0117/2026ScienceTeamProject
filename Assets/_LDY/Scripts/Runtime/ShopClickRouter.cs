public class ShopClickRouter
{
    private readonly IPurchaseService purchase;
    private readonly IItemToggleService toggle;
    private readonly IUpgradeService upgrade;

    public ShopClickRouter(IPurchaseService purchase, IItemToggleService toggle, IUpgradeService upgrade)
    {
        this.purchase = purchase;
        this.toggle = toggle;
        this.upgrade = upgrade;
    }

    public void OnItemClicked(ItemData item)
    {
        if (purchase.IsOwned(item)) toggle.Toggle(item);
        else purchase.TryPurchase(item);
    }

    public void OnUpgradeClicked(UpgradeData data) => upgrade.TryUpgrade(data);
}
