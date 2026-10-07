using UnityEngine;

public class UiBinder : MonoBehaviour
{
    [SerializeField] private HudView hudView;
    [SerializeField] private ItemShopView itemShopView;
    [SerializeField] private UpgradeShopView upgradeShopView;
    [SerializeField] private CharacterView characterView;
    [SerializeField] private CharacterClickView characterClickView;
    [SerializeField] private CoinPopupView coinPopupView;

    private ShopClickRouter router;
    private IClickService click;

    public void Bind(GameContext context)
    {
        hudView.Bind(context.Wallet, context.Grid, context.IncomeForecast, context.Stats, context.CautionThreshold);
        itemShopView.Bind(context.Items, context.ItemInfo);
        upgradeShopView.Bind(context.Upgrade.Upgrades, context.UpgradeInfo);
        characterView.Bind(context.Reaction);
        coinPopupView.Bind(context.Payout, context.Click);

        click = context.Click;
        characterClickView.OnClicked += click.Click;

        router = new ShopClickRouter(context.Purchase, context.Toggle, context.Upgrade);
        itemShopView.OnItemClicked += router.OnItemClicked;
        upgradeShopView.OnUpgradeClicked += router.OnUpgradeClicked;
    }

    private void OnDestroy()
    {
        if (click != null) characterClickView.OnClicked -= click.Click;
        if (router == null) return;
        itemShopView.OnItemClicked -= router.OnItemClicked;
        upgradeShopView.OnUpgradeClicked -= router.OnUpgradeClicked;
    }
}
