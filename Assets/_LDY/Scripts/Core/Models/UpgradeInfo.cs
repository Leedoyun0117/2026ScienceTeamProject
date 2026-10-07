public readonly struct UpgradeInfo
{
    public int Level { get; }
    public int MaxLevel { get; }
    public int Cost { get; }
    public bool IsMax { get; }
    public bool CanAfford { get; }
    public string CurrentText { get; }
    public string NextText { get; }

    public UpgradeInfo(int level, int maxLevel, int cost, bool isMax, bool canAfford, string currentText, string nextText)
    {
        Level = level;
        MaxLevel = maxLevel;
        Cost = cost;
        IsMax = isMax;
        CanAfford = canAfford;
        CurrentText = currentText;
        NextText = nextText;
    }
}
