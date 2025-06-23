using UnityEngine;

 ������� CreateAssetMenu �������� ���������� ��'���� ����� ���� ����� ���� Assets/Create
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Tooltip("�������� ����� ��������.")]
    public string itemName = "New Item";
    [Tooltip("���� ��������.")]
    [TextArea(3, 5)]
    public string description = "���� ��������.";
    [Tooltip("������, �� ������������ � ��������.")]
    public Sprite icon = null;
    [Tooltip("����������� ������� �������� � ������ ���� ���������.")]
    [Range(1, 99)] // ��������� �� 99, �� �� �������
    public int maxStack = 1;
}
