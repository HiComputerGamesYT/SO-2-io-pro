using UnityEngine;
using System.Collections;

// Типи поведінки ШІ
public enum AiType { Hostile, Neutral, Friendly }
// Поточні стани моба
public enum AiState { Idle, Wandering, Chasing, Attacking, Fleeing }

// Клас для налаштування предметів, що випадають
[System.Serializable]
public class LootDrop
{
    public Item item;
    public int quantity;
    [Range(0f, 1f)]
    public float dropChance = 1f;
}

[RequireComponent(typeof(Rigidbody2D))]
public class MobController : MonoBehaviour
{
    [Header("1. Основна Поведінка")]
    [Tooltip("Ставлення до гравця та інших мобів. Hostile - ворог, Neutral - нейтрал, Friendly - союзник.")]
    public AiType aiType = AiType.Neutral;

    [Header("2. Реакції на Гравця")]
    [Tooltip("Чи буде моб тікати, якщо гравець просто підійде близько?")]
    public bool fleesOnApproach = false;
    [Tooltip("Радіус, в якому моб починає тікати від гравця.")]
    public float fleeRadius = 5f;
    [Tooltip("Чи буде моб атакувати у відповідь, якщо його вдарити?")]
    public bool fightsBackWhenAttacked = true;

    [Header("3. Бойові Характеристики")]
    public float maxHealth = 50f;
    public float moveSpeed = 2f;
    public float damage = 10f;
    [Tooltip("Радіус, в якому ворожий моб помічає гравця і починає переслідування.")]
    public float aggroRadius = 8f;
    [Tooltip("Радіус, в якому моб може атакувати.")]
    public float attackRadius = 1.5f;
    public float attackCooldown = 1f;

    [Header("4. Параметри Втечі")]
    [Tooltip("Відсоток здоров'я (0-1), при якому моб почне тікати, навіть якщо він сміливий.")]
    [Range(0f, 1f)]
    public float fleeHealthThreshold = 0.2f;

    [Header("5. Випадаючі предмети (Лут)")]
    public LootDrop[] lootTable;

    [Header("6. Візуальні Елементи")]
    [Tooltip("Трансформ смужки здоров'я, яка буде масштабуватися.")]
    public Transform healthBarFill;

    // Приватні змінні
    private float currentHealth;
    private AiState currentState;
    private Transform playerTransform;
    private Transform currentTarget;
    private Rigidbody2D rb;
    private float nextAttackTime = 0;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
        }

        currentState = AiState.Idle;
        StartCoroutine(StateMachine());
    }

    void Update()
    {
        HandleRotation();
        if (healthBarFill != null && healthBarFill.parent != null)
        {
            healthBarFill.parent.rotation = Quaternion.identity;
        }
    }

    private void HandleRotation()
    {
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg + 90f;
            rb.rotation = Mathf.LerpAngle(rb.rotation, angle, Time.deltaTime * 10f);
        }
    }

    private IEnumerator StateMachine()
    {
        while (true)
        {
            if (currentTarget == null)
            {
                FindTarget();
            }

            switch (currentState)
            {
                case AiState.Idle: UpdateIdleState(); break;
                case AiState.Wandering: UpdateWanderingState(); break;
                case AiState.Chasing: UpdateChasingState(); break;
                case AiState.Attacking: UpdateAttackingState(); break;
                case AiState.Fleeing: UpdateFleeingState(); break;
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void FindTarget()
    {
        if (playerTransform == null) return;

        if (aiType == AiType.Hostile && IsInRange(playerTransform.position, aggroRadius))
        {
            currentTarget = playerTransform;
            currentState = AiState.Chasing;
        }
    }

    #region Логіка станів

    private void UpdateIdleState()
    {
        rb.linearVelocity = Vector2.zero;
        if (playerTransform != null && fleesOnApproach && IsInRange(playerTransform.position, fleeRadius))
        {
            currentTarget = playerTransform;
            currentState = AiState.Fleeing;
        }
    }

    private void UpdateWanderingState()
    {
        // TODO: Додати логіку блукання
        currentState = AiState.Idle;
    }

    private void UpdateChasingState()
    {
        if (currentTarget == null || !IsInRange(currentTarget.position, aggroRadius * 1.2f))
        {
            currentTarget = null;
            currentState = AiState.Idle;
            return;
        }
        if (IsInRange(currentTarget.position, attackRadius))
        {
            currentState = AiState.Attacking;
            return;
        }
        Vector2 direction = (currentTarget.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    private void UpdateAttackingState()
    {
        if (currentTarget == null || !IsInRange(currentTarget.position, attackRadius * 1.2f))
        {
            currentState = AiState.Chasing;
            return;
        }

        rb.linearVelocity = Vector2.zero;

        if (Time.time >= nextAttackTime)
        {
            if (currentTarget.TryGetComponent<PlayerController>(out PlayerController player))
            {
                player.TakeDamage(damage);
            }
            else if (currentTarget.TryGetComponent<MobController>(out MobController mob))
            {
                mob.TakeDamage(damage);
            }
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void UpdateFleeingState()
    {
        if (currentTarget == null || !IsInRange(currentTarget.position, fleeRadius * 1.5f))
        {
            currentTarget = null;
            currentState = AiState.Idle;
            return;
        }
        Vector2 direction = (transform.position - currentTarget.position).normalized;
        rb.linearVelocity = direction * moveSpeed * 1.5f;
    }

    #endregion

    public void TakeDamage(float incomingDamage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= incomingDamage;
        UpdateHealthBar();

        if (aiType == AiType.Neutral)
        {
            currentState = fightsBackWhenAttacked ? AiState.Chasing : AiState.Fleeing;
            if (playerTransform != null) currentTarget = playerTransform;
        }
        else if (currentHealth <= maxHealth * fleeHealthThreshold)
        {
            currentState = AiState.Fleeing;
            if (playerTransform != null) currentTarget = playerTransform;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);
            healthBarFill.localScale = new Vector3(healthPercent, 1f, 1f);
        }
    }

    private void Die()
    {
        foreach (var drop in lootTable)
        {
            if (Random.value < drop.dropChance)
            {
                SpawnLoot(drop.item, drop.quantity);
            }
        }
        Destroy(gameObject);
    }

    private void SpawnLoot(Item itemToSpawn, int quantity)
    {
        if (itemToSpawn == null || quantity <= 0 || InventoryManager.instance.worldItemPrefab == null) return;

        Vector3 spawnPosition = transform.position + (Vector3)Random.insideUnitCircle * 0.5f;
        GameObject itemObject = Instantiate(InventoryManager.instance.worldItemPrefab, spawnPosition, Quaternion.identity);

        SpriteRenderer sr = itemObject.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingLayerName = "Items";

        WorldItem worldItem = itemObject.GetComponent<WorldItem>();
        if (worldItem != null)
        {
            worldItem.itemData = itemToSpawn;
            worldItem.quantity = quantity;
        }
    }

    private bool IsInRange(Vector3 targetPosition, float range)
    {
        return Vector2.Distance(transform.position, targetPosition) <= range;
    }
} // <--- ОСЬ ДУЖКА, ЯКОЇ НЕ ВИСТАЧАЛО