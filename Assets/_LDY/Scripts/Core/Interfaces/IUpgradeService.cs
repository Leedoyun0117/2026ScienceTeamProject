using System;
using System.Collections.Generic;

public interface IUpgradeService
{
    IReadOnlyList<UpgradeData> Upgrades { get; }
    event Action<UpgradeData, int> OnLevelChanged;

    int GetLevel(UpgradeData data);
    bool IsMaxed(UpgradeData data);
    int GetCost(UpgradeData data);
    IUpgradeEffect GetEffect(UpgradeData data);
    bool TryUpgrade(UpgradeData data);
}
