using UnityEngine;

// ������ ������, ��� ��������� ��������
public enum ItemType { Resource, Tool, Consumable }

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Header("������� ����������")]
    public string itemName = "����� �������";
    [TextArea(3, 5)]
    public string description = "���� ����� ��������.";
    public Sprite icon = null;
    [Range(1, 99)]
    public int maxStack = 1;
    public ItemType itemType = ItemType.Resource;

    [Header("������������ ����������� (���� �� ����������)")]
    [Tooltip("�����, ��� ���������� �������� ��������. ����������, ���� ��� �������� - �� Tool.")]
    public float damage = 10f;
}
