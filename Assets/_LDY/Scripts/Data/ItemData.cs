using UnityEngine;

[CreateAssetMenu(menuName = "Tycoon/Item Data", fileName = "NewItem")]
public class ItemData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField, Min(0)] private int price;
    [SerializeField, Min(0)] private int powerConsumption;
    [SerializeField, Min(0)] private int baseIncome;
    [SerializeField, Min(0)] private float incomeMultiplier = 1f;

    public string DisplayName => displayName;
    public int Price => price;
    public int PowerConsumption => powerConsumption;
    public int BaseIncome => baseIncome;
    public float IncomeMultiplier => incomeMultiplier;
}
