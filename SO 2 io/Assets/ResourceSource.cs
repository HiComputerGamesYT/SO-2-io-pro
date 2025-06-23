using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ResourceSource : MonoBehaviour
{
    [Header("������������ �������")]
    [Tooltip("����������� ������'� ������� (���������, 100 ��� ������).")]
    public float maxHealth = 100f;

    [Header("��� (�� ������)")]
    [Tooltip("�������, ���� ������.")]
    public Item lootItem;

    [Tooltip("���� �������� 1 ������� ���� ��� ������� ����.")]
    [Range(0f, 1f)]
    public float dropChanceOnHit = 0.3f; // 30% ����

    [Tooltip("ʳ������ ��������, �� ����������� ��������� ��� �������.")]
    public int dropQuantityOnDestroy = 5;

    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        // ������������, �� �������� �� � ��������, ��� �� ��� �������� ����������
        GetComponent<Collider2D>().isTrigger = false;
    }

    /// <summary>
    /// ����� ��� ��������� ����� �������.
    /// </summary>
    /// <param name="damageAmount">ʳ������ �����.</param>
    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log($"<color=orange>{gameObject.name} ������� {damageAmount} �����. ���������� ������'�: {currentHealth}/{maxHealth}</color>");

        // ���� �������� ��� ��� ����
        if (Random.value <= dropChanceOnHit)
        {
            SpawnLoot(1);
            Debug.Log($"<color=green>����� 1 {lootItem.itemName} ��� ����!</color>");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// �����, �� ����������� ��� ������� �������.
    /// </summary>
    private void Die()
    {
        Debug.Log($"<color=red>{gameObject.name} �������!</color>");
        // ������������ ��� ��� �������
        SpawnLoot(dropQuantityOnDestroy);
        Destroy(gameObject);
    }

    private void SpawnLoot(int quantity)
    {
        if (lootItem == null || InventoryManager.instance.worldItemPrefab == null) return;

        GameObject droppedLoot = Instantiate(InventoryManager.instance.worldItemPrefab, transform.position, Quaternion.identity);

        WorldItem worldItem = droppedLoot.GetComponent<WorldItem>();
        if (worldItem != null)
        {
            worldItem.itemData = lootItem;
            worldItem.quantity = quantity;
        }
    }
}
