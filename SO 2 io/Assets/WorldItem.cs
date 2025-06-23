using UnityEngine;

// ��� ������ ���� �������������� �� GameObject'��, �� ������������� �������� �� ����
// � �� ������� ���� �������.
public class WorldItem : MonoBehaviour
{
    [Tooltip("���������� �������� (ScriptableObject), ���� ���� �������.")]
    public Item itemData; // ���� ���������� ��� Asset "Item" (���������, StoneItem)
    [Tooltip("ʳ������ ����� ��������, ��� ���� �������.")]
    [Range(1, 99)]
    public int quantity = 1;

    [Tooltip("����� ���� ������ ��������.")]
    public float pickupRadius = 1f;

    // ������� ��� ��������� �� InventoryManager
    private InventoryManager inventoryManager;

    void Awake()
    {
        // �������������, �� �� ��'��� � Collider2D � �� � ��������.
        // ���� ���� ����, ������ ���� �����������.
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>(); // ����� BoxCollider2D, �������� �� �������
            (col as CircleCollider2D).radius = pickupRadius;
        }
        col.isTrigger = true; // ������ �������� �������� ��� �������� �����

        // ������ InventoryManager � ����
        inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogError("WorldItem: InventoryManager �� �������� � ����. ϳ��� �������� �� �����������.");
        }
    }

    // ����� ��� ������ ��������
    public void PickUp()
    {
        if (inventoryManager != null && itemData != null)
        {
            // ���������� ������ ������� �� ���������
            bool addedSuccessfully = inventoryManager.AddItem(itemData, quantity);

            if (addedSuccessfully)
            {
                Debug.Log($"ϳ������ {quantity} x {itemData.itemName}.");
                Destroy(gameObject); // ������� ��'��� �������� � ����
            }
            else
            {
                Debug.Log($"�� ������� ������� {itemData.itemName}. �������� ����������.");
            }
        }
    }

    // ³��������� ������ ������ � ��������
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
