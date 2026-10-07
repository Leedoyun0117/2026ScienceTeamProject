using UnityEngine;

[CreateAssetMenu(menuName = "Tycoon/Item Data", fileName = "NewItem")]
public class ItemData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField, TextArea] private string description;
    [SerializeField, Min(0)] private int price;
    [SerializeField, Min(0)] private int powerConsumption;
    [SerializeField, Min(0)] private int baseIncome;
    [SerializeField, Min(0)] private float incomeMultiplier = 1f;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public string Description => description;
    public int Price => price;
    public int PowerConsumption => powerConsumption;
    public int BaseIncome => baseIncome;
    public float IncomeMultiplier => incomeMultiplier;
}
