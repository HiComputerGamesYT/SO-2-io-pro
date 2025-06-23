using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Важливо! Для інтерфейсів Drag & Drop
using System.Collections.Generic;

// Основний клас InventoryUI, що є компонентом MonoBehaviour
public class InventoryUI : MonoBehaviour
{
    [Header("UI Елементи")]
    [Tooltip("Панель (RectTransform), яка містить всі слоти інвентарю. У неї буде компонент GridLayoutGroup.")]
    public RectTransform slotsParent; // Батьківський об'єкт для слотів
    [Tooltip("Префаб одного слота інвентарю (UI GameObject) з прикріпленим скриптом InventorySlotUI.")]
    public GameObject inventorySlotPrefab;
    [Tooltip("Кнопка для розблокування слотів.")]
    public Button unlockSlotButton;
    [Tooltip("Текстовий об'єкт для відображення вартості розблокування слота.")]
    public Text unlockCostText;

    // --- Елементи для Drag & Drop ---
    [Header("Drag & Drop UI")]
    [Tooltip("Зображення (Image), яке буде перетягуватися за курсором. Повинно бути окремим GameObject.")]
    public Image draggedItemImage; // Це окремий GameObject Image, який буде перетягуватися
    [Tooltip("RectTransform панелі інвентарю, щоб детектувати скидання предметів поза нею (викидання).")]
    public RectTransform inventoryPanelRectTransform; // Для детекції скидання за межами інвентаря

    private InventoryManager inventoryManager; // Посилання на InventoryManager
    private List<GameObject> uiSlots = new List<GameObject>(); // Список створених UI слотів

    [HideInInspector] public InventorySlot draggedSlotData; // Дані слота, що перетягується (доступно для InventorySlotUI)
    [HideInInspector] public int draggedSlotIndex; // Індекс слота, що перетягується (доступно для InventorySlotUI)
    private Canvas canvas; // Посилання на головний Canvas для коректного позиціонування Drag & Drop елементів

    // Метод Awake викликається при завантаженні об'єкта
    void Awake()
    {
        canvas = GetComponentInParent<Canvas>(); // Отримати посилання на Canvas, на якому знаходиться UI
        if (canvas == null)
        {
            Debug.LogError("InventoryUI: Canvas не знайдено як батьківський об'єкт. Drag&Drop може працювати некоректно.");
        }
    }

    // Метод Start викликається перед першим оновленням кадру
    void Start()
    {
        inventoryManager = FindObjectOfType<InventoryManager>(); // Знаходимо InventoryManager у сцені
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryUI: InventoryManager не знайдено в сцені. Інвентарний інтерфейс не працюватиме.");
            enabled = false; // Вимкнути скрипт, якщо менеджер відсутній
            return;
        }

        // Перевіряємо, чи призначені елементи для Drag & Drop
        if (draggedItemImage == null)
        {
            Debug.LogError("InventoryUI: draggedItemImage не призначено. Функціонал Drag&Drop не працюватиме.");
        }
        else
        {
            draggedItemImage.gameObject.SetActive(false); // Спочатку приховуємо елемент перетягування
        }

        if (inventoryPanelRectTransform == null)
        {
            Debug.LogError("InventoryUI: inventoryPanelRectTransform не призначено. Функція викидання предметів у світ не працюватиме.");
        }

        // Підписуємося на подію зміни інвентарю від InventoryManager
        inventoryManager.onInventoryChangedCallback += UpdateUI;

        // Ініціалізуємо UI інвентарю
        InitializeUI();
        UpdateUI(); // Перше оновлення UI при запуску

