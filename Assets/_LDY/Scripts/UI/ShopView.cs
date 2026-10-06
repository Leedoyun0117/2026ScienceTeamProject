using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopView : MonoBehaviour
{
    [SerializeField] private ItemButtonView buttonPrefab;
    [SerializeField] private Transform container;

    private readonly Dictionary<ItemData, ItemButtonView> buttons = new Dictionary<ItemData, ItemButtonView>();
    private IPurchaseService purchase;
    private IPowerGrid grid;

    public event Action<ItemData> OnItemClicked;

    public void Bind(ItemData[] items, IPurchaseService purchase, IPowerGrid grid)
    {
        this.purchase = purchase;
        this.grid = grid;

        foreach (var item in items)
        {
            var view = Instantiate(buttonPrefab, container);
            view.Init(item, clicked => OnItemClicked?.Invoke(clicked));
            buttons[item] = view;
        }

        grid.OnActiveItemsChanged += RefreshAll;
        purchase.OnItemPurchased += HandlePurchased;
        RefreshAll();
    }

    private void OnDestroy()
    {
        if (grid != null) grid.OnActiveItemsChanged -= RefreshAll;
        if (purchase != null) purchase.OnItemPurchased -= HandlePurchased;
    }

    private void HandlePurchased(ItemData item) => RefreshAll();

    private void RefreshAll()
    {
        foreach (var pair in buttons)
            pair.Value.Refresh(purchase.IsOwned(pair.Key), grid.IsActive(pair.Key));
    }
}
