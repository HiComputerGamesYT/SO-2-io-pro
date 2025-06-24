using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public enum ItemType
    {
        Resource,
        Tool,
        Block
    }

    [Header("������� ����������")]
    public string itemName = "New Item";
    public Sprite icon = null;

    public ItemType itemType = ItemType.Resource;

    [Tooltip("����������� ������� �������� � ������ �����.")]
    public int maxStack = 16;

    [Header("��� �����������")]
    public float damage = 10f;

    // --- ������ ��² ���� ��� ---
    [Header("��� ���������� �����")]
    [Tooltip("���������� ���� ����� �����, ���� ������� ����� ��������")]
    public TileBase correspondingTile;

    [Tooltip("ʳ������ ������'�, ��� �� ���� ���� ������������.")]
    public int blockHealth = 50;

    [Tooltip("�������, ���� ���� ������� ��� ��������� ����� (������� �������, ���� ����� �� ������).")]
    public Item itemToDrop;

    [Tooltip("���� �������� �������� (�� 0 �� 1, �� 1 = 100%).")]
    [Range(0f, 1f)]
    public float dropChance = 0.5f;
    // --------------------------------

    [Header("������ ��� ���� ���")]
    [Tooltip("������, ���� �'��������� � ���, ���� ������� ���������")]
    public GameObject itemPrefab;
}