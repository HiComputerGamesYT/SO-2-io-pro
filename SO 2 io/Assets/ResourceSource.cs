using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ResourceSource : MonoBehaviour
{
    [Header("Налаштування Ресурсу")]
    [Tooltip("Максимальне здоров'я ресурсу (наприклад, 100 для дерева).")]
    public float maxHealth = 100f;

    [Header("Лут (що випадає)")]
    [Tooltip("Предмет, який випадає.")]
    public Item lootItem;

    [Tooltip("Шанс випадіння 1 одиниці луту при кожному ударі.")]
    [Range(0f, 1f)]
    public float dropChanceOnHit = 0.3f; // 30% шанс

    [Tooltip("Кількість предметів, які гарантовано випадають при знищенні.")]
    public int dropQuantityOnDestroy = 5;

    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        // Переконаємось, що колайдер не є тригером, щоб він був фізичною перешкодою
        GetComponent<Collider2D>().isTrigger = false;
    }

    /// <summary>
    /// Метод для нанесення шкоди ресурсу.
    /// </summary>
    /// <param name="damageAmount">Кількість шкоди.</param>
    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log($"<color=orange>{gameObject.name} отримав {damageAmount} шкоди. Залишилось здоров'я: {currentHealth}/{maxHealth}</color>");

        // Шанс отримати лут при ударі
        if (Random.value <= dropChanceOnHit)
        {
            SpawnLoot(1);
            Debug.Log($"<color=green>Випав 1 {lootItem.itemName} при ударі!</color>");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Метод, що викликається при знищенні ресурсу.
    /// </summary>
    private void Die()
    {
        Debug.Log($"<color=red>{gameObject.name} знищено!</color>");
        // Гарантований лут при знищенні
        SpawnLoot(dropQuantityOnDestroy);
        Destroy(gameObject);
    }

    private void SpawnLoot(int quantity)
    {
        if (lootItem == null || InventoryManager.instance.worldItemPrefab == null) return;

        GameObject droppedLoot = Instantiate(InventoryManager.instance.worldItemPrefab, transform.position, Quaternion.identity);

        WorldItem worldItem = droppedLoot.GetComponent<WorldItem>();
        if (worldItem != null)
        {
            worldItem.itemData = lootItem;
            worldItem.quantity = quantity;
        }
    }
}
