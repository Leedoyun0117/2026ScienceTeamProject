using UnityEngine;

public class CoinPopupView : MonoBehaviour
{
    [SerializeField] private CoinPopupItem popupPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private float spreadX = 40f;

    private IIncomePayout payout;
    private IClickService click;

    public void Bind(IIncomePayout payout, IClickService click)
    {
        this.payout = payout;
        this.click = click;
        payout.OnPaid += Show;
        click.OnClicked += Show;
    }

    private void OnDestroy()
    {
        if (payout != null) payout.OnPaid -= Show;
        if (click != null) click.OnClicked -= Show;
    }

    private void Show(int amount)
    {
        var item = Instantiate(popupPrefab, container);
        item.Play($"+{amount}", new Vector2(Random.Range(-spreadX, spreadX), 0f));
    }
}
