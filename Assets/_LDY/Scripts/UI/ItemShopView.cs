using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemShopView : MonoBehaviour
{
    [SerializeField] private ItemCardView cardPrefab;
    [SerializeField] private Transform container;

    private readonly Dictionary<ItemData, ItemCardView> cards = new Dictionary<ItemData, ItemCardView>();
    private IItemInfoProvider info;

    public event Action<ItemData> OnItemClicked;

    public void Bind(IReadOnlyList<ItemData> items, IItemInfoProvider info)
    {
        this.info = info;

        foreach (var item in items)
        {
            if (item == null) continue;
            var card = Instantiate(cardPrefab, container);
            card.Init(item, clicked => OnItemClicked?.Invoke(clicked));
            cards[item] = card;
        }

        info.OnChanged += RefreshAll;
        RefreshAll();
    }

    private void OnDestroy()
    {
        if (info != null) info.OnChanged -= RefreshAll;
    }

    private void RefreshAll()
    {
        foreach (var pair in cards) pair.Value.Refresh(info.GetInfo(pair.Key));
    }
}
