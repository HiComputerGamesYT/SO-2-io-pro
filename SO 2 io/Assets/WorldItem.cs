using UnityEngine;

// ��� ��������� ����� �������� �� ������� ��������, �� ������ �� ����.
[RequireComponent(typeof(SpriteRenderer))]
public class WorldItem : MonoBehaviour
{
    [Header("��� ��������")]
    [Tooltip("��� �������� (ScriptableObject), ���� ����������� ��� ��'���.")]
    public Item itemData;

    [Tooltip("ʳ������ �������� � ����� �����.")]
    [Range(1, 99)]
    public int quantity = 1;

    void Start()
    {
        // ����������� ������������ ������ �� ����� ����� � Item, ���� �� �� ������������ ������.
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null && itemData != null)
        {
            sr.sprite = itemData.icon;
        }
    }

    /// <summary>
    /// ��� ����� ����������� ����� (���������, �������), ��� ������� �������.
    /// </summary>
    public void PickUp()
    {
        if (InventoryManager.instance == null)
        {
            Debug.LogError("�� ������� ������ InventoryManager.instance! ������� �� ���� ���� ��������.");
            return;
        }

        // ���������� ������ ������� �� ��������� ������.
        bool wasPickedUp = InventoryManager.instance.AddItem(itemData, quantity);

        // ���� ������� ���� ������ ������ (�������), ������� ���� ��'��� � ����.
        if (wasPickedUp)
        {
            Destroy(gameObject);
        }
        else
        {
            // ���� �������� ����������, ������� �� ���������� � ���������� �� ����.
            Debug.Log("�������� ����������! ��������� ������� " + itemData.itemName);
            // ��� ����� ������ ���� ��� ��������� �����, ���� ������ ������, �� �������� ������.
        }
    }
}
