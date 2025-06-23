using UnityEngine;

// Цей компонент треба додавати до префабів предметів, що лежать на землі.
[RequireComponent(typeof(SpriteRenderer))]
public class WorldItem : MonoBehaviour
{
    [Header("Дані Предмету")]
    [Tooltip("Дані предмета (ScriptableObject), який представляє цей об'єкт.")]
    public Item itemData;

    [Tooltip("Кількість предметів в цьому стаку.")]
    [Range(1, 99)]
    public int quantity = 1;

    void Start()
    {
        // Автоматично встановлюємо спрайт на основі даних з Item, якщо він не встановлений вручну.
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null && itemData != null)
        {
            sr.sprite = itemData.icon;
        }
    }

    /// <summary>
    /// Цей метод викликається ззовні (наприклад, гравцем), щоб підібрати предмет.
    /// </summary>
    public void PickUp()
    {
        if (InventoryManager.instance == null)
        {
            Debug.LogError("Не вдалося знайти InventoryManager.instance! Предмет не може бути підібраний.");
            return;
        }

        // Намагаємось додати предмет до інвентарю гравця.
        bool wasPickedUp = InventoryManager.instance.AddItem(itemData, quantity);

        // Якщо предмет було успішно додано (повністю), знищуємо його об'єкт зі світу.
        if (wasPickedUp)
        {
            Destroy(gameObject);
        }
        else
        {
            // Якщо інвентар заповнений, предмет не підбирається і залишається на землі.
            Debug.Log("Інвентар заповнений! Неможливо підібрати " + itemData.itemName);
            // Тут можна додати звук або візуальний ефект, який покаже гравцю, що інвентар повний.
        }
    }
}
