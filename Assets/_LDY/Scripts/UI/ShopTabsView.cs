using UnityEngine;
using UnityEngine.UI;

public class ShopTabsView : MonoBehaviour
{
    [SerializeField] private Button itemTabButton;
    [SerializeField] private Button upgradeTabButton;
    [SerializeField] private GameObject itemPanel;
    [SerializeField] private GameObject upgradePanel;

    private void Awake()
    {
        itemTabButton.onClick.AddListener(() => Show(true));
        upgradeTabButton.onClick.AddListener(() => Show(false));
        Show(true);
    }

    private void Show(bool items)
    {
        itemPanel.SetActive(items);
        upgradePanel.SetActive(!items);
        itemTabButton.interactable = !items;
        upgradeTabButton.interactable = items;
    }
}
