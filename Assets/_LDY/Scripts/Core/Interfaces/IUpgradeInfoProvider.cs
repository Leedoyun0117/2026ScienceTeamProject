using System;

public interface IUpgradeInfoProvider
{
    event Action OnChanged;
    UpgradeInfo GetInfo(UpgradeData data);
}
