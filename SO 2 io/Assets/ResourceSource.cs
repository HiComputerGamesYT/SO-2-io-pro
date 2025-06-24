using UnityEngine;

public class ResourceSource : MonoBehaviour
{
    [Header("Налаштування здоров'я")]
    public float maxHealth = 100f;

    [Header("Налаштування видобутку")]
    public Item lootItem;
    public int lootQuantityOnDestroy = 5;
    [Range(0f, 1f)]
    public float dropOnHitChance = 0.5f;

    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // Тепер цей метод приймає інформацію про того, хто завдав шкоди
    public void TakeDamage(float damage, Transform damageDealer)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;

        if (Random.value < dropOnHitChance)
        {
            // Передаємо інформацію про гравця далі
            SpawnLoot(1, damageDealer);
        }

        if (currentHealth <= 0)
        {
            Die(damageDealer);
        }
    }

    private void Die(Transform damageDealer)
    {
        // Передаємо інформацію про гравця далі
        SpawnLoot(lootQuantityOnDestroy, damageDealer);
        Destroy(gameObject);
    }

    // Тепер цей метод знає, де знаходиться гравець
    private void SpawnLoot(int quantity, Transform damageDealer)
    {
        if (lootItem == null || InventoryManager.instance.worldItemPrefab == null) return;

        // --- ВИРІШЕННЯ ПРОБЛЕМИ ---
        // Створюємо об'єкт біля гравця, а не біля дерева
        Vector3 spawnPosition = damageDealer.position + (Vector3)Random.insideUnitCircle * 0.5f;
        GameObject itemObject = Instantiate(InventoryManager.instance.worldItemPrefab, spawnPosition, Quaternion.identity);

        SpriteRenderer sr = itemObject.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Items";
        }

        WorldItem worldItem = itemObject.GetComponent<WorldItem>();
        if (worldItem != null)
        {
            worldItem.itemData = lootItem;
            worldItem.quantity = quantity;
        }
    }
}