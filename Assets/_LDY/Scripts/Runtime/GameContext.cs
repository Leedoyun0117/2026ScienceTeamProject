using System.Collections.Generic;

public class GameContext
{
    public IPlayerStats Stats { get; set; }
    public IWallet Wallet { get; set; }
    public IPowerGrid Grid { get; set; }
    public IIncomeCalculator IncomeCalculator { get; set; }
    public IIncomeForecast IncomeForecast { get; set; }
    public IIncomePayout Payout { get; set; }
    public IClickService Click { get; set; }
    public IPurchaseService Purchase { get; set; }
    public IItemToggleService Toggle { get; set; }
    public IUpgradeService Upgrade { get; set; }
    public IItemInfoProvider ItemInfo { get; set; }
    public IUpgradeInfoProvider UpgradeInfo { get; set; }
    public IReactionResolver Reaction { get; set; }
    public IReadOnlyList<ItemData> Items { get; set; }
    public float CautionThreshold { get; set; }
}
