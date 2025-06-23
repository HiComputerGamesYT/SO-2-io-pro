using UnityEngine;

// Додаємо перелік, щоб розрізняти предмети
public enum ItemType { Resource, Tool, Consumable }

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Header("Основна інформація")]
    public string itemName = "Новий предмет";
    [TextArea(3, 5)]
    public string description = "Опис цього предмету.";
    public Sprite icon = null;
    [Range(1, 99)]
    public int maxStack = 1;
    public ItemType itemType = ItemType.Resource;

    [Header("Налаштування Інструменту (якщо це інструмент)")]
    [Tooltip("Шкода, яку інструмент наносить ресурсам. Ігнорується, якщо тип предмету - не Tool.")]
    public float damage = 10f;
}
