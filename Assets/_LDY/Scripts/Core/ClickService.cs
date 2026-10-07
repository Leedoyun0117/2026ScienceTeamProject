using System;

public class ClickService : IClickService
{
    private readonly IWallet wallet;
    private readonly IPlayerStats stats;
    private readonly IPowerGrid grid;

    public event Action<int> OnClicked;

    public ClickService(IWallet wallet, IPlayerStats stats, IPowerGrid grid)
    {
        this.wallet = wallet;
        this.stats = stats;
        this.grid = grid;
    }

    public void Click()
    {
        if (grid.IsBlackout) return;

        int amount = stats.ClickIncome;
        if (amount <= 0) return;

        wallet.Add(amount);
        OnClicked?.Invoke(amount);
    }
}
