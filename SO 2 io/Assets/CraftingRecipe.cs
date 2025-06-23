using UnityEngine;
using System.Collections.Generic;

// Цей клас описує один інгредієнт у рецепті (предмет та його кількість)
[System.Serializable]
public class Ingredient
{
    public Item item; // Який предмет потрібен
    [Range(1, 99)]
    public int quantity; // Скільки штук
}

// Атрибут CreateAssetMenu дозволяє створювати Рецепти через меню Assets > Create
[CreateAssetMenu(fileName = "New Recipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Інгредієнти")]
    [Tooltip("Список предметів, необхідних для створення.")]
    public List<Ingredient> ingredients;

    [Header("Результат")]
    [Tooltip("Предмет, який буде створено.")]
    public Item outputItem;

    [Tooltip("Кількість предметів, які будуть створені.")]
    [Range(1, 99)]
    public int outputQuantity = 1;
}
