using UnityEngine;

public class ResourceSource : MonoBehaviour
{
    [Header("������������ ������'�")]
    public float maxHealth = 100f;

    [Header("������������ ���������")]
    public Item lootItem;
    public int lootQuantityOnDestroy = 5;
    [Range(0f, 1f)]
    public float dropOnHitChance = 0.5f;

    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // ����� ��� ����� ������ ���������� ��� ����, ��� ������ �����
    public void TakeDamage(float damage, Transform damageDealer)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;

        if (Random.value < dropOnHitChance)
        {
            // �������� ���������� ��� ������ ���
            SpawnLoot(1, damageDealer);
        }

        if (currentHealth <= 0)
        {
            Die(damageDealer);
        }
    }

    private void Die(Transform damageDealer)
    {
        // �������� ���������� ��� ������ ���
        SpawnLoot(lootQuantityOnDestroy, damageDealer);
        Destroy(gameObject);
    }

    // ����� ��� ����� ���, �� ����������� �������
    private void SpawnLoot(int quantity, Transform damageDealer)
    {
        if (lootItem == null || InventoryManager.instance.worldItemPrefab == null) return;

        // --- ��в����� �������� ---
        // ��������� ��'��� ��� ������, � �� ��� ������
        Vector3 spawnPosition = damageDealer.position + (Vector3)Random.insideUnitCircle * 0.5f;
        GameObject itemObject = Instantiate(InventoryManager.instance.worldItemPrefab, spawnPosition, Quaternion.identity);

        SpriteRenderer sr = itemObject.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Items";
        }

        WorldItem worldItem = itemObject.GetComponent<WorldItem>();
        if (worldItem != null)
        {
            worldItem.itemData = lootItem;
            worldItem.quantity = quantity;
        }
    }
}