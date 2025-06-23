using UnityEngine;
using System.Collections.Generic;

public class CraftingUI : MonoBehaviour
{
    [Header("Основні Налаштування")]
    [Tooltip("Головний об'єкт панелі крафту, який буде вмикатися/вимикатися.")]
    public GameObject craftingPanel;
    [Tooltip("Клавіша для відкриття/закриття вікна крафту.")]
    public KeyCode toggleKey = KeyCode.C;

    [Header("UI Елементи (призначте в інспекторі)")]
    [Tooltip("Панель, де будуть відображатися всі слоти з рецептами.")]
    public Transform recipeSlotsParent;
    [Tooltip("Префаб одного слота (кнопки) для рецепту.")]
    public GameObject craftingSlotPrefab;

    private List<CraftingSlotUI> currentSlots = new List<CraftingSlotUI>();

    void Start()
    {
        if (craftingPanel == null || recipeSlotsParent == null || craftingSlotPrefab == null)
        {
            Debug.LogError("CraftingUI: Не всі поля призначені в інспекторі! (Crafting Panel, Recipe Slots Parent, Crafting Slot Prefab)");
            return;
        }

        // Підписуємося на подію зміни інвентарю, щоб оновлювати кнопки
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.onInventoryChangedCallback += UpdateAllSlotsStatus;
        }

        // Ініціалізуємо і ховаємо вікно при старті
        InitializeCraftingUI();
        craftingPanel.SetActive(false);
    }

    void Update()
    {
        // Відкриваємо/закриваємо вікно по натисканню клавіші
        if (Input.GetKeyDown(toggleKey))
        {
            bool isActive = !craftingPanel.activeSelf;
            craftingPanel.SetActive(isActive);

            // Якщо вікно відкривається, оновлюємо статус рецептів
            if (isActive)
            {
                UpdateAllSlotsStatus();
            }
        }
    }

    void OnDestroy()
    {
        // Важливо відписатися від події, щоб уникнути помилок
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.onInventoryChangedCallback -= UpdateAllSlotsStatus;
        }
    }

    /// <summary>
    /// Створює слоти для всіх доступних рецептів.
    /// </summary>
    void InitializeCraftingUI()
    {
        foreach (Transform child in recipeSlotsParent)
        {
            Destroy(child.gameObject);
        }
        currentSlots.Clear();

        CraftingManager craftingManager = CraftingManager.instance;
        if (craftingManager != null && craftingManager.recipes != null)
        {
            foreach (CraftingRecipe recipe in craftingManager.recipes)
            {
                GameObject slotGO = Instantiate(craftingSlotPrefab, recipeSlotsParent);
                CraftingSlotUI slotUI = slotGO.GetComponent<CraftingSlotUI>();
                if (slotUI != null)
                {
                    slotUI.Setup(recipe);
                    currentSlots.Add(slotUI);
                }
            }
        }
    }

    /// <summary>
    /// Оновлює статус (можна/не можна скрафтити) для всіх слотів.
    /// </summary>
    void UpdateAllSlotsStatus()
    {
        if (craftingPanel.activeSelf == false) return; // Не оновлювати, якщо вікно закрите

        foreach (CraftingSlotUI slot in currentSlots)
        {
            slot.UpdateCraftableStatus();
        }
    }
}
