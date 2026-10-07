using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeShopView : MonoBehaviour
{
    [SerializeField] private UpgradeCardView cardPrefab;
    [SerializeField] private Transform container;

    private readonly Dictionary<UpgradeData, UpgradeCardView> cards = new Dictionary<UpgradeData, UpgradeCardView>();
    private IUpgradeInfoProvider info;

    public event Action<UpgradeData> OnUpgradeClicked;

    public void Bind(IReadOnlyList<UpgradeData> upgrades, IUpgradeInfoProvider info)
    {
        this.info = info;

        foreach (var data in upgrades)
        {
            var card = Instantiate(cardPrefab, container);
            card.Init(data, clicked => OnUpgradeClicked?.Invoke(clicked));
            cards[data] = card;
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
