using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

// --- ДОДАНО: Новий клас для зберігання даних про кожен блок ---
// Він зберігає не тільки здоров'я, але й посилання на предмет,
// з якого був створений, щоб знати, що має випасти при руйнуванні.
public class BlockData
{
    public int currentHealth;
    public Item sourceItem;
}

public class WorldManager : MonoBehaviour
{
    public static WorldManager instance;

    [Header("Налаштування світу")]
    public Tilemap groundTilemap;
    public Tilemap buildingTilemap; // Це поле ми додали минулого разу
    public int width = 300, height = 300;
    public int topBiomeHeight = 100, bottomBiomeHeight = 100;

    [Header("Налаштування біомів")]
    public BiomeDefinition snowBiome;
    public BiomeDefinition grassBiome;
    public BiomeDefinition mountainBiome;
    public float musicFadeSpeed = 1.0f;

    [Header("Конфігурації Спавнерів")]
    public SpawnerConfig[] spawnerConfigs;
    public float spawnerTickRate = 5f;

    // --- ДОДАНО: Словник для зберігання здоров'я всіх поставлених блоків ---
    // Ключ - це координата блоку, значення - об'єкт з його даними (здоров'я, предмет).
    public Dictionary<Vector3Int, BlockData> placedBlocksData = new Dictionary<Vector3Int, BlockData>();

    // --- ВАШ КОД ЗАЛИШИВСЯ БЕЗ ЗМІН ---
    private AudioSource audioSource;
    private BiomeType currentBiomeType = BiomeType.None;
    private Transform playerTransform;
    private PlayerController playerController;
    private Dictionary<SpawnerConfig, List<GameObject>> spawnedObjects = new Dictionary<SpawnerConfig, List<GameObject>>();
    private Dictionary<SpawnerConfig, float> respawnTimers = new Dictionary<SpawnerConfig, float>();

    void Awake()
    {
        if (instance == null) { instance = this; }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        InitializeAudio();
        FindPlayer();
        GenerateWorld();
        TimeManager.OnPhaseChanged += HandlePhaseChange;
        InitializeSpawners();
        StartCoroutine(SpawnerUpdateLoop());
    }

    void OnDestroy()
    {
        TimeManager.OnPhaseChanged -= HandlePhaseChange;
    }

    void Update()
    {
        if (playerTransform != null)
        {
            DetectAndSwitchBiomeMusic();
        }
    }

    // --- ДОДАНО: Два нових публічних методи для керування блоками ---

    /// <summary>
    /// Реєструє новий поставлений блок і додає його в базу даних здоров'я.
    /// Цей метод буде викликатися з PlayerController.
    /// </summary>
    public void RegisterPlacedTile(Vector3Int position, Item item)
    {
        if (item.itemType != Item.ItemType.Block || placedBlocksData.ContainsKey(position)) return;

        BlockData newBlockData = new BlockData
        {
            currentHealth = item.blockHealth,
            sourceItem = item
        };

        placedBlocksData.Add(position, newBlockData);
        Debug.Log($"Зареєстровано блок в позиції {position} з {item.blockHealth} HP.");
    }

    /// <summary>
    /// Наносить шкоду блоку в заданій позиції.
    /// Цей метод також буде викликатися з PlayerController.
    /// </summary>
    public void DamageTile(Vector3Int position, float damage)
    {
        if (!placedBlocksData.ContainsKey(position)) return;

        BlockData blockData = placedBlocksData[position];
        blockData.currentHealth -= (int)damage; // Віднімаємо здоров'я

        Debug.Log($"Нанесено {damage} шкоди блоку в {position}. Залишилось здоров'я: {blockData.currentHealth}");

        if (blockData.currentHealth <= 0)
        {
            // Якщо здоров'я скінчилось, руйнуємо блок
            Debug.Log($"Блок в {position} зруйновано!");
            buildingTilemap.SetTile(position, null); // Видаляємо тайл з карти

            // Логіка випадіння ресурсу
            if (blockData.sourceItem.itemToDrop != null && Random.value < blockData.sourceItem.dropChance)
            {
                Item itemToDrop = blockData.sourceItem.itemToDrop;
                if (itemToDrop.itemPrefab != null)
                {
                    Vector3 worldPosition = buildingTilemap.GetCellCenterWorld(position);
                    Instantiate(itemToDrop.itemPrefab, worldPosition, Quaternion.identity);
                    Debug.Log($"З блоку випав предмет: {itemToDrop.itemName}");
                }
            }

            placedBlocksData.Remove(position); // Видаляємо блок з нашої бази даних
        }
    }

