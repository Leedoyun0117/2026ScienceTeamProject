 using UnityEngine;

public enum UpgradeType
{
    PowerLimit,
    IncomeMultiplier,
    IncomeInterval,
    ClickIncome,
}

[CreateAssetMenu(menuName = "Tycoon/Upgrade Data", fileName = "NewUpgrade")]
public class UpgradeData : ScriptableObject
{
    [SerializeField] private UpgradeType type;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField, Min(0)] private int baseCost = 50;
    [SerializeField, Min(1f)] private float costGrowth = 1.5f;
    [SerializeField] private float effectPerLevel = 10f;
    [SerializeField, Min(1)] private int maxLevel = 5;

    public UpgradeType Type => type;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public float EffectPerLevel => effectPerLevel;
    public int MaxLevel => maxLevel;

    public int CostAt(int level) => Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, level));
}
