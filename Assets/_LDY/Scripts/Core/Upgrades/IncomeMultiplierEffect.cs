public class IncomeMultiplierEffect : IUpgradeEffect
{
    private readonly IPlayerStatsMutator stats;
    private readonly float baseMultiplier;

    public UpgradeType Type => UpgradeType.IncomeMultiplier;

    public IncomeMultiplierEffect(IPlayerStatsMutator stats, float baseMultiplier)
    {
        this.stats = stats;
        this.baseMultiplier = baseMultiplier;
    }

    public float ValueAt(UpgradeData data, int level) => baseMultiplier + data.EffectPerLevel * level;

    public void Apply(UpgradeData data, int level) => stats.SetIncomeMultiplier(ValueAt(data, level));

    public string Format(float value) => $"수익 x{value:0.00}";
}
