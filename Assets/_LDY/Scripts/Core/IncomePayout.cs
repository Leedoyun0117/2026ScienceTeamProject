using System;

public class IncomePayout : IIncomePayout
{
    private readonly IWallet wallet;
    private readonly IIncomeCalculator calculator;

    public event Action<int> OnPaid;

    public IncomePayout(IWallet wallet, IIncomeCalculator calculator)
    {
        this.wallet = wallet;
        this.calculator = calculator;
    }

    public void Pay()
    {
        int income = calculator.CalculateTickIncome();
        if (income <= 0) return;

        wallet.Add(income);
        OnPaid?.Invoke(income);
    }
}
