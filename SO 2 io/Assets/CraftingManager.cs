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

    [Header("������������ ������")]
    [Tooltip("������ ��� �������, ��������� � ��.")]
    public List<CraftingRecipe> recipes;

    private InventoryManager inventoryManager;

    void Start()
    {
        inventoryManager = InventoryManager.instance;
    }

    public void Craft(CraftingRecipe recipe)
    {
        if (recipe == null || inventoryManager == null) return;

        // 1. ����������, �� ������ �� ��������� ��� �������
        if (CanCraft(recipe))
        {
            Debug.Log($"<color=green>[Crafting] ����! ��������� '{recipe.outputItem.itemName}'.</color>");
            // 2. ���� ������, �������� �����䳺���
            RemoveIngredients(recipe);
            // 3. ������ ��������� � ��������
            inventoryManager.AddItem(recipe.outputItem, recipe.outputQuantity);
        }
        else
        {
            Debug.LogWarning($"<color=red>[Crafting] ����������� ������� ��� '{recipe.outputItem.itemName}'.</color>");
        }
    }

    public bool CanCraft(CraftingRecipe recipe)
    {
        Debug.Log($"--- ����²��� �������: '{recipe.name}' ---");
        foreach (var ingredient in recipe.ingredients)
        {
            if (!inventoryManager.HasItem(ingredient.item, ingredient.quantity))
            {
                Debug.LogError($"[����²��� ���������] �� �������� ��������� '{ingredient.item.itemName}' � ��������.");
                return false;
            }
            Debug.Log($"[����²��� ��ϲ���] �������� ��������� '{ingredient.item.itemName}'.");
        }
        Debug.Log($"--- �Ѳ �����Ĳ���� ��� '{recipe.name}' ��������! ---");
        return true;
    }

    private void RemoveIngredients(CraftingRecipe recipe)
    {
        Debug.Log($"--- ��������� �����Ĳ��Ҳ� ��� '{recipe.name}' ---");
        foreach (var ingredient in recipe.ingredients)
        {
            inventoryManager.RemoveItem(ingredient.item, ingredient.quantity);
        }
    }
}
