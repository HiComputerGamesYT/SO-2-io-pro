using UnityEngine;
using UnityEngine.UI; // Для роботи з UI елементами, такими як Health/Energy Bar

public class PlayerController : MonoBehaviour
{
    // --- Параметри руху ---
    [Header("Рух")]
    [Tooltip("Швидкість нормального пересування персонажа.")]
    public float moveSpeed = 5f;
    [Tooltip("Множник швидкості, коли персонаж біжить.")]
    public float runSpeedMultiplier = 1.5f;
    [Tooltip("Кількість енергії, що витрачається за секунду при бігу.")]
    public float runEnergyCostPerSecond = 5f;

    // --- Параметри атаки ---
    [Header("Атака")]
    [Tooltip("Префаб або об'єкт, який буде відображати коло атаки.")]
    public GameObject attackIndicatorPrefab;
    [Tooltip("Радіус кола атаки відносно розміру персонажа (наприклад, 0.33 для 1/3).")]
    [Range(0.1f, 2f)]
    public float attackRadiusMultiplier = 0.33f;
    [Tooltip("Відстань кола атаки від центру персонажа.")]
    public float attackCircleOffset = 0.5f;
    [Tooltip("Час між атаками в секундах.")]
    public float attackCooldown = 1.0f;

    // --- Параметри здоров'я та енергії ---
    [Header("Характеристики")]
    [Tooltip("Початкове і максимальне здоров'я персонажа.")]
    public int maxHealth = 100;
    [Tooltip("Початкова і максимальна енергія персонажа.")]
    public int maxEnergy = 100;
    [Tooltip("Кількість енергії, що відновлюється за секунду, коли персонаж не атакує.")]
    public float energyRegenRate = 10f;
    [Tooltip("Вартість енергії за одну атаку.")]
    public int attackEnergyCost = 10;

    // --- UI елементи (призначте їх в Інспекторі) ---
    [Header("UI Елементи")]
    [Tooltip("Текстовий об'єкт для відображення здоров'я.")]
    public Text healthText;
    [Tooltip("Image або Slider для заповнення смуги здоров'я.")]
    public Image healthFillImage;
    [Tooltip("Текстовий об'єкт для відображення енергії.")]
    public Text energyText;
    [Tooltip("Image або Slider для заповнення смуги енергії.")]
    public Image energyFillImage;

    // --- Параметри карти (будуть встановлені WorldManager) ---
    private float mapMinX, mapMaxX, mapMinY, mapMaxY;
    private bool mapBoundsSet = false;

    // --- Приватні змінні стану ---
    private Rigidbody2D rb;
    private Camera mainCamera;
    private float currentHealth;
    private float currentEnergy;
    private float nextAttackTime = 0f;
    private GameObject currentAttackIndicator;
    private SpriteRenderer attackIndicatorRenderer;

