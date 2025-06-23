using UnityEngine;
using System.Collections.Generic;

// Клас для представлення слота інвентарю
// [System.Serializable] дозволяє бачити цей клас в Інспекторі Unity
[System.Serializable]
public class InventorySlot
{
    public Item item;       // Предмет у цьому слоті (посилання на Item ScriptableObject)
    public int quantity;    // Кількість предметів у цьому слоті
    public bool isLocked;   // Чи заблокований слот

    // Конструктор для слота, дозволяє вказати, чи він заблокований при створенні
    public InventorySlot(bool locked = false)
    {
        item = null;
        quantity = 0;
        isLocked = locked;
    }

    // Метод для додавання предмета до слота
    // Повертає кількість, яка не помістилася в цей слот (залишок)
    public int AddItem(Item newItem, int amount)
    {
        // Якщо слот порожній або вже містить цей самий предмет
        if (item == null || item == newItem)
        {
            item = newItem; // Присвоюємо новий предмет (якщо слот був порожній)
            int spaceLeft = item.maxStack - quantity; // Скільки місця залишилося в слоті
            int amountToAdd = Mathf.Min(amount, spaceLeft); // Скільки можна додати
            quantity += amountToAdd; // Додаємо кількість
            return amount - amountToAdd; // Повертаємо залишок, який не вдалося додати
        }
        return amount; // Якщо предмет не співпадає, повертає всю вхідну кількість
    }

    // Метод для очищення слота (предмет і кількість обнуляються)
    public void ClearSlot()
    {
        item = null;
        quantity = 0;
    }

    // Перевіряє, чи слот порожній
    public bool IsEmpty()
    {
        return item == null || quantity == 0;
    }

    // Перевіряє, чи можна додати в слот певний предмет (з урахуванням типу предмета, максимального стека та блокування)
    public bool CanAddItem(Item newItem)
    {
        // Слот може прийняти предмет, якщо:
        // 1. Він порожній АБО
        // 2. Він містить такий самий предмет І не заповнений до максимального стека
        // І при цьому слот НЕ заблокований
        return (IsEmpty() || (item == newItem && quantity < item.maxStack)) && !isLocked;
    }
}


// Основний клас InventoryManager, що є компонентом MonoBehaviour
public class InventoryManager : MonoBehaviour
{
    [Tooltip("Загальна кількість слотів інвентарю.")]
    public int inventorySize = 20;
    [Tooltip("Кількість початково заблокованих слотів (від кінця інвентарю).")]
    public int lockedSlotsCount = 10;
    [Tooltip("Вартість (умовно) розблокування одного слота (наприклад, ресурсів).")]
    public int unlockSlotCost = 10;

    public List<InventorySlot> slots;

    // Подія, яка викликається кожного разу, коли інвентар змінюється
    // UI та інші системи можуть підписатися на цю подію, щоб оновитися
    public delegate void OnInventoryChanged();
    public event OnInventoryChanged onInventoryChangedCallback;

    // Метод Awake викликається при завантаженні об'єкта (навіть якщо скрипт вимкнено)
    void Awake()
    {
        slots = new List<InventorySlot>();
        for (int i = 0; i < inventorySize; i++)
        {
            // Створюємо нові слоти; останні lockedSlotsCount слотів будуть заблоковані
            slots.Add(new InventorySlot(i >= (inventorySize - lockedSlotsCount)));
        }
    }

    // Метод для додавання предмета в інвентар
    // Повертає true, якщо предмет (або його частина) був успішно доданий
    public bool AddItem(Item item, int quantity)
    {
        if (item == null || quantity <= 0) // Перевірка вхідних даних
        {
            Debug.LogWarning("AddItem: Предмет або кількість некоректні.");
            return false;
        }

        // 1. Спробувати додати до існуючого слота (стакування предметів)
        foreach (InventorySlot slot in slots)
        {
            if (slot.item == item && slot.quantity < item.maxStack && !slot.isLocked)
            {
                int remainingAmount = slot.AddItem(item, quantity); // Спробувати додати в цей слот
                if (remainingAmount == 0) // Якщо все помістилося
                {
                    onInventoryChangedCallback?.Invoke(); // Сповіщаємо про зміни
                    return true;
                }
                quantity = remainingAmount; // Оновлюємо кількість, яка залишилася для додавання
            }
        }

        // 2. Спробувати додати в порожній слот
        foreach (InventorySlot slot in slots)
        {
            if (slot.IsEmpty() && !slot.isLocked) // Якщо слот порожній і не заблокований
            {
                int remainingAmount = slot.AddItem(item, quantity); // Додаємо
                if (remainingAmount == 0) // Якщо все помістилося
                {
                    onInventoryChangedCallback?.Invoke(); // Сповіщаємо про зміни
                    return true;
                }
                quantity = remainingAmount; // Оновлюємо кількість, яка залишилася
            }
        }

        // Якщо дійшли сюди, значить інвентар заповнений або немає місця
        Debug.LogWarning($"Інвентар заповнений або немає місця для {item.itemName} x{quantity}.");
        onInventoryChangedCallback?.Invoke(); // Все одно викликаємо, щоб UI оновився (навіть якщо не вдалося)
        return false; // Не вдалося додати всі предмети
    }

