using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeCardView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Color availableColor = new Color(0.25f, 0.4f, 0.55f);
    [SerializeField] private Color disabledColor = new Color(0.38f, 0.38f, 0.42f);

    public void Init(UpgradeData data, Action<UpgradeData> onClick)
    {
        nameText.text = data.DisplayName;
        iconImage.sprite = data.Icon;
        iconImage.enabled = data.Icon != null;
        button.onClick.AddListener(() => onClick(data));
    }

    public void Refresh(UpgradeInfo info)
    {
        levelText.text = info.IsMax ? $"Lv {info.Level}/{info.MaxLevel} (MAX)" : $"Lv {info.Level}/{info.MaxLevel}";
        effectText.text = info.IsMax ? info.CurrentText : $"{info.CurrentText} ->{info.NextText}";
        costText.text = info.IsMax ? "MAX" : $"{info.Cost}원";

        bool available = !info.IsMax && info.CanAfford;
        button.interactable = available;
        background.color = available ? availableColor : disabledColor;
    }
}
