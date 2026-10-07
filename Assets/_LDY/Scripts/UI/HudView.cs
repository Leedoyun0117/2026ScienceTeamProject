using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HudView : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text incomeText;
    [SerializeField] private TMP_Text perSecondText;
    [SerializeField] private TMP_Text clickIncomeText;
    [SerializeField] private Image gaugeFill;
    [SerializeField] private GameObject blackoutWarning;
    [SerializeField] private Color safeColor = new Color(0.3f, 0.8f, 0.4f);
    [SerializeField] private Color cautionColor = new Color(0.95f, 0.8f, 0.2f);
    [SerializeField] private Color dangerColor = new Color(0.9f, 0.25f, 0.2f);

    private IWallet wallet;
    private IPowerGrid grid;
    private IIncomeForecast forecast;
    private IPlayerStats stats;
    private float cautionThreshold;

    public void Bind(IWallet wallet, IPowerGrid grid, IIncomeForecast forecast, IPlayerStats stats, float cautionThreshold)
    {
        this.wallet = wallet;
        this.grid = grid;
        this.forecast = forecast;
        this.stats = stats;
        this.cautionThreshold = cautionThreshold;

        wallet.OnMoneyChanged += ShowMoney;
        grid.OnPowerChanged += ShowPower;
        grid.OnBlackoutChanged += ShowBlackout;
        forecast.OnTickIncomeChanged += ShowIncome;
        forecast.OnPerSecondIncomeChanged += ShowPerSecond;
        stats.OnStatsChanged += ShowClickIncome;

        ShowMoney(wallet.Money);
        ShowPower(grid.CurrentPower, grid.Limit);
        ShowBlackout(grid.IsBlackout);
        ShowIncome(forecast.TickIncome);
        ShowPerSecond(forecast.PerSecondIncome);
        ShowClickIncome();
    }

    private void OnDestroy()
    {
        if (wallet != null) wallet.OnMoneyChanged -= ShowMoney;
        if (grid != null)
        {
            grid.OnPowerChanged -= ShowPower;
            grid.OnBlackoutChanged -= ShowBlackout;
        }
        if (stats != null) stats.OnStatsChanged -= ShowClickIncome;
        if (forecast != null)
        {
            forecast.OnTickIncomeChanged -= ShowIncome;
            forecast.OnPerSecondIncomeChanged -= ShowPerSecond;
        }
    }

    private void ShowMoney(int money) => moneyText.text = $"돈: {money}";

    private void ShowIncome(int income) => incomeText.text = $"틱당 수익: +{income}";

    private void ShowPerSecond(float income) => perSecondText.text = $"초당 수익: +{income:0.#}";

    private void ShowClickIncome() => clickIncomeText.text = $"클릭당 수익: +{stats.ClickIncome}";

    private void ShowPower(int current, int limit)
    {
        powerText.text = $"전력: {current} / {limit}";

        float ratio = limit > 0 ? (float)current / limit : 1f;
        var rect = gaugeFill.rectTransform;
        rect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        gaugeFill.color = ratio > 1f ? dangerColor : ratio >= cautionThreshold ? cautionColor : safeColor;
    }

    private void ShowBlackout(bool blackout) => blackoutWarning.SetActive(blackout);
}
