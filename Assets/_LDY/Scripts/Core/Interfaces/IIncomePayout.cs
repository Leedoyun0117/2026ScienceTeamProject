using System;

public interface IIncomePayout
{
    event Action<int> OnPaid;
    void Pay();
}
