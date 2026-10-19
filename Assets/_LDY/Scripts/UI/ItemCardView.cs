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
    [SerializeField] private Image breakerImage;
    [SerializeField] private Sprite availableSprite;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;
    [SerializeField] private Sprite breakerOnSprite;
    [SerializeField] private Sprite breakerOffSprite;

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
                Apply("구매 가능", availableSprite, true);
                break;
            case ItemCardState.Unaffordable:
                Apply("돈 부족", lockedSprite, false);
                break;
            case ItemCardState.On:
                Apply("", onSprite, true, breakerOnSprite);
                break;
            case ItemCardState.Off:
                Apply("", offSprite, true, breakerOffSprite);
                break;
        }
    }

    private void Apply(string state, Sprite sprite, bool interactable, Sprite breaker = null)
    {
        stateText.text = state;
        background.sprite = sprite;
        breakerImage.sprite = breaker;
        breakerImage.enabled = breaker != null;
        button.interactable = interactable;
    }
}
