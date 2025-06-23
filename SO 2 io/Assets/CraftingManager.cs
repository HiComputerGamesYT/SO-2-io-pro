using UnityEngine;
using System.Collections.Generic;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager instance;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
    }

    [Header("Налаштування Крафту")]
    [Tooltip("Список всіх рецептів, доступних у грі.")]
    public List<CraftingRecipe> recipes;

    private InventoryManager inventoryManager;

    void Start()
    {
        inventoryManager = InventoryManager.instance;
    }

    public void Craft(CraftingRecipe recipe)
    {
        if (recipe == null || inventoryManager == null) return;

        // 1. Перевіряємо, чи можемо ми скрафтити цей предмет
        if (CanCraft(recipe))
        {
            Debug.Log($"<color=green>[Crafting] Успіх! Створюємо '{recipe.outputItem.itemName}'.</color>");
            // 2. Якщо можемо, забираємо інгредієнти
            RemoveIngredients(recipe);
            // 3. Додаємо результат в інвентар
            inventoryManager.AddItem(recipe.outputItem, recipe.outputQuantity);
        }
        else
        {
            Debug.LogWarning($"<color=red>[Crafting] Недостатньо ресурсів для '{recipe.outputItem.itemName}'.</color>");
        }
    }

    public bool CanCraft(CraftingRecipe recipe)
    {
        Debug.Log($"--- ПЕРЕВІРКА РЕЦЕПТУ: '{recipe.name}' ---");
        foreach (var ingredient in recipe.ingredients)
        {
            if (!inventoryManager.HasItem(ingredient.item, ingredient.quantity))
            {
                Debug.LogError($"[ПЕРЕВІРКА ПРОВАЛЕНА] Не знайдено достатньо '{ingredient.item.itemName}' в інвентарі.");
                return false;
            }
            Debug.Log($"[ПЕРЕВІРКА УСПІШНА] Знайдено достатньо '{ingredient.item.itemName}'.");
        }
        Debug.Log($"--- УСІ ІНГРЕДІЄНТИ для '{recipe.name}' ЗНАЙДЕНО! ---");
        return true;
    }

    private void RemoveIngredients(CraftingRecipe recipe)
    {
        Debug.Log($"--- ВИДАЛЕННЯ ІНГРЕДІЄНТІВ для '{recipe.name}' ---");
        foreach (var ingredient in recipe.ingredients)
        {
            inventoryManager.RemoveItem(ingredient.item, ingredient.quantity);
        }
    }
}
