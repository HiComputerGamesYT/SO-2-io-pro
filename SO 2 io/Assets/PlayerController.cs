using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Рух")]
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.5f;
    public float runEnergyCostPerSecond = 5f;

    [Header("Атака")]
    public float baseDamage = 5f;
    public float attackCooldown = 0.5f;
    public int attackEnergyCost = 5;

    [Header("Анімація Атаки")]
    public float swingAngle = 75f;
    public float swingDuration = 0.2f;

    [Header("Взаємодія")]
    public KeyCode pickupKey = KeyCode.E;
    public float pickupRadius = 1.5f;
    public LayerMask itemLayer;
    public LayerMask resourceLayer;

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

        if (Input.GetMouseButton(0) && Time.time >= nextAttackTime)
        {
            PerformAttack();
        }

        if (Input.GetKeyDown(pickupKey))
        {
            TryPickupItem();
        }

        HandleHotbarInput();
    }

    private void UpdateHeldItemVisual()
    {
        if (weaponHolderRenderer == null) return;
        Item activeItem = InventoryManager.instance.GetActiveItem();
        if (activeItem != null && activeItem.itemType == ItemType.Tool)
        {
            weaponHolderRenderer.sprite = activeItem.icon;
        }
        else
        {
            weaponHolderRenderer.sprite = null;
        }
    }

    private void PerformAttack()
    {
        if (currentEnergy < attackEnergyCost) return;

        currentEnergy -= attackEnergyCost;
        nextAttackTime = Time.time + attackCooldown;

        Vector2 attackPosition = (Vector2)transform.position + (Vector2)(transform.up * -1f * 0.7f);
        float attackRadius = 0.5f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPosition, attackRadius, resourceLayer);

        float currentDamage = baseDamage;
        Item activeItem = InventoryManager.instance.GetActiveItem();
        if (activeItem != null && activeItem.itemType == ItemType.Tool)
        {
            currentDamage = activeItem.damage;
            if (weaponHolderRenderer != null && weaponSwingCoroutine == null)
            {
                weaponSwingCoroutine = StartCoroutine(WeaponSwingAnimation());
            }
        }

        // --- ЗМІНА: Тепер перевіряємо не тільки ресурси, а й мобів ---
        foreach (var hit in hits)
        {
            // Спочатку перевіряємо, чи це моб
            if (hit.TryGetComponent<MobController>(out MobController mob))
            {
                mob.TakeDamage(currentDamage);
            }
            // Якщо не моб, перевіряємо, чи це ресурс
            else if (hit.TryGetComponent<ResourceSource>(out ResourceSource resource))
            {
                resource.TakeDamage(currentDamage, transform);
            }
        }
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

    private void TryPickupItem()
    {
        var colliders = Physics2D.OverlapCircleAll(transform.position, pickupRadius, itemLayer);
        if (colliders.Length == 0) return;

        var closestCollider = colliders.OrderBy(c => (c.transform.position - transform.position).sqrMagnitude).FirstOrDefault();

        if (closestCollider != null && closestCollider.TryGetComponent<WorldItem>(out var worldItem))
        {
            worldItem.PickUp();
        }
    }

    private void HandleHotbarInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) InventoryManager.instance.SetActiveSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) InventoryManager.instance.SetActiveSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) InventoryManager.instance.SetActiveSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) InventoryManager.instance.SetActiveSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) InventoryManager.instance.SetActiveSlot(4);
    }

    private void HandleMovement()
    {
        float currentSpeed = moveSpeed;
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Return)) && moveInput.magnitude > 0 && currentEnergy > 0)
        {
            currentSpeed *= runSpeedMultiplier;
            currentEnergy -= runEnergyCostPerSecond * Time.fixedDeltaTime;
        }
        rb.linearVelocity = moveInput.normalized * currentSpeed;

        if (mapBoundsSet)
        {
            rb.position = new Vector2(Mathf.Clamp(rb.position.x, mapMinX, mapMaxX), Mathf.Clamp(rb.position.y, mapMinY, mapMaxY));
        }
    }

    private void HandleRotationTowardsMouse()
    {
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 lookDirection = mouseWorldPosition - transform.position;
        rb.rotation = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg + 90f;
    }

    // --- ДОДАНО: Метод для отримання шкоди ---
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log($"Гравець отримав {amount} шкоди, залишилось {currentHealth} HP");
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
        // TODO: Логіка смерті, наприклад, перезапуск сцени
    }
    // ------------------------------------------

    private void RegenerateEnergy()
    {
        bool isRunning = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Return)) && rb.linearVelocity.magnitude > 0.1f;
        if (!Input.GetMouseButton(0) && !isRunning && currentEnergy < maxEnergy)
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
}