        // Налаштування кнопки розблокування
        if (unlockSlotButton != null)
        {
            unlockSlotButton.onClick.AddListener(OnUnlockSlotButtonClicked); // Додаємо обробник натискання кнопки
            if (unlockCostText != null)
            {
                unlockCostText.text = "Розблокувати слот за " + inventoryManager.unlockSlotCost; // Відображаємо вартість
            }
        }
    }

    // Ініціалізує візуальні слоти інвентарю (створює GameObject'и слотів)
    void InitializeUI()
    {
        // Очищаємо всі існуючі слоти перед створенням нових (для уникнення дублювання)
        foreach (Transform child in slotsParent)
        {
            Destroy(child.gameObject);
        }
        uiSlots.Clear();

        // Створюємо візуальні слоти відповідно до розміру інвентарю
        for (int i = 0; i < inventoryManager.inventorySize; i++)
        {
            GameObject slotGO = Instantiate(inventorySlotPrefab, slotsParent); // Створюємо слот з префаба
            uiSlots.Add(slotGO); // Додаємо його до списку UI слотів

            // Прикріплюємо InventorySlotUI скрипт та передаємо необхідні посилання
            // Оскільки InventorySlotUI тепер окремий скрипт, ми його просто отримуємо або додаємо
            InventorySlotUI slotUIComponent = slotGO.GetComponent<InventorySlotUI>();
            if (slotUIComponent == null)
            {
                slotUIComponent = slotGO.AddComponent<InventorySlotUI>(); // Додаємо, якщо його немає в префабі
            }
            slotUIComponent.inventorySlot = inventoryManager.GetSlot(i); // Передаємо логічні дані слота
            slotUIComponent.slotIndex = i; // Передаємо індекс слота
            slotUIComponent.parentUI = this; // Передаємо посилання на цей InventoryUI

            slotUIComponent.UpdateSlotDisplay(); // Оновлюємо початковий вигляд слота
        }
    }

    // Оновлює весь UI інвентарю (викликається при зміні інвентаря)
    void UpdateUI()
    {
        for (int i = 0; i < uiSlots.Count; i++)
        {
            InventorySlotUI slotUIComponent = uiSlots[i].GetComponent<InventorySlotUI>();
            if (slotUIComponent != null)
            {
                slotUIComponent.inventorySlot = inventoryManager.GetSlot(i); // Оновлюємо логічні дані
                slotUIComponent.UpdateSlotDisplay(); // Оновлюємо візуальне відображення
            }
        }
        CheckUnlockButtonState(); // Оновлюємо стан кнопки розблокування
    }

    // Викликається InventorySlotUI при початку перетягування предмета
    public void StartDraggingItem(InventorySlot slotData, int slotIndex, Sprite itemSprite)
    {
        draggedSlotData = slotData; // Зберігаємо дані слота, що перетягується
        draggedSlotIndex = slotIndex; // Зберігаємо індекс слота, що перетягується

        draggedItemImage.sprite = itemSprite; // Встановлюємо спрайт предмета на зображення перетягування
        draggedItemImage.gameObject.SetActive(true); // Показуємо зображення
        draggedItemImage.SetNativeSize(); // Встановлюємо оригінальний розмір спрайта
        draggedItemImage.transform.SetParent(canvas.transform); // Переміщуємо на Canvas, щоб воно відображалося зверху всіх інших елементів UI
    }

    // Викликається InventorySlotUI при завершенні перетягування предмета
    public void EndDraggingItem()
    {
        draggedItemImage.gameObject.SetActive(false); // Приховуємо зображення перетягування
        draggedItemImage.transform.SetParent(transform); // Повертаємо на місце (для чистоти)
        draggedItemImage.rectTransform.localPosition = Vector3.zero; // Скидаємо позицію

        // Перевіряємо, чи був предмет скинутий поза межами інвентаря (щоб викинути його)
        // EventSystem.current.currentSelectedGameObject == null перевіряє, чи курсор над порожнім UI простором
        // RectTransformUtility.RectangleContainsScreenPoint перевіряє, чи курсор знаходиться всередині RectTransform
        if (EventSystem.current.currentSelectedGameObject == null ||
            (inventoryPanelRectTransform != null && !RectTransformUtility.RectangleContainsScreenPoint(inventoryPanelRectTransform, Input.mousePosition, Camera.main)))
        {
            DropItemInWorld(draggedSlotData, draggedSlotIndex); // Викликаємо метод викидання у світ
        }

        draggedSlotData = null; // Очищаємо дані про перетягуваний слот
        draggedSlotIndex = -1; // Скидаємо індекс
        UpdateUI(); // Оновлюємо весь UI на випадок, якщо щось змінилося
    }

    // Обробляє скидання предмета з одного слота на інший (викликається InventorySlotUI)
    public void HandleItemDrop(int sourceIndex, int targetIndex)
    {
        if (sourceIndex == targetIndex) return; // Якщо предмет скинули на той самий слот, нічого не робимо

        // Викликаємо метод обміну слотів у InventoryManager
        inventoryManager.SwapSlots(sourceIndex, targetIndex);
        UpdateUI(); // Оновлюємо UI після обміну
    }

    // Метод для викидання предмета у світ (поки що тільки логування)
    private void DropItemInWorld(InventorySlot itemToDrop, int sourceSlotIndex)
    {
        if (itemToDrop == null || itemToDrop.IsEmpty()) return;

        // ТУТ МАЄ БУТИ ЛОГІКА СПАВНУ РЕАЛЬНОГО GAMEOBJECT ПРЕДМЕТА У СВІТІ
        // Наприклад: Instantiate(itemToDrop.item.worldPrefab, playerTransform.position, Quaternion.identity);
        Debug.Log($"Викинуто {itemToDrop.quantity} x {itemToDrop.item.itemName} у світ з слота {sourceSlotIndex}.");

        // Очищаємо слот в інвентарі
        inventoryManager.GetSlot(sourceSlotIndex).ClearSlot();
        inventoryManager.onInventoryChangedCallback?.Invoke(); // Повідомляємо UI про зміну
    }


    // Обробник натискання кнопки розблокування слота
    public void OnUnlockSlotButtonClicked()
    {
        for (int i = 0; i < inventoryManager.slots.Count; i++)
        {
            if (inventoryManager.slots[i].isLocked) // Знаходимо перший заблокований слот
            {
                // Тут можна додати перевірку на наявність ресурсів у гравця для розблокування
                if (inventoryManager.UnlockSlot(i)) // Спробуємо розблокувати слот
                {
                    UpdateUI(); // Оновлюємо UI
                    CheckUnlockButtonState(); // Оновлюємо стан кнопки розблокування
                    return; // Виходимо після успішного розблокування
                }
                break; // Зупинити після спроби розблокувати перший знайдений
            }
        }
        Debug.Log("Немає заблокованих слотів для розблокування або недостатньо ресурсів.");
    }

    // Перевіряє стан кнопки розблокування (чи є ще заблоковані слоти)
    private void CheckUnlockButtonState()
    {
        if (unlockSlotButton != null)
        {
            bool hasLockedSlots = false;
            foreach (var slot in inventoryManager.slots)
            {
                if (slot.isLocked)
                {
                    hasLockedSlots = true;
                    break;
                }
            }
            unlockSlotButton.interactable = hasLockedSlots; // Вимкнути кнопку, якщо немає заблокованих слотів
        }
    }
}
