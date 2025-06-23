using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq; // Для використання .Any()

// Перелік типів біомів для зручності
public enum BiomeType { None, Snow, Grass, Mountain }

// Клас для визначення окремого біому та його властивостей для налаштування в Інспекторі
[System.Serializable]
public class BiomeDefinition
{
    [Tooltip("Тип біому (для внутрішнього використання та музики).")]
    public BiomeType type;
    [Tooltip("Аудіо кліп для цього біому.")]
    public AudioClip biomeMusic;
    [Tooltip("Шанс спавну первинного тайла (наприклад, сніг для зимового біому, трава для трав'яного).")]
    [Range(0.0f, 1.0f)]
    public float primaryTileChance = 0.8f;
    [Tooltip("Первинний тайл для цього біому.")]
    public TileBase primaryTile;
    [Tooltip("Вторинний тайл для цього біому (наприклад, земля/бруд).")]
    public TileBase secondaryTile;
}

public class WorldManager : MonoBehaviour
{
    // --- Налаштування Tilemap ---
    [Header("Tilemap")]
    [Tooltip("Посилання на Tilemap для землі. Створіть GameObject з TilemapRenderer та Tilemap Collider 2D.")]
    public Tilemap groundTilemap;

    // --- Налаштування генерації світу ---
    [Header("Налаштування Світу")]
    [Tooltip("Ширина генерованого світу (в плитках).")]
    public int width = 300;
    [Tooltip("Висота генерованого світу (в плитках).")]
    public int height = 300;
    [Tooltip("Висота зимового біому (зверху карти, в плитках).")]
    public int topBiomeHeight = 100;
    [Tooltip("Висота гірського біому (знизу карти, в плитках).")]
    public int bottomBiomeHeight = 100;

    // --- Налаштування Біомів ---
    [Header("Налаштування Біомів (заповніть в Інспекторі!)")]
    public BiomeDefinition snowBiome;
    public BiomeDefinition grassBiome;
    public BiomeDefinition mountainBiome;
    [Tooltip("Швидкість згасання/появи музики при зміні біому.")]
    public float musicFadeSpeed = 1.0f; // Швидкість (в секундах)

    // Приватні змінні
    private AudioSource audioSource;
    private BiomeType currentBiomeType = BiomeType.None; // Поточний активний біом
    private Transform playerTransform; // Посилання на об'єкт гравця
    private PlayerController playerController; // Посилання на контролер гравця для встановлення меж
    private Dictionary<string, TileBase> assignedTilesMap; // Словник для зберігання тайлів за назвою

    void Awake()
    {
        // Перевірка та ініціалізація Tilemap
        if (groundTilemap == null)
        {
            groundTilemap = FindObjectOfType<Tilemap>();
            if (groundTilemap == null) { Debug.LogError("WorldManager: GroundTilemap не знайдено в сцені."); return; }
        }

        // Ініціалізація AudioSource для музики
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.loop = true; // Зациклення музики
        audioSource.playOnAwake = false; // Не грати при запуску

        // Заповнюємо словник призначених тайлів з BiomeDefinitions
        assignedTilesMap = new Dictionary<string, TileBase>();
        AddBiomeTilesToMap(snowBiome);
        AddBiomeTilesToMap(grassBiome);
        AddBiomeTilesToMap(mountainBiome);

        // Якщо деякі тайли все ще відсутні, створюємо запасні
        if (assignedTilesMap.Count < 5) // Припускаємо мінімум 5 унікальних тайлів
        {
            Debug.LogWarning("WorldManager: Деякі базові тайли не призначені в Biome Definitions. Створюю їх програмно як тимчасове рішення. Рекомендовано призначити Assets Tile для кращого контролю.");
            CreateFallbackTilesProgrammatically();
        }
    }

    void Start()
    {
        // Знайти гравця за тегом "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }
        else
        {
            Debug.LogError("WorldManager: Об'єкт гравця з тегом 'Player' не знайдено. Музика біомів та обмеження руху не працюватимуть.");
        }

        GenerateWorld(); // Початкова генерація світу
    }

