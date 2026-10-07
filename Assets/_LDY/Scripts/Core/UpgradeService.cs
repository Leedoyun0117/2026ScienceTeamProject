using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeService : IUpgradeService
{
    private readonly IWallet wallet;
    private readonly Dictionary<UpgradeType, IUpgradeEffect> effects = new Dictionary<UpgradeType, IUpgradeEffect>();
    private readonly Dictionary<UpgradeData, int> levels = new Dictionary<UpgradeData, int>();
    private readonly List<UpgradeData> upgrades = new List<UpgradeData>();

    public IReadOnlyList<UpgradeData> Upgrades => upgrades;
    public event Action<UpgradeData, int> OnLevelChanged;

    public UpgradeService(IWallet wallet, IEnumerable<IUpgradeEffect> effects, IEnumerable<UpgradeData> candidates)
    {
        this.wallet = wallet;
        foreach (var effect in effects) this.effects[effect.Type] = effect;

        var registeredTypes = new HashSet<UpgradeType>();
        foreach (var data in candidates)
        {
            if (data == null) continue;

            if (!this.effects.ContainsKey(data.Type))
            {
                Debug.LogWarning($"[Upgrade] '{data.name}' ignored: no effect registered for {data.Type}.");
                continue;
            }

            if (!registeredTypes.Add(data.Type))
            {
                Debug.LogWarning($"[Upgrade] '{data.name}' ignored: another {data.Type} upgrade is already registered.");
                continue;
            }

            upgrades.Add(data);
            levels[data] = 0;
        }
    }

    public int GetLevel(UpgradeData data) => levels.TryGetValue(data, out int level) ? level : 0;

    public bool IsMaxed(UpgradeData data) => GetLevel(data) >= data.MaxLevel;

    public int GetCost(UpgradeData data) => data.CostAt(GetLevel(data));

    public IUpgradeEffect GetEffect(UpgradeData data) => effects[data.Type];

    public bool TryUpgrade(UpgradeData data)
    {
        if (data == null || !levels.TryGetValue(data, out int level)) return false;
        if (IsMaxed(data)) return false;
        if (!wallet.TrySpend(GetCost(data))) return false;

        levels[data] = ++level;
        GetEffect(data).Apply(data, level);
        OnLevelChanged?.Invoke(data, level);
        return true;
    }
}
