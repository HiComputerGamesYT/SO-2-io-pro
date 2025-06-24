using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Tilemaps; // ДОДАНО: Для роботи з тайлмапами

public class PlayerController : MonoBehaviour
{
    // Всі ваші налаштування залишаються без змін
    [Header("Рух")]
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.5f;
    public float runEnergyCostPerSecond = 5f;

    [Header("Атака")]
    public float baseDamage = 5f;
    public float attackCooldown = 0.5f;
    public int attackEnergyCost = 5;
    [SerializeField] private float attackOffset = 0.7f;
    [SerializeField] private float attackRadius = 0.5f;

    [Header("Анімація Атаки")]
    public float swingAngle = 75f;
    public float swingDuration = 0.2f;

    [Header("Взаємодія")]
    public KeyCode pickupKey = KeyCode.E;
    public float pickupRadius = 1.5f;
    public LayerMask itemLayer;
    [SerializeField] private KeyCode[] hotbarKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5 };

    [Header("Характеристики")]
    public int maxHealth = 100;
    public int maxEnergy = 100;
    public float energyRegenRate = 10f;

    [Header("UI та Візуальні Елементи")]
    public TextMeshProUGUI healthText;
    public Image healthFillImage;
    public TextMeshProUGUI energyText;
    public Image energyFillImage;
    public SpriteRenderer weaponHolderRenderer;

    // Всі ваші приватні змінні збережено
    private Rigidbody2D rb;
    private Camera mainCamera;
    private float currentHealth, currentEnergy, nextAttackTime;
    private Vector2 moveInput;
    private float mapMinX, mapMaxX, mapMinY, mapMaxY;
    private bool mapBoundsSet = false;
    private Coroutine weaponSwingCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
    }

    void Start()
    {
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.onInventoryChangedCallback += UpdateHeldItemVisual;
        }
        UpdateHeldItemVisual();
    }

    void OnDestroy()
    {
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.onInventoryChangedCallback -= UpdateHeldItemVisual;
        }
    }

    void Update()
    {
        HandleInput();
        HandleRotationTowardsMouse();
        RegenerateEnergy();
        UpdateUI();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleInput()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        // Атака на ліву кнопку миші
        if (Input.GetMouseButtonDown(0))
        {
            PerformAttack();
        }

        // Будівництво на праву кнопку миші
        if (Input.GetMouseButtonDown(1))
        {
            TryPlaceBlock();
        }

        if (Input.GetKeyDown(pickupKey))
        {
            TryPickupItem();
        }

        HandleHotbarInput();
    }

    // --- ОНОВЛЕНО: Логіка атаки тепер може ламати блоки ---
    private void PerformAttack()
    {
        if (currentEnergy < attackEnergyCost || Time.time < nextAttackTime) return;

        currentEnergy -= attackEnergyCost;
        nextAttackTime = Time.time + attackCooldown;

        float currentDamage = baseDamage;
        Item activeItem = InventoryManager.instance.GetActiveItem();
        if (activeItem != null && activeItem.itemType == Item.ItemType.Tool)
        {
            currentDamage = activeItem.damage;
            if (weaponHolderRenderer != null && weaponSwingCoroutine == null)
            {
                weaponSwingCoroutine = StartCoroutine(WeaponSwingAnimation());
            }
        }

        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        // Спочатку перевіряємо, чи не намагаємось ми вдарити по блоку
        Tilemap buildingTilemap = WorldManager.instance.buildingTilemap;
        Vector3Int cellToAttack = buildingTilemap.WorldToCell(mouseWorldPosition);

        // Якщо в цій клітинці є блок, який зареєстрований в WorldManager...
        if (WorldManager.instance.placedBlocksData.ContainsKey(cellToAttack))
        {
            // ... то наносимо шкоду цьому блоку
            WorldManager.instance.DamageTile(cellToAttack, currentDamage);
            return; // Виходимо, щоб не бити мобів крізь стіну
        }

        // Якщо по блоку не вдарили, виконуємо звичайну атаку по мобам/ресурсам
        Vector2 directionToMouse = ((Vector2)mouseWorldPosition - rb.position).normalized;
        Vector2 attackPosition = rb.position + directionToMouse * attackOffset;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPosition, attackRadius);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<MobController>(out MobController mob))
            {
                mob.TakeDamage(currentDamage);
            }
            else if (hit.TryGetComponent<ResourceSource>(out ResourceSource resource))
            {
                resource.TakeDamage(currentDamage, transform);
            }
        }
    }

    // --- ОНОВЛЕНО: Логіка будівництва тепер реєструє блок в WorldManager ---
    private void TryPlaceBlock()
    {
        Item activeItem = InventoryManager.instance.GetActiveItem();

        if (activeItem == null || activeItem.itemType != Item.ItemType.Block || activeItem.correspondingTile == null)
        {
            return;
        }

        Tilemap buildingTilemap = WorldManager.instance.buildingTilemap;
        if (buildingTilemap == null)
        {
            Debug.LogError("Building Tilemap не налаштована в WorldManager!");
            return;
        }

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = buildingTilemap.WorldToCell(mouseWorldPos);

        if (buildingTilemap.GetTile(cellPosition) != null)
        {
            return;
        }

        buildingTilemap.SetTile(cellPosition, activeItem.correspondingTile);

        // Реєструємо наш новий блок в базі даних WorldManager
        WorldManager.instance.RegisterPlacedTile(cellPosition, activeItem);

        InventoryManager.instance.RemoveItem(activeItem, 1);
    }

    // --- ВАШ КОД ЗАЛИШИВСЯ БЕЗ ЗМІН ---
    private void HandleRotationTowardsMouse()
    {
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 lookDirection = mouseWorldPosition - transform.position;
        float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg + 90f;
        rb.rotation = angle;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        UpdateUI();
    }

    private void Die()
    {
        Debug.Log("Гравець помер!");
    }

    private void UpdateHeldItemVisual()
    {
        if (weaponHolderRenderer == null) return;
        Item activeItem = InventoryManager.instance.GetActiveItem();
        if (activeItem != null && activeItem.icon != null)
        {
            weaponHolderRenderer.sprite = activeItem.icon;
        }
        else
        {
            weaponHolderRenderer.sprite = null;
        }
    }

    private void TryPickupItem()
    {
        var colliders = Physics2D.OverlapCircleAll(transform.position, pickupRadius, itemLayer);
        if (colliders.Length == 0) return;

        Collider2D closestCollider = null;
        float minSqrDistance = float.MaxValue;

        foreach (var currentCollider in colliders)
        {
            float sqrDistance = (currentCollider.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < minSqrDistance)
            {
                minSqrDistance = sqrDistance;
                closestCollider = currentCollider;
            }
        }

        if (closestCollider != null && closestCollider.TryGetComponent<WorldItem>(out var worldItem))
        {
            worldItem.PickUp();
        }
    }

    private void HandleHotbarInput()
    {
        for (int i = 0; i < hotbarKeys.Length; i++)
        {
            if (Input.GetKeyDown(hotbarKeys[i]))
            {
                InventoryManager.instance.SetActiveSlot(i);
                break;
            }
        }
    }

    private void HandleMovement()
    {
        float currentSpeed = moveSpeed;
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Return)) && moveInput.magnitude > 0.1f && currentEnergy > 0)
        {
            currentSpeed *= runSpeedMultiplier;
            currentEnergy -= runEnergyCostPerSecond * Time.fixedDeltaTime;
        }

        rb.linearVelocity = moveInput.normalized * currentSpeed;

        if (mapBoundsSet)
        {
            rb.position = new Vector2(
                Mathf.Clamp(rb.position.x, mapMinX, mapMaxX),
                Mathf.Clamp(rb.position.y, mapMinY, mapMaxY)
            );
        }
    }

    private void RegenerateEnergy()
    {
        bool isRunning = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Return)) && rb.linearVelocity.magnitude > 0.1f;
        if (!isRunning && !Input.GetMouseButton(0) && currentEnergy < maxEnergy)
        {
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + energyRegenRate * Time.deltaTime);
        }
    }

    private void UpdateUI()
    {
        if (healthText != null) healthText.text = $"{(int)currentHealth} / {maxHealth}";
        if (healthFillImage != null) healthFillImage.fillAmount = currentHealth / (float)maxHealth;
        if (energyText != null) energyText.text = $"{(int)currentEnergy} / {maxEnergy}";
        if (energyFillImage != null) energyFillImage.fillAmount = currentEnergy / (float)maxEnergy;
    }

    public void SetMapBounds(float minX, float maxX, float minY, float maxY)
    {
        mapMinX = minX; mapMaxX = maxX; mapMinY = minY; mapMaxY = maxY;
        mapBoundsSet = true;
    }

    private IEnumerator WeaponSwingAnimation()
    {
        Quaternion initialRotation = weaponHolderRenderer.transform.localRotation;
        Quaternion startRotation = initialRotation * Quaternion.Euler(0, 0, swingAngle / 2);
        Quaternion endRotation = initialRotation * Quaternion.Euler(0, 0, -swingAngle / 2);
        float halfDuration = swingDuration / 2;
        float elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            weaponHolderRenderer.transform.localRotation = Quaternion.Slerp(startRotation, endRotation, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            weaponHolderRenderer.transform.localRotation = Quaternion.Slerp(endRotation, initialRotation, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        weaponHolderRenderer.transform.localRotation = initialRotation;
        weaponSwingCoroutine = null;
    }
}