    void Update()
    {
        // Перемикання музики в залежності від позиції гравця
        if (playerTransform != null)
        {
            DetectAndSwitchBiomeMusic();
        }
    }

    // Додає тайли з BiomeDefinition до словника
    private void AddBiomeTilesToMap(BiomeDefinition biome)
    {
        if (biome == null) return;
        if (biome.primaryTile != null && !assignedTilesMap.ContainsKey(biome.primaryTile.name))
        {
            assignedTilesMap.Add(biome.primaryTile.name, biome.primaryTile);
        }
        if (biome.secondaryTile != null && !assignedTilesMap.ContainsKey(biome.secondaryTile.name))
        {
            assignedTilesMap.Add(biome.secondaryTile.name, biome.secondaryTile);
        }
    }

    // Створює тимчасові тайли, якщо їх не призначено (як fallback)
    private void CreateFallbackTilesProgrammatically()
    {
        Sprite baseSquareSprite = Resources.Load<Sprite>("Square");
        if (baseSquareSprite == null)
        {
            Texture2D tempTex = new Texture2D(16, 16);
            tempTex.filterMode = FilterMode.Point;
            tempTex.wrapMode = TextureWrapMode.Clamp;
            Color[] colors = new Color[16 * 16];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.magenta;
            tempTex.SetPixels(colors);
            tempTex.Apply();
            baseSquareSprite = Sprite.Create(tempTex, new Rect(0, 0, tempTex.width, tempTex.height), Vector2.one * 0.5f, 16);
        }

        System.Action<string, Color, System.Action<TileBase>> createAndAssignTile = (name, color, assignAction) => {
            if (!assignedTilesMap.ContainsKey(name))
            {
                Tile newTile = ScriptableObject.CreateInstance<Tile>();
                newTile.sprite = baseSquareSprite;
                newTile.color = color;
                newTile.name = name;
                assignedTilesMap.Add(name, newTile);
                assignAction(newTile);
            }
        };
        // Це дефолтні тайли, якщо BiomeDefinitions не заповнені
        if (!assignedTilesMap.ContainsKey("SnowTile")) createAndAssignTile("SnowTile", Color.white, (t) => snowBiome.primaryTile = t);
        if (!assignedTilesMap.ContainsKey("GrassTile")) createAndAssignTile("GrassTile", Color.green, (t) => grassBiome.primaryTile = t);
        if (!assignedTilesMap.ContainsKey("DirtTile")) createAndAssignTile("DirtTile", new Color(0.5f, 0.3f, 0.1f), (t) => grassBiome.secondaryTile = t);
        if (!assignedTilesMap.ContainsKey("StoneTile")) createAndAssignTile("StoneTile", new Color(0.5f, 0.5f, 0.5f), (t) => mountainBiome.primaryTile = t);
        if (!assignedTilesMap.ContainsKey("MountainBaseTile")) createAndAssignTile("MountainBaseTile", new Color(0.2f, 0.2f, 0.2f), (t) => mountainBiome.secondaryTile = t);
    }

