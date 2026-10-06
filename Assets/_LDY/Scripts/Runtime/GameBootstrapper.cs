using UnityEngine;

public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private int startingMoney = 100;
    [SerializeField] private int powerLimit = 100;
    [SerializeField] private ItemData[] items;
    [SerializeField] private GameTicker ticker;
    [SerializeField] private HudView hudView;
    [SerializeField] private ShopView shopView;

    private IWallet wallet;
    private IIncomeCalculator incomeCalculator;

    private void Awake()
    {
        wallet = new Wallet(startingMoney);
        var grid = new PowerGrid(powerLimit);
        var purchase = new PurchaseService(wallet, grid);
        var toggle = new ItemToggleService(purchase, grid);
        incomeCalculator = new IncomeCalculator(grid);

        hudView.Bind(wallet, grid);
        shopView.Bind(items, purchase, grid);
        shopView.OnItemClicked += item =>
        {
            if (purchase.IsOwned(item)) toggle.Toggle(item);
            else purchase.TryPurchase(item);
        };

        ticker.OnTick += HandleTick;
    }

    private void OnDestroy()
    {
        if (ticker != null) ticker.OnTick -= HandleTick;
    }

    private void HandleTick() => wallet.Add(incomeCalculator.CalculateTickIncome());
}
