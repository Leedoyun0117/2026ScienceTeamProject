public class ItemToggleService : IItemToggleService
{
    private readonly IPurchaseService purchase;
    private readonly IPowerGrid grid;

    public ItemToggleService(IPurchaseService purchase, IPowerGrid grid)
    {
        this.purchase = purchase;
        this.grid = grid;
    }

    public void Toggle(ItemData item)
    {
        if (!purchase.IsOwned(item)) return;

        if (grid.IsActive(item)) grid.Deactivate(item);
        else grid.Activate(item);
    }
}