    // --- ВСЯ ВАША СТАРА ЛОГІКА ЗАЛИШИЛАСЯ НИЖЧЕ БЕЗ ЗМІН ---
    #region Ініціалізація
    private void InitializeAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    private void FindPlayer()
    {
        PlayerController playerObj = FindObjectOfType<PlayerController>();
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerController = playerObj;
        }
        else
        {
            Debug.LogError("WorldManager: Гравець не знайдений! Спавн та межі карти не працюватимуть.");
        }
    }
    #endregion

    #region Генерація Світу, Спавн Гравця та Межі Карти
    void GenerateWorld()
    {
        groundTilemap.ClearAllTiles();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                BiomeType biome = GetBiomeForHeight(y);
                BiomeDefinition def = GetBiomeDefinition(biome);
                if (def != null)
                {
                    float noise = Mathf.PerlinNoise((float)x / width * 20f, (float)y / height * 20f);
                    TileBase tile = noise > (1f - def.primaryTileChance) ? def.primaryTile : def.secondaryTile;
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }
        Debug.Log($"Світ розміром {width}x{height} згенеровано.");

        if (playerController != null)
        {
            float mapBoundsMinX = groundTilemap.CellToWorld(new Vector3Int(0, 0, 0)).x;
            float mapBoundsMaxX = groundTilemap.CellToWorld(new Vector3Int(width, 0, 0)).x;
            float mapBoundsMinY = groundTilemap.CellToWorld(new Vector3Int(0, 0, 0)).y;
            float mapBoundsMaxY = groundTilemap.CellToWorld(new Vector3Int(0, height, 0)).y;
            playerController.SetMapBounds(mapBoundsMinX, mapBoundsMaxX, mapBoundsMinY, mapBoundsMaxY);
        }

        if (playerTransform != null)
        {
            float grassBiomeWorldYStart = groundTilemap.CellToWorld(new Vector3Int(0, bottomBiomeHeight, 0)).y;
            float grassBiomeWorldYEnd = groundTilemap.CellToWorld(new Vector3Int(0, height - topBiomeHeight, 0)).y;

            if (grassBiomeWorldYStart < grassBiomeWorldYEnd)
            {
                float spawnX = groundTilemap.CellToWorld(new Vector3Int(width / 2, 0, 0)).x;
                float spawnY = Random.Range(grassBiomeWorldYStart, grassBiomeWorldYEnd);
                playerTransform.position = new Vector3(spawnX, spawnY, 0);
                Debug.Log($"Гравець заспавнився в трав'яному біомі на ({spawnX:F2}, {spawnY:F2})");
            }
        }
    }
    #endregion

    #region Нова система динамічного спавну
    private void HandlePhaseChange(GamePhase newPhase)
    {
        foreach (var config in spawnerConfigs)
        {
            bool shouldBeActive = config.activePhases.Contains(newPhase);
            if (!shouldBeActive && spawnedObjects.ContainsKey(config))
            {
                List<GameObject> objectsToDestroy = new List<GameObject>(spawnedObjects[config]);
                foreach (var obj in objectsToDestroy)
                {
                    if (obj != null) Destroy(obj);
                }
                spawnedObjects[config].Clear();
            }
        }
    }

    private IEnumerator SpawnerUpdateLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnerTickRate);
            if (TimeManager.instance == null) continue;
            GamePhase currentPhase = TimeManager.instance.GetCurrentPhase();

            foreach (var config in spawnerConfigs)
            {
                if (!config.activePhases.Contains(currentPhase)) continue;
                if (!spawnedObjects.ContainsKey(config)) continue;

                spawnedObjects[config].RemoveAll(obj => obj == null);
                int currentCount = spawnedObjects[config].Count;

                if (currentCount < config.minCount)
                {
                    for (int i = 0; i < config.minCount - currentCount; i++) SpawnObject(config);
                }
                else if (currentCount < config.maxCount)
                {
                    respawnTimers[config] += spawnerTickRate;
                    if (respawnTimers[config] >= config.passiveRespawnTime)
                    {
                        SpawnObject(config);
                        respawnTimers[config] = 0f;
                    }
                }
            }
        }
    }

    private void InitializeSpawners()
    {
        if (TimeManager.instance == null) { Debug.LogError("TimeManager не знайдено!"); return; }
        GamePhase initialPhase = TimeManager.instance.GetCurrentPhase();
        foreach (var config in spawnerConfigs)
        {
            spawnedObjects[config] = new List<GameObject>();
            respawnTimers[config] = 0f;
            if (config.activePhases.Contains(initialPhase))
            {
                for (int i = 0; i < config.maxCount; i++) SpawnObject(config);
            }
        }
    }

    private void SpawnObject(SpawnerConfig config)
    {
        Vector3? spawnPosition = GetRandomPositionInBiome(config.targetBiome);
        if (spawnPosition.HasValue)
        {
            GameObject newObj = Instantiate(config.prefabToSpawn, spawnPosition.Value, Quaternion.identity, transform);
            spawnedObjects[config].Add(newObj);
        }
    }

    private Vector3? GetRandomPositionInBiome(BiomeType biome)
    {
        int minY = 0, maxY = 0;
        switch (biome)
        {
            case BiomeType.Grass: minY = bottomBiomeHeight; maxY = height - topBiomeHeight; break;
            case BiomeType.Snow: minY = height - topBiomeHeight; maxY = height; break;
            case BiomeType.Mountain: minY = 0; maxY = bottomBiomeHeight; break;
        }
        for (int i = 0; i < 20; i++)
        {
            int randomX = Random.Range(0, width);
            int randomY = Random.Range(minY, maxY);
            Vector3 position = groundTilemap.CellToWorld(new Vector3Int(randomX, randomY, 0)) + new Vector3(0.5f, 0.5f, 0);
            if (Physics2D.OverlapCircle(position, 1.5f) == null) return position;
        }
        return null;
    }
    #endregion

    #region Музика та визначення біомів
    private void DetectAndSwitchBiomeMusic()
    {
        if (playerTransform == null || audioSource == null) return;

        int playerTileY = groundTilemap.WorldToCell(playerTransform.position).y;
        BiomeType newBiome = GetBiomeForHeight(playerTileY);

        if (newBiome != currentBiomeType)
        {
            currentBiomeType = newBiome;
            BiomeDefinition def = GetBiomeDefinition(newBiome);
            if (def != null && def.biomeMusic != null)
            {
                if (audioSource.clip != def.biomeMusic)
                {
                    StartCoroutine(FadeMusic(def.biomeMusic));
                }
            }
        }
    }

    private IEnumerator FadeMusic(AudioClip newClip)
    {
        float startVolume = audioSource.volume;
        float timer = 0;

        while (timer < musicFadeSpeed)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0, timer / musicFadeSpeed);
            timer += Time.deltaTime;
            yield return null;
        }

        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();

        timer = 0;
        while (timer < musicFadeSpeed)
        {
            audioSource.volume = Mathf.Lerp(0, 1, timer / musicFadeSpeed);
            timer += Time.deltaTime;
            yield return null;
        }
        audioSource.volume = 1;
    }

    private BiomeType GetBiomeForHeight(int y)
    {
        if (y >= height - topBiomeHeight) return BiomeType.Snow;
        if (y < bottomBiomeHeight) return BiomeType.Mountain;
        return BiomeType.Grass;
    }

    private BiomeDefinition GetBiomeDefinition(BiomeType type)
    {
        switch (type)
        {
            case BiomeType.Snow: return snowBiome;
            case BiomeType.Grass: return grassBiome;
            case BiomeType.Mountain: return mountainBiome;
            default: return null;
        }
    }
    #endregion
}