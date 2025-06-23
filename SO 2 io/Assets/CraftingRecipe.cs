using UnityEngine;
using System.Collections.Generic;

// ��� ���� ����� ���� �����䳺�� � ������ (������� �� ���� �������)
[System.Serializable]
public class Ingredient
{
    public Item item; // ���� ������� �������
    [Range(1, 99)]
    public int quantity; // ������ ����
}

// ������� CreateAssetMenu �������� ���������� ������� ����� ���� Assets > Create
[CreateAssetMenu(fileName = "New Recipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("�����䳺���")]
    [Tooltip("������ ��������, ���������� ��� ���������.")]
    public List<Ingredient> ingredients;

    [Header("���������")]
    [Tooltip("�������, ���� ���� ��������.")]
    public Item outputItem;

    [Tooltip("ʳ������ ��������, �� ������ �������.")]
    [Range(1, 99)]
    public int outputQuantity = 1;
}
