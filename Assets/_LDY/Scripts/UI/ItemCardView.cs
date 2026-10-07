using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemCardView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text incomeText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private Color purchasableColor = new Color(0.2f, 0.45f, 0.3f);
    [SerializeField] private Color unaffordableColor = new Color(0.38f, 0.38f, 0.42f);
    [SerializeField] private Color onColor = new Color(0.2f, 0.35f, 0.6f);
    [SerializeField] private Color offColor = new Color(0.45f, 0.32f, 0.2f);

    public void Init(ItemData item, Action<ItemData> onClick)
    {
        nameText.text = item.DisplayName;
        descriptionText.text = item.Description;
        priceText.text = $"가격 {item.Price}";
        powerText.text = $"전력 {item.PowerConsumption}";
        iconImage.sprite = item.Icon;
        iconImage.enabled = item.Icon != null;
        warningText.text = "한도 초과 예상!";
        button.onClick.AddListener(() => onClick(item));
    }

    public void Refresh(ItemInfo info)
    {
        incomeText.text = $"+{info.ExpectedIncome}/틱";
        warningText.gameObject.SetActive(info.WillExceedLimit);

        switch (info.State)
        {
            case ItemCardState.Purchasable:
                Apply("구매 가능", purchasableColor, true);
                break;
            case ItemCardState.Unaffordable:
                Apply("돈 부족", unaffordableColor, false);
                break;
            case ItemCardState.On:
                Apply("ON", onColor, true);
                break;
            case ItemCardState.Off:
                Apply("OFF", offColor, true);
                break;
        }
    }

    private void Apply(string state, Color color, bool interactable)
    {
        stateText.text = state;
        background.color = color;
        button.interactable = interactable;
    }
}
