using UnityEngine;

public class IncomeIntervalEffect : IUpgradeEffect
{
    private readonly IPlayerStatsMutator stats;
    private readonly float baseInterval;
    private readonly float minInterval;

    public UpgradeType Type => UpgradeType.IncomeInterval;

    public IncomeIntervalEffect(IPlayerStatsMutator stats, float baseInterval, float minInterval)
    {
        this.stats = stats;
        this.baseInterval = baseInterval;
        this.minInterval = minInterval;
    }

    public float ValueAt(UpgradeData data, int level) =>
        Mathf.Max(minInterval, baseInterval * Mathf.Pow(data.EffectPerLevel, level));

    public void Apply(UpgradeData data, int level) => stats.SetTickInterval(ValueAt(data, level));

    public string Format(float value) => $"주기 {value:0.00}초";
}
