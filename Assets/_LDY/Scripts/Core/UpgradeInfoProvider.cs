using System;

public class UpgradeInfoProvider : IUpgradeInfoProvider
{
    private readonly IWallet wallet;
    private readonly IUpgradeService service;

    public event Action OnChanged;

    public UpgradeInfoProvider(IWallet wallet, IUpgradeService service)
    {
        this.wallet = wallet;
        this.service = service;

        wallet.OnMoneyChanged += _ => RaiseChanged();
        service.OnLevelChanged += (_, __) => RaiseChanged();
    }

    public UpgradeInfo GetInfo(UpgradeData data)
    {
        int level = service.GetLevel(data);
        bool isMax = service.IsMaxed(data);
        int cost = service.GetCost(data);
        var effect = service.GetEffect(data);

        string current = effect.Format(effect.ValueAt(data, level));
        string next = isMax ? current : effect.Format(effect.ValueAt(data, level + 1));

        return new UpgradeInfo(level, data.MaxLevel, cost, isMax, wallet.CanAfford(cost), current, next);
    }

    private void RaiseChanged() => OnChanged?.Invoke();
}
