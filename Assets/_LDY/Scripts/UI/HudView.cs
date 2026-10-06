using TMPro;
using UnityEngine;

public class HudView : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color blackoutColor = Color.red;

    private IWallet wallet;
    private IPowerGrid grid;

    public void Bind(IWallet wallet, IPowerGrid grid)
    {
        this.wallet = wallet;
        this.grid = grid;

        wallet.OnMoneyChanged += ShowMoney;
        grid.OnPowerChanged += ShowPower;
        grid.OnBlackoutChanged += ShowBlackout;

        ShowMoney(wallet.Money);
        ShowPower(grid.CurrentPower, grid.Limit);
        ShowBlackout(grid.IsBlackout);
    }

    private void OnDestroy()
    {
        if (wallet != null) wallet.OnMoneyChanged -= ShowMoney;
        if (grid == null) return;
        grid.OnPowerChanged -= ShowPower;
        grid.OnBlackoutChanged -= ShowBlackout;
    }

    private void ShowMoney(int money) => moneyText.text = $"돈: {money}";

    private void ShowPower(int current, int limit) => powerText.text = $"전력: {current} / {limit}";

    private void ShowBlackout(bool blackout) => powerText.color = blackout ? blackoutColor : normalColor;
}
