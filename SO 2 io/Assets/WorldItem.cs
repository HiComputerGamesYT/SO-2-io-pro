using UnityEngine;

// Цей скрипт буде прикріплюватися до GameObject'ів, які представляють предмети на карті
// І які гравець може підібрати.
public class WorldItem : MonoBehaviour
{
    [Tooltip("Визначення предмета (ScriptableObject), який буде підібрано.")]
    public Item itemData; // Сюди перетягніть ваш Asset "Item" (наприклад, StoneItem)
    [Tooltip("Кількість цього предмета, яка буде підібрана.")]
    [Range(1, 99)]
    public int quantity = 1;

    [Tooltip("Радіус зони підбору предмета.")]
    public float pickupRadius = 1f;

    // Сховище для посилання на InventoryManager
    private InventoryManager inventoryManager;

    void Awake()
    {
        // Переконайтеся, що на об'єкті є Collider2D і він є тригером.
        // Якщо його немає, додамо його автоматично.
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>(); // Можна BoxCollider2D, залежить від спрайта
            (col as CircleCollider2D).radius = pickupRadius;
        }
        col.isTrigger = true; // Робимо колайдер тригером для детекції входу

        // Знайти InventoryManager у сцені
        inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogError("WorldItem: InventoryManager не знайдено в сцені. Підбір предметів не працюватиме.");
        }
    }

    // Метод для підбору предмета
    public void PickUp()
    {
        if (inventoryManager != null && itemData != null)
        {
            // Спробувати додати предмет до інвентарю
            bool addedSuccessfully = inventoryManager.AddItem(itemData, quantity);

            if (addedSuccessfully)
            {
                Debug.Log($"Підібрано {quantity} x {itemData.itemName}.");
                Destroy(gameObject); // Знищуємо об'єкт предмета зі світу
            }
            else
            {
                Debug.Log($"Не вдалося підібрати {itemData.itemName}. Інвентар заповнений.");
            }
        }
    }

    // Візуалізація радіуса підбору в редакторі
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
