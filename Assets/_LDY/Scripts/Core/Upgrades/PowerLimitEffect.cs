using UnityEngine;

public class PowerLimitEffect : IUpgradeEffect
{
    private readonly IPlayerStatsMutator stats;
    private readonly int baseLimit;

    public UpgradeType Type => UpgradeType.PowerLimit;

    public PowerLimitEffect(IPlayerStatsMutator stats, int baseLimit)
    {
        this.stats = stats;
        this.baseLimit = baseLimit;
    }

    public float ValueAt(UpgradeData data, int level) => baseLimit + data.EffectPerLevel * level;

    public void Apply(UpgradeData data, int level) => stats.SetPowerLimit(Mathf.RoundToInt(ValueAt(data, level)));

    public string Format(float value) => $"한도 {Mathf.RoundToInt(value)}";
}
