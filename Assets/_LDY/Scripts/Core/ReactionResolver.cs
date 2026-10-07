using System;

public class ReactionResolver : IReactionResolver
{
    private readonly float cautionThreshold;
    private bool blackout;
    private bool nearLimit;
    private ReactionType? activeOneShot;

    public ReactionType Current { get; private set; } = ReactionType.Idle;
    public event Action<ReactionType> OnReaction;

    public ReactionResolver(IPowerGrid grid, IPurchaseService purchase, IUpgradeService upgrade,
        IIncomePayout payout, float cautionThreshold)
    {
        this.cautionThreshold = cautionThreshold;

        grid.OnPowerChanged += HandlePower;
        grid.OnBlackoutChanged += HandleBlackout;
        purchase.OnItemPurchased += _ => TryOneShot(ReactionType.Purchase);
        upgrade.OnLevelChanged += (_, __) => TryOneShot(ReactionType.Purchase);
        payout.OnPaid += _ => TryOneShot(ReactionType.Income);
    }

    public void Complete(ReactionType finished)
    {
        if (activeOneShot != finished) return;
        activeOneShot = null;
        Emit(BaseReaction, false);
    }

    private ReactionType BaseReaction =>
        blackout ? ReactionType.Blackout : nearLimit ? ReactionType.NearLimit : ReactionType.Idle;

    private void HandlePower(int current, int limit)
    {
        float ratio = limit > 0 ? (float)current / limit : 1f;
        nearLimit = ratio >= cautionThreshold;
        RefreshBase();
    }

    private void HandleBlackout(bool isBlackout)
    {
        blackout = isBlackout;
        if (isBlackout) RefreshBase();
        else TryOneShot(ReactionType.Recovery);
    }

    private void RefreshBase()
    {
        var baseReaction = BaseReaction;
        if (activeOneShot.HasValue && Priority(activeOneShot.Value) >= Priority(baseReaction)) return;

        activeOneShot = null;
        Emit(baseReaction, false);
    }

    private void TryOneShot(ReactionType type)
    {
        var effective = activeOneShot ?? BaseReaction;
        if (Priority(type) < Priority(effective)) return;

        activeOneShot = type;
        Emit(type, true);
    }

    private void Emit(ReactionType type, bool force)
    {
        if (!force && type == Current) return;
        Current = type;
        OnReaction?.Invoke(type);
    }

    private static int Priority(ReactionType type)
    {
        switch (type)
        {
            case ReactionType.Blackout: return 5;
            case ReactionType.Recovery: return 4;
            case ReactionType.Purchase: return 3;
            case ReactionType.NearLimit: return 2;
            case ReactionType.Income: return 1;
            default: return 0;
        }
    }
}
