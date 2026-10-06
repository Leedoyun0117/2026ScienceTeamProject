using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemButtonView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    private ItemData item;

    public void Init(ItemData item, Action<ItemData> onClick)
    {
        this.item = item;
        button.onClick.AddListener(() => onClick(item));
    }

    public void Refresh(bool owned, bool active)
    {
        string state = !owned ? $"{item.Price}원" : active ? "ON" : "OFF";
        label.text = $"{item.DisplayName}\n전력 {item.PowerConsumption} | {state}";
    }
}
