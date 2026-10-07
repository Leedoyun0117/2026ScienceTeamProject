public enum ItemCardState
{
    Purchasable,
    Unaffordable,
    On,
    Off,
}

public readonly struct ItemInfo
{
    public ItemCardState State { get; }
    public int ExpectedIncome { get; }
    public bool WillExceedLimit { get; }

    public ItemInfo(ItemCardState state, int expectedIncome, bool willExceedLimit)
    {
        State = state;
        ExpectedIncome = expectedIncome;
        WillExceedLimit = willExceedLimit;
    }
}