    // Генерація світу
    void GenerateWorld()
    {
        groundTilemap.ClearAllTiles(); // Очищаємо попередні тайли

        // Генеруємо випадкові зсуви для Perlin Noise
        float globalOffsetX = Random.Range(0f, 99999f);
        float globalOffsetY = Random.Range(0f, 99999f);

        // Розрахунок меж світу в світових координатах
        // Центр Tilemap в Unity зазвичай знаходиться в (0,0) світових координатах
        float worldMinX = groundTilemap.origin.x + groundTilemap.cellSize.x / 2f;
        float worldMaxX = groundTilemap.origin.x + groundTilemap.size.x * groundTilemap.cellSize.x - groundTilemap.cellSize.x / 2f;
        float worldMinY = groundTilemap.origin.y + groundTilemap.cellSize.y / 2f;
        float worldMaxY = groundTilemap.origin.y + groundTilemap.size.y * groundTilemap.cellSize.y - groundTilemap.cellSize.y / 2f;

        // Коректне визначення меж, якщо Tilemap має негативні координати початку
        // або якщо вона не зосереджена в 0,0.
        // Замість this.width/2f, використовуємо фактичні розміри Tilemap в світових одиницях.
        float mapBoundsMinX = groundTilemap.CellToWorld(new Vector3Int(0, 0, 0)).x;
        float mapBoundsMaxX = groundTilemap.CellToWorld(new Vector3Int(width, 0, 0)).x;
        float mapBoundsMinY = groundTilemap.CellToWorld(new Vector3Int(0, 0, 0)).y;
        float mapBoundsMaxY = groundTilemap.CellToWorld(new Vector3Int(0, height, 0)).y;

        // Передача меж карти гравцеві
        if (playerController != null)
        {
            playerController.SetMapBounds(mapBoundsMinX, mapBoundsMaxX, mapBoundsMinY, mapBoundsMaxY);
            Debug.Log($"Межі карти встановлено: X ({mapBoundsMinX:F2}, {mapBoundsMaxX:F2}), Y ({mapBoundsMinY:F2}, {mapBoundsMaxY:F2})");
        }

        // Спавн гравця в трав'яному біомі
        if (playerTransform != null)
        {
            // Визначення Y-діапазону для трав'яного біому в світових координатах
            // Припускаємо, що трав'яний біом знаходиться між нижнім та верхнім біомами
            float grassBiomeWorldYStart = groundTilemap.CellToWorld(new Vector3Int(0, bottomBiomeHeight, 0)).y;
            float grassBiomeWorldYEnd = groundTilemap.CellToWorld(new Vector3Int(0, height - topBiomeHeight, 0)).y;

            // Перевіряємо, чи є взагалі трав'яний біом
            if (grassBiomeWorldYStart < grassBiomeWorldYEnd)
            {
                // Випадкова позиція в межах трав'яного біому
                float spawnX = Random.Range(mapBoundsMinX, mapBoundsMaxX);
                float spawnY = Random.Range(grassBiomeWorldYStart, grassBiomeWorldYEnd);
                playerTransform.position = new Vector3(spawnX, spawnY, 0); // Позиція Z має бути 0 для 2D
                Debug.Log($"Гравець заспавнився в трав'яному біомі на ({spawnX:F2}, {spawnY:F2})");
            }
            else
            {
                Debug.LogWarning("WorldManager: Трав'яний біом відсутній або занадто малий. Гравець заспавниться в центрі світу.");
                playerTransform.position = Vector3.zero;
            }
        }


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileBase tileToPlace = null;
                // Шум для різноманітності в межах біому
                float terrainNoiseValue = Mathf.PerlinNoise((float)x / width * 20f + globalOffsetX, (float)y / height * 20f + globalOffsetY);

                // Визначення біому за Y-координатою плитки
                // Плитка (x, y) у локальних координатах Tilemap
                // У нас є fixed-zones, тому просто використовуємо y для визначення зони.
                if (y >= height - topBiomeHeight) // Верхня зона (Зима)
                {
                    if (snowBiome != null && snowBiome.primaryTile != null && snowBiome.secondaryTile != null)
                    {
                        tileToPlace = terrainNoiseValue > (1f - snowBiome.primaryTileChance) ? snowBiome.primaryTile : snowBiome.secondaryTile;
                    }
                }
                else if (y < bottomBiomeHeight) // Нижня зона (Гори/Камінь)
                {
                    if (mountainBiome != null && mountainBiome.primaryTile != null && mountainBiome.secondaryTile != null)
                    {
                        tileToPlace = terrainNoiseValue > (1f - mountainBiome.primaryTileChance) ? mountainBiome.primaryTile : mountainBiome.secondaryTile;
                    }
                }
                else // Середня зона (Трава)
                {
                    if (grassBiome != null && grassBiome.primaryTile != null && grassBiome.secondaryTile != null)
                    {
                        tileToPlace = terrainNoiseValue > (1f - grassBiome.primaryTileChance) ? grassBiome.primaryTile : grassBiome.secondaryTile;
                    }
                }

                // Встановлюємо тайл на Tilemap
                if (tileToPlace != null)
                {
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), tileToPlace);
                }
                else
                {
                    // Якщо тайл не був призначений, можна поставити дефолтний, наприклад, траву
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), grassBiome?.primaryTile ?? null);
                }
            }
        }
        Debug.Log($"Світ розміром {width}x{height} згенеровано.");

        // Після генерації світу та спавну гравця, ініціалізуємо музику біому
        if (playerTransform != null)
        {
            DetectAndSwitchBiomeMusic();
        }
    }

    // Визначає поточний біом гравця та перемикає музику
    private void DetectAndSwitchBiomeMusic()
    {
        BiomeType newBiome = BiomeType.None;
        float playerWorldY = playerTransform.position.y;

        // Перетворення світової Y-координати гравця в Y-координату плитки
        // Це дозволить визначити, в якому біомі (зоні) знаходиться гравець
        // groundTilemap.WorldToCell(playerTransform.position).y повертає Y-індекс комірки
        int playerTileY = groundTilemap.WorldToCell(playerTransform.position).y;

        // Перевіряємо, в якій зоні знаходиться гравець за його Y-координатою плитки
        if (playerTileY >= height - topBiomeHeight) // Верхній біом (Сніг)
        {
            newBiome = BiomeType.Snow;
        }
        else if (playerTileY < bottomBiomeHeight) // Нижня зона (Гори)
        {
            newBiome = BiomeType.Mountain;
        }
        else // Середня зона (Трава)
        {
            newBiome = BiomeType.Grass;
        }

        // Якщо біом змінився, перемикаємо музику
        if (newBiome != currentBiomeType)
        {
            currentBiomeType = newBiome;
            SwitchBiomeMusic(currentBiomeType);
        }
    }

    // Перемикає музичний кліп для поточного біому
    private void SwitchBiomeMusic(BiomeType biome)
    {
        if (audioSource == null) return;

        AudioClip targetClip = null;
        switch (biome)
        {
            case BiomeType.Snow:
                if (snowBiome != null) targetClip = snowBiome.biomeMusic;
                break;
            case BiomeType.Grass:
                if (grassBiome != null) targetClip = grassBiome.biomeMusic;
                break;
            case BiomeType.Mountain: // Виправлено: Type замість None.
                if (mountainBiome != null) targetClip = mountainBiome.biomeMusic;
                break;
        }

        if (targetClip != null && audioSource.clip != targetClip)
        {
            StartCoroutine(FadeMusic(audioSource, targetClip, musicFadeSpeed));
            Debug.Log($"Змінено музику на біом: {biome}");
        }
        else if (targetClip == null && audioSource.isPlaying)
        {
            StartCoroutine(FadeMusic(audioSource, null, musicFadeSpeed));
            Debug.LogWarning($"Музика для біому {biome} не призначена. Музика зупинена.");
        }
    }

    // Корутина для плавного згасання та появи музики
    private System.Collections.IEnumerator FadeMusic(AudioSource audioSource, AudioClip newClip, float fadeDuration)
    {
        // Зберігаємо початкову гучність, щоб повернутися до неї
        float initialVolume = 1f; // Припускаємо, що повна гучність - 1
        if (audioSource.clip != null && audioSource.isPlaying)
        {
            initialVolume = audioSource.volume; // Якщо музика вже грає, беремо її поточну гучність
            // Згасання поточної музики
            float timer = 0f;
            while (timer < fadeDuration && audioSource.volume > 0)
            {
                audioSource.volume = Mathf.Lerp(initialVolume, 0, timer / fadeDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            audioSource.Stop();
        }

        audioSource.volume = 0; // Встановлюємо гучність на 0 перед запуском нової пісні
        audioSource.clip = newClip;

        if (newClip != null)
        {
            audioSource.Play();
            // Поява нової музики
            float timer = 0f;
            while (timer < fadeDuration && audioSource.volume < 1) // Припускаємо, що кінцева гучність має бути 1
            {
                audioSource.volume = Mathf.Lerp(0, 1, timer / fadeDuration); // Плавно збільшуємо до 1
                timer += Time.deltaTime;
                yield return null;
            }
            audioSource.volume = 1f; // Встановлюємо на повну гучність
        }
    }
}