    // Додано посилання на InventoryManager
    private InventoryManager inventoryManager;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("PlayerController: На об'єкті немає компонента Rigidbody2D. Будь ласка, додайте його.", this);
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("PlayerController: Камера не знайдена. Переконайтеся, що у сцені є камера з тегом 'MainCamera'.", this);
        }

        currentHealth = maxHealth;
        currentEnergy = maxEnergy;

        // Знайдіть InventoryManager в сцені
        inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogWarning("PlayerController: InventoryManager не знайдено в сцені. Деякі функції інвентарю можуть не працювати.");
        }


        // Створення візуального індикатора атаки, якщо префаб призначено
        if (attackIndicatorPrefab != null)
        {
            currentAttackIndicator = Instantiate(attackIndicatorPrefab, transform);
            attackIndicatorRenderer = currentAttackIndicator.GetComponent<SpriteRenderer>();
            if (attackIndicatorRenderer == null)
            {
                Debug.LogError("PlayerController: Префаб індикатора атаки не має компонента SpriteRenderer.", this);
            }
            attackIndicatorRenderer.color = Color.yellow;
            currentAttackIndicator.transform.localRotation = Quaternion.Euler(0, 0, 0);
            attackIndicatorRenderer.enabled = false;
        }
        else
        {
            Debug.LogWarning("PlayerController: Attack Indicator Prefab не призначено. Атака не буде візуалізована.");
        }

        UpdateUI();
    }

    void Update()
    {
        HandleMovementInput();
        HandleRotationTowardsMouse();
        HandleAttackInput();
        RegenerateEnergy();
        UpdateUI();

        // Тимчасовий код для тестування додавання предмета (можна видалити пізніше)
        if (Input.GetKeyDown(KeyCode.I) && inventoryManager != null)
        {
            // Для тестування, просто додаємо випадковий предмет. Вам потрібно буде створити реальні предмети.
            // Приклад: inventoryManager.AddItem(new Item { Name = "Stone", Icon = null, MaxStack = 99 }, 1);
            Debug.Log("Натиснуто I - Додавання предмета (функція поки не реалізована, потрібен реальний предмет)");
        }
    }

    void FixedUpdate()
    {
        PerformMovement();
    }

    // Встановлює межі карти для гравця
    public void SetMapBounds(float minX, float maxX, float minY, float maxY)
    {
        mapMinX = minX;
        mapMaxX = maxX;
        mapMinY = minY;
        mapMaxY = maxY;
        mapBoundsSet = true;
    }

    // Обробка вводу для руху та витрати енергії при бігу
    void HandleMovementInput()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector2 moveDirection = new Vector2(horizontalInput, verticalInput).normalized;

        float currentSpeed = moveSpeed;
        bool isRunning = false;
        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.Return)) && currentEnergy > 0)
        {
            currentSpeed *= runSpeedMultiplier;
            isRunning = true;
        }

        // Витрата енергії при бігу
        if (isRunning && moveDirection.magnitude > 0)
        {
            currentEnergy -= runEnergyCostPerSecond * Time.deltaTime;
            currentEnergy = Mathf.Max(0, currentEnergy);
        }

        rb.linearVelocity = moveDirection * currentSpeed;
    }

    void PerformMovement()
    {
        // Обмеження позиції гравця в межах карти
        if (mapBoundsSet)
        {
            float clampedX = Mathf.Clamp(rb.position.x, mapMinX + playerSize, mapMaxX - playerSize);
            float clampedY = Mathf.Clamp(rb.position.y, mapMinY + playerSize, mapMaxY - playerSize);
            rb.position = new Vector2(clampedX, clampedY);
        }
    }

    // Обертання персонажа до курсору миші (нижня частина до курсора)
    void HandleRotationTowardsMouse()
    {
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 lookDirection = mouseWorldPosition - transform.position;

        // Додаємо 90 градусів, щоб "низ" персонажа (спрайт, який дивиться вгору по осі Y)
        // був повернутий до курсору.
        float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg + 90f;

        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }

    // Обробка вводу для атаки
    void HandleAttackInput()
    {
        if (Input.GetMouseButton(0))
        {
            if (Time.time >= nextAttackTime)
            {
                if (currentEnergy >= attackEnergyCost)
                {
                    PerformAttack();
                    currentEnergy -= attackEnergyCost;
                    nextAttackTime = Time.time + attackCooldown;
                }
                else
                {
                    if (attackIndicatorRenderer != null)
                    {
                        attackIndicatorRenderer.enabled = false;
                    }
                }
            }
            else
            {
                if (attackIndicatorRenderer != null)
                {
                    attackIndicatorRenderer.enabled = false;
                }
            }
        }
        else
        {
            if (attackIndicatorRenderer != null)
            {
                attackIndicatorRenderer.enabled = false;
            }
        }
    }

    // Виконання логіки атаки
    void PerformAttack()
    {
        if (currentAttackIndicator != null)
        {
            attackIndicatorRenderer.enabled = true;

            // Обчислення позиції кола атаки
            float actualAttackOffset = playerSize * attackCircleOffset;

            // Встановлюємо позицію індикатора атаки відносно центру гравця.
            // Vector3.down, оскільки "низ" персонажа дивиться на мишку, а атака має бути "перед" ним.
            currentAttackIndicator.transform.localPosition = Vector3.down * actualAttackOffset;

            // Встановлення розміру кола атаки
            float circleSize = playerSize * attackRadiusMultiplier * 2;
            currentAttackIndicator.transform.localScale = new Vector3(circleSize, circleSize, 1);

            Invoke("HideAttackIndicator", 0.1f);
        }

        Debug.Log("Персонаж атакує!");
    }

    // Приховати візуальний індикатор атаки
    void HideAttackIndicator()
    {
        if (attackIndicatorRenderer != null)
        {
            attackIndicatorRenderer.enabled = false;
        }
    }

    // Відновлення енергії
    void RegenerateEnergy()
    {
        bool isRunning = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.Return));

        // Відновлюємо енергію, тільки якщо кнопка атаки не натиснута, НЕ БІЖИТЬ і енергія не повна
        if (!Input.GetMouseButton(0) && !isRunning && currentEnergy < maxEnergy)
        {
            currentEnergy += energyRegenRate * Time.deltaTime;
            currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
        }
    }

    // Оновлення елементів UI (текст та смуги)
    void UpdateUI()
    {
        if (healthText != null)
        {
            healthText.text = $"Здоров'я: {Mathf.RoundToInt(currentHealth)}/{maxHealth}";
        }
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = currentHealth / maxHealth;
        }

        if (energyText != null)
        {
            energyText.text = $"Енергія: {Mathf.RoundToInt(currentEnergy)}/{maxEnergy}";
        }
        if (energyFillImage != null)
        {
            energyFillImage.fillAmount = currentEnergy / maxEnergy;
        }
    }

    // --- Додаткові методи (для використання іншими скриптами) ---
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        UpdateUI();
    }

    void Die()
    {
        Debug.Log("Персонаж помер!");
    }

    // Допоміжна властивість для розміру гравця (якщо не використовується CircleCollider2D)
    private float playerSize
    {
        get
        {
            CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider != null)
            {
                return circleCollider.radius;
            }
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                return Mathf.Max(spriteRenderer.bounds.extents.x, spriteRenderer.bounds.extents.y);
            }
            return 0.5f;
        }
    }
}
