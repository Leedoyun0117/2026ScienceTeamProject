using UnityEngine;

public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private int startingMoney = 50;
    [SerializeField] private int powerLimit = 80;
    [SerializeField] private float baseIncomeMultiplier = 1f;
    [SerializeField, Min(0.1f)] private float baseTickInterval = 1f;
    [SerializeField, Min(0.1f)] private float minTickInterval = 0.3f;
    [SerializeField, Min(0)] private int baseClickIncome = 1;
    [SerializeField, Range(0f, 1f)] private float cautionThreshold = 0.7f;
    [SerializeField] private ItemData[] items;
    [SerializeField] private UpgradeData[] upgrades;
    [SerializeField] private GameTicker ticker;
    [SerializeField] private UiBinder uiBinder;

    private GameContext context;
    private PlayerStats stats;

    private void Awake()
    {
        context = BuildContext();
        ticker.OnTick += context.Payout.Pay;
        ticker.Bind(stats);
        uiBinder.Bind(context);
    }

    private void OnDestroy()
    {
        if (ticker != null && context != null) ticker.OnTick -= context.Payout.Pay;
    }

    private GameContext BuildContext()
    {
        stats = new PlayerStats(powerLimit, baseIncomeMultiplier, baseTickInterval, baseClickIncome);
        var wallet = new Wallet(startingMoney);
        var grid = new PowerGrid(stats);
        var calculator = new IncomeCalculator(grid, stats);
        var purchase = new PurchaseService(wallet, grid);
        var payout = new IncomePayout(wallet, calculator);
        var effects = new IUpgradeEffect[]
        {
            new PowerLimitEffect(stats, powerLimit),
            new IncomeMultiplierEffect(stats, baseIncomeMultiplier),
            new IncomeIntervalEffect(stats, baseTickInterval, minTickInterval),
            new ClickIncomeEffect(stats, baseClickIncome),
        };
        var upgrade = new UpgradeService(wallet, effects, upgrades);

        return new GameContext
        {
            Stats = stats,
            Wallet = wallet,
            Grid = grid,
            IncomeCalculator = calculator,
            IncomeForecast = new IncomeForecast(calculator, grid, stats),
            Payout = payout,
            Click = new ClickService(wallet, stats, grid),
            Purchase = purchase,
            Toggle = new ItemToggleService(purchase, grid),
            Upgrade = upgrade,
            ItemInfo = new ItemInfoProvider(wallet, purchase, grid, calculator, stats),
            UpgradeInfo = new UpgradeInfoProvider(wallet, upgrade),
            Reaction = new ReactionResolver(grid, purchase, upgrade, payout, cautionThreshold),
            Items = items,
            CautionThreshold = cautionThreshold,
        };
    }
}
