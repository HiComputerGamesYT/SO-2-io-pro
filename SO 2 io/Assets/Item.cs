using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public enum ItemType
    {
        Resource,
        Tool,
        Block
    }

    [Header("Основна інформація")]
    public string itemName = "New Item";
    public Sprite icon = null;

    public ItemType itemType = ItemType.Resource;

    [Tooltip("Максимальна кількість предметів в одному стаку.")]
    public int maxStack = 16;

    [Header("Для інструментів")]
    public float damage = 10f;

    // --- ДОДАНО НОВІ ПОЛЯ ТУТ ---
    [Header("Для будівельних блоків")]
    [Tooltip("Перетягніть сюди ассет тайлу, який відповідає цьому предмету")]
    public TileBase correspondingTile;

    [Tooltip("Кількість здоров'я, яке має блок після встановлення.")]
    public int blockHealth = 50;

    [Tooltip("Предмет, який може випасти при руйнуванні блоку (залиште порожнім, якщо нічого не випадає).")]
    public Item itemToDrop;

    [Tooltip("Шанс випадіння предмету (від 0 до 1, де 1 = 100%).")]
    [Range(0f, 1f)]
    public float dropChance = 0.5f;
    // --------------------------------

    [Header("Префаб для світу гри")]
    [Tooltip("Префаб, який з'являється в світі, коли предмет викидають")]
    public GameObject itemPrefab;
}