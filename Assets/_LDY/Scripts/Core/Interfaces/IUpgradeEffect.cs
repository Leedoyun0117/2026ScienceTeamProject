public interface IUpgradeEffect
{
    UpgradeType Type { get; }
    float ValueAt(UpgradeData data, int level);
    void Apply(UpgradeData data, int level);
    string Format(float value);
}