    // Метод для видалення предмета з інвентарю
    // Повертає true, якщо предмет був успішно видалений (або його частина)
    public bool RemoveItem(Item item, int quantity)
    {
        if (item == null || quantity <= 0) // Перевірка вхідних даних
        {
            Debug.LogWarning("RemoveItem: Предмет або кількість некоректні.");
            return false;
        }

        int totalRemoved = 0; // Лічильник для відстеження, скільки предметів було видалено
        // Проходимо слоти у зворотному порядку (може бути корисним, наприклад, якщо предмети в кінці інвентарю)
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            InventorySlot slot = slots[i];
            if (slot.item == item && !slot.IsEmpty()) // Якщо слот містить потрібний предмет і не порожній
            {
                int amountToRemove = Mathf.Min(quantity, slot.quantity); // Скільки можна видалити з цього слота
                slot.quantity -= amountToRemove; // Зменшуємо кількість
                totalRemoved += amountToRemove; // Додаємо до загальної видаленої кількості
                quantity -= amountToRemove; // Зменшуємо кількість, яку ще потрібно видалити

                if (slot.quantity <= 0) // Якщо слот став порожнім, очищаємо його
                {
                    slot.ClearSlot();
                }

                if (quantity <= 0) break; // Якщо вся потрібна кількість видалена, виходимо з циклу
            }
        }

        if (totalRemoved > 0) // Якщо щось було видалено
        {
            onInventoryChangedCallback?.Invoke(); // Сповіщаємо про зміни
            return true;
        }
        Debug.LogWarning($"RemoveItem: Не вдалося знайти {item.itemName} або достатньої кількості для видалення.");
        return false; // Нічого не видалено
    }

    // Метод для розблокування слота за його індексом
    public bool UnlockSlot(int slotIndex)
    {
        // Перевіряємо, чи індекс знаходиться в межах діапазону слотів
        if (slotIndex >= 0 && slotIndex < slots.Count)
        {
            if (slots[slotIndex].isLocked) // Якщо слот дійсно заблокований
            {
                // Тут у майбутньому можна додати логіку перевірки ресурсів гравця для розблокування
                // Наприклад:
                // if (Player.instance.HasEnoughResource("Gold", unlockSlotCost))
                // {
                //     Player.instance.RemoveResource("Gold", unlockSlotCost);
                slots[slotIndex].isLocked = false; // Розблоковуємо слот
                onInventoryChangedCallback?.Invoke(); // Сповіщаємо про зміни
                Debug.Log($"Слот {slotIndex} розблоковано.");
                return true;
                // }
                // else
                // {
                //     Debug.Log("Недостатньо ресурсів для розблокування слота.");
                //     return false;
                // }
            }
            else
            {
                Debug.Log($"Слот {slotIndex} вже розблоковано.");
            }
        }
        Debug.LogWarning($"UnlockSlot: Некоректний індекс слота ({slotIndex}).");
        return false; // Слот не розблоковано
    }

    // Метод для обміну місцями вмісту двох слотів
    // Також обробляє логіку стакування, якщо предмети однакові
    public void SwapSlots(int indexA, int indexB)
    {
        // Перевіряємо, чи індекси знаходяться в межах діапазону слотів
        if (indexA < 0 || indexA >= slots.Count || indexB < 0 || indexB >= slots.Count)
        {
            Debug.LogWarning($"SwapSlots: Спроба обміняти слоти за межами діапазону. IndexA: {indexA}, IndexB: {indexB}, InventorySize: {slots.Count}.");
            return;
        }

        InventorySlot slotA = slots[indexA];
        InventorySlot slotB = slots[indexB];

        // Не дозволяємо перетягувати в заблокований слот, якщо він не порожній і НЕ є тим самим, що тягнемо
        // Це означає, що можна викинути предмет із заблокованого слота, але не можна покласти щось у заблокований слот, якщо він вже не пустий.
        if (slotB.isLocked && !slotB.IsEmpty() && indexA != indexB)
        {
            Debug.Log("Не можна перетягнути предмет у заблокований слот, якщо він не порожній.");
            onInventoryChangedCallback?.Invoke(); // Оновлюємо UI, якщо перетягування не відбулося
            return;
        }

        // Логіка стакування: якщо предмети однакові і цільовий слот (slotB) не повний
        if (slotA.item != null && slotA.item == slotB.item && slotB.quantity < slotB.item.maxStack && !slotB.isLocked)
        {
            int amountToStack = Mathf.Min(slotA.quantity, slotB.item.maxStack - slotB.quantity); // Скільки можна додати до стека
            slotB.quantity += amountToStack; // Додаємо до цільового слота
            slotA.quantity -= amountToStack; // Залишок у вихідному слоті

            if (slotA.quantity <= 0) // Якщо вихідний слот став порожнім, очищаємо його
            {
                slotA.ClearSlot();
            }
        }
        else // Просто обмін місцями (якщо предмети різні або стакування неможливе)
        {
            // Тимчасово зберігаємо вміст слота B
            Item tempItem = slotB.item;
            int tempQuantity = slotB.quantity;

            // Переміщуємо вміст слота A в слот B
            slotB.item = slotA.item;
            slotB.quantity = slotA.quantity;

            // Переміщуємо тимчасово збережений вміст в слот A
            slotA.item = tempItem;
            slotA.quantity = tempQuantity;
        }

        onInventoryChangedCallback?.Invoke(); // Повідомляємо UI про зміни
        Debug.Log($"Обміняно/стаковано слоти {indexA} та {indexB}.");
    }

    // Метод для отримання даних слота за індексом
    public InventorySlot GetSlot(int index)
    {
        if (index >= 0 && index < slots.Count)
        {
            return slots[index];
        }
        Debug.LogWarning($"GetSlot: Некоректний індекс слота ({index}).");
        return null;
    }
}
