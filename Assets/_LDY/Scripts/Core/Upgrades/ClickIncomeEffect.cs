using UnityEngine;

public class ClickIncomeEffect : IUpgradeEffect
{
    private readonly IPlayerStatsMutator stats;
    private readonly int baseClickIncome;

    public UpgradeType Type => UpgradeType.ClickIncome;

    public ClickIncomeEffect(IPlayerStatsMutator stats, int baseClickIncome)
    {
        this.stats = stats;
        this.baseClickIncome = baseClickIncome;
    }

    public float ValueAt(UpgradeData data, int level) => baseClickIncome + data.EffectPerLevel * level;

    public void Apply(UpgradeData data, int level) => stats.SetClickIncome(Mathf.RoundToInt(ValueAt(data, level)));

    public string Format(float value) => $"클릭 +{Mathf.RoundToInt(value)}";
}
