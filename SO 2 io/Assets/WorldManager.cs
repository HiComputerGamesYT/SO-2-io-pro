// SO 2 io/Assets/WorldManager.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BlockData
{
    public int currentHealth;
    public Item sourceItem;
}

public class GeneratedMiniLocationInfo
{
    public MiniLocationDefinition Definition;
    public BoundsInt Bounds; // Границы мини-локации в клеточных координатах
    public Vector3Int Center; // Центр круговой мини-локации (для расчета музыки по радиусу)
    public int Radius; // Радиус круговой мини-локации (для расчета музыки по радиусу)
}

public class WorldManager : MonoBehaviour
{
    public static WorldManager instance;

    [Header("Налаштування світу")]
    public Tilemap groundTilemap;
    public Tilemap buildingTilemap;
    public int width = 300, height = 300;

    [Header("Налаштування шуму генерації")]
    public float baseNoiseScale = 0.02f;
    public float detailNoiseScale = 0.05f;
    public float detailNoiseStrength = 0.2f;
    public int noiseSeedOffset = 12345;

    [Header("Налаштування біомів")]
    public List<BiomeDefinition> allBiomes;

    [Range(0.0f, 1.0f)] public float frozenOceanStartRatio = 0.95f;
    [Range(0.0f, 1.0f)] public float tundraStartRatio = 0.85f;
    [Range(0.0f, 1.0f)] public float forestTundraStartRatio = 0.75f;
    [Range(0.0f, 1.0f)] public float taigaStartRatio = 0.65f;
    [Range(0.0f, 1.0f)] public float mixedForestStartRatio = 0.55f;
    [Range(0.0f, 1.0f)] public float broadleafForestStartRatio = 0.45f;
    [Range(0.0f, 1.0f)] public float steppeStartRatio = 0.35f;
    [Range(0.0f, 1.0f)] public float savannaStartRatio = 0.25f;
    [Range(0.0f, 1.0f)] public float jungleStartRatio = 0.15f;
    [Range(0.0f, 1.0f)] public float desertStartRatio = 0.05f;

    [Header("Налаштування міні-локацій")]
    public List<MiniLocationDefinition> allMiniLocations;

    public float musicFadeSpeed = 1.0f;

    [Header("Конфігурації Спавнерів")]
    public SpawnerConfig[] spawnerConfigs;
    public float spawnerTickRate = 5f;

    public Dictionary<Vector3Int, BlockData> placedBlocksData = new Dictionary<Vector3Int, BlockData>();

    private AudioSource audioSource;
    private BiomeType currentBiomeType = BiomeType.None;
    private AudioClip currentPlayingMusicClip = null;
    private Transform playerTransform;
    private PlayerController playerController;
    private Dictionary<SpawnerConfig, List<GameObject>> spawnedObjects = new Dictionary<SpawnerConfig, List<GameObject>>();
    private Dictionary<SpawnerConfig, float> respawnTimers = new Dictionary<SpawnerConfig, float>();

    private List<GeneratedMiniLocationInfo> activeMiniLocations = new List<GeneratedMiniLocationInfo>();


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

    public void RegisterPlacedTile(Vector3Int position, Item item)
    {
        if (item.itemType != Item.ItemType.Block || placedBlocksData.ContainsKey(position)) return;

        BlockData newBlockData = new BlockData
        {
            currentHealth = item.blockHealth,
            sourceItem = item
        };

        placedBlocksData.Add(position, newBlockData);
    }

    public void DamageTile(Vector3Int position, float damage)
    {
        if (!placedBlocksData.ContainsKey(position)) return;

        BlockData blockData = placedBlocksData[position];
        blockData.currentHealth -= (int)damage;

        if (blockData.currentHealth <= 0)
        {
            buildingTilemap.SetTile(position, null);

            if (blockData.sourceItem.itemToDrop != null && Random.value < blockData.sourceItem.dropChance)
            {
                Item itemToDrop = blockData.sourceItem.itemToDrop;
                if (itemToDrop.itemPrefab != null)
                {
                    Vector3 worldPosition = buildingTilemap.GetCellCenterWorld(position);
                    Instantiate(itemToDrop.itemPrefab, worldPosition, Quaternion.identity);
                }
            }

            placedBlocksData.Remove(position);
        }
    }

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
        activeMiniLocations.Clear(); // Очищаем список мини-локаций при новой генерации мира

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                BiomeDefinition def = GetBiomeDefinitionForHeight(y);
                if (def != null)
                {
                    float baseNoise = Mathf.PerlinNoise(
                        (float)(x + noiseSeedOffset) * baseNoiseScale,
                        (float)(y + noiseSeedOffset) * baseNoiseScale
                    );

                    float detailNoise = Mathf.PerlinNoise(
                        (float)(x + noiseSeedOffset * 2) * detailNoiseScale,
                        (float)(y + noiseSeedOffset * 2) * detailNoiseScale
                    );

                    float combinedNoise = baseNoise + detailNoise * detailNoiseStrength;

                    TileBase tile = combinedNoise > (1f - def.primaryTileChance) ? def.primaryTile : def.secondaryTile;
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
                else
                {
                    Debug.LogWarning($"No BiomeDefinition found for height {y}. Check your ratios and allBiomes list.");
                }
            }
        }
        Debug.Log($"Світ розміром {width}x{height} згенеровано.");

        GenerateMiniLocations(); // Генерируем мини-локации после основной генерации мира

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
            float spawnX = groundTilemap.CellToWorld(new Vector3Int(width / 2, 0, 0)).x;
            int middleY = height / 2;
            BiomeDefinition middleBiome = GetBiomeDefinitionForHeight(middleY);
            if (middleBiome != null)
            {
                float spawnY = groundTilemap.CellToWorld(new Vector3Int(0, middleY, 0)).y;
                playerTransform.position = new Vector3(spawnX, spawnY, 0);
                Debug.Log($"Гравець заспавнився в біомі {middleBiome.biomeType} на ({spawnX:F2}, {spawnY:F2})");
            }
            else
            {
                Debug.LogWarning("Не удалось найти средний биом для спавна игрока, спавним в центре карты.");
                float spawnY = groundTilemap.CellToWorld(new Vector3Int(0, height / 2, 0)).y;
                playerTransform.position = new Vector3(spawnX, spawnY, 0);
            }
        }
    }

    // --- УЛУЧШЕННАЯ ФУНКЦИЯ: Генерация мини-локаций ---
    private void GenerateMiniLocations()
    {
        Debug.Log("Starting mini-location generation...");
        Dictionary<MiniLocationDefinition, int> currentSpawnCounts = new Dictionary<MiniLocationDefinition, int>();
        foreach (var miniLocDef in allMiniLocations)
        {
            currentSpawnCounts[miniLocDef] = 0;
        }

        for (int x = 0; x < width; x += 10) // Проходим по миру с шагом 10 для оптимизации
        {
            for (int y = 0; y < height; y += 10)
            {
                Vector3Int currentCell = new Vector3Int(x, y, 0);
                BiomeDefinition currentMainBiomeDef = GetBiomeDefinitionForHeight(y);
                BiomeType currentBiomeType = currentMainBiomeDef?.biomeType ?? BiomeType.None;

                foreach (var miniLocDef in allMiniLocations)
                {
                    if (currentSpawnCounts[miniLocDef] >= miniLocDef.maxRandomSpawns) continue;

                    if (miniLocDef.allowedBiomes != null && miniLocDef.allowedBiomes.Count > 0 && !miniLocDef.allowedBiomes.Contains(currentBiomeType))
                    {
                        continue;
                    }

                    if (Random.value < miniLocDef.randomSpawnChance)
                    {
                        BoundsInt proposedBounds;
                        Vector3Int proposedCenter;
                        int proposedRadius;

                        if (miniLocDef.isCircular)
                        {
                            proposedCenter = currentCell;
                            proposedRadius = Random.Range(miniLocDef.minSize, miniLocDef.maxSize);

                            if (proposedCenter.x - proposedRadius < 0 || proposedCenter.x + proposedRadius >= width ||
                                proposedCenter.y - proposedRadius < 0 || proposedCenter.y + proposedRadius >= height)
                            {
                                continue;
                            }
                            proposedBounds = new BoundsInt(proposedCenter.x - proposedRadius, proposedCenter.y - proposedRadius, 0, proposedRadius * 2 + 1, proposedRadius * 2 + 1, 1);
                        }
                        else // is prefab
                        {
                            proposedCenter = currentCell;
                            proposedRadius = 0; // Не используется для префабов
                            // Для префаба берем 5x5 область для проверки наложения
                            proposedBounds = new BoundsInt(currentCell.x - 2, currentCell.y - 2, 0, 5, 5, 1);
                        }

                        // --- Проверка на наложение с уже сгенерированными мини-локациями ---
                        bool overlapsExistingMiniLocation = false;
                        foreach (var existingMiniLocInfo in activeMiniLocations)
                        {
                            // Manual Overlaps check for BoundsInt
                            if (proposedBounds.xMin < existingMiniLocInfo.Bounds.xMax && proposedBounds.xMax > existingMiniLocInfo.Bounds.xMin &&
                                proposedBounds.yMin < existingMiniLocInfo.Bounds.yMax && proposedBounds.yMax > existingMiniLocInfo.Bounds.yMin)
                            {
                                overlapsExistingMiniLocation = true;
                                Debug.Log($"Attempted mini-location {miniLocDef.locationName} at {currentCell} overlaps with existing {existingMiniLocInfo.Definition.locationName}. Skipping.");
                                break;
                            }
                        }
                        if (overlapsExistingMiniLocation) continue; // Если накладывается, пробуем следующую

                        // Если нет наложений, то генерируем мини-локацию
                        if (miniLocDef.isCircular)
                        {
                            activeMiniLocations.Add(new GeneratedMiniLocationInfo
                            {
                                Definition = miniLocDef,
                                Bounds = proposedBounds, // Используем уже вычисленные proposedBounds
                                Center = proposedCenter,
                                Radius = proposedRadius
                            });

                            int miniLocNoiseOffsetX = Random.Range(-10000, 10000);
                            int miniLocNoiseOffsetY = Random.Range(-10000, 10000);

                            Debug.Log($"Successfully started generation for circular mini-location: {miniLocDef.locationName} at ({proposedCenter.x}, {proposedCenter.y}) with radius {proposedRadius}.");


                            for (int circleX = proposedBounds.xMin; circleX < proposedBounds.xMax; circleX++)
                            {
                                for (int circleY = proposedBounds.yMin; circleY < proposedBounds.yMax; circleY++)
                                {
                                    float distanceToCircleCenter = Vector2.Distance(new Vector2(proposedCenter.x, proposedCenter.y), new Vector2(circleX, circleY));

                                    // Для круговых локаций, плитка всегда размещается в рамках радиуса.
                                    bool shouldPlaceTileInThisLocation = (distanceToCircleCenter <= proposedRadius);

                                    if (shouldPlaceTileInThisLocation)
                                    {
                                        Vector3Int tilePos = new Vector3Int(circleX, circleY, 0);
                                        if (groundTilemap.HasTile(tilePos))
                                        {
                                            TileBase tileToPlace = null;

                                            // --- НОВАЯ ЛОГИКА РАЗМЕЩЕНИЯ ЖИДКОСТИ ВНУТРИ ЛОКАЦИИ (как ты просил) ---
                                            if (miniLocDef.groundTiles != null && miniLocDef.groundTiles.Count > 0)
                                            {
                                                // Для CentralLake (лава)
                                                if (miniLocDef.liquidPlacementStyle == LiquidPlacementStyle.CentralLake)
                                                {
                                                    float normalizedDistance = distanceToCircleCenter / proposedRadius; // 0 в центре, 1 на краю
                                                    if (miniLocDef.liquidTile != null && normalizedDistance < (1.0f - miniLocDef.liquidTileChance))
                                                    {
                                                        tileToPlace = miniLocDef.liquidTile;
                                                    }
                                                    else
                                                    {
                                                        tileToPlace = miniLocDef.groundTiles[Random.Range(0, miniLocDef.groundTiles.Count)];
                                                    }
                                                }
                                                // Для IrregularPatches (болото)
                                                else if (miniLocDef.liquidPlacementStyle == LiquidPlacementStyle.IrregularPatches)
                                                {
                                                    float liquidShapeNoise = Mathf.PerlinNoise(
                                                        (float)(circleX + miniLocNoiseOffsetX) * miniLocDef.liquidShapeNoiseScale,
                                                        (float)(circleY + miniLocNoiseOffsetY) * miniLocDef.liquidShapeNoiseScale
                                                    );
                                                    // Если шум выше порога, и есть жидкий тайл, размещаем жидкость
                                                    if (miniLocDef.liquidTile != null && liquidShapeNoise > miniLocDef.liquidShapeThreshold)
                                                    {
                                                        tileToPlace = miniLocDef.liquidTile;
                                                    }
                                                    else
                                                    {
                                                        tileToPlace = miniLocDef.groundTiles[Random.Range(0, miniLocDef.groundTiles.Count)];
                                                    }
                                                }
                                                // Если стиль не указан или LiquidPlacementStyle.None, просто смешиваем землю/жидкость базовым шумом
                                                else
                                                {
                                                    float liquidMixNoise = Mathf.PerlinNoise(
                                                        (float)(circleX + miniLocNoiseOffsetX) * miniLocDef.miniLocationNoiseScale,
                                                        (float)(circleY + miniLocNoiseOffsetY) * miniLocDef.miniLocationNoiseScale
                                                    );
                                                    if (miniLocDef.liquidTile != null && liquidMixNoise < miniLocDef.liquidTileChance)
                                                    {
                                                        tileToPlace = miniLocDef.liquidTile;
                                                    }
                                                    else
                                                    {
                                                        tileToPlace = miniLocDef.groundTiles[Random.Range(0, miniLocDef.groundTiles.Count)];
                                                    }
                                                }
                                            }
                                            // Если наземных тайлов нет (список пуст), но есть жидкий - ставим только жидкий
                                            else if (miniLocDef.liquidTile != null)
                                            {
                                                tileToPlace = miniLocDef.liquidTile;
                                                Debug.LogWarning($"MiniLocation {miniLocDef.locationName} at {tilePos}: Ground Tiles list is empty. Only placing liquid tile.");
                                            }
                                            // Если есть только наземные тайлы (но нет жидкого)
                                            else if (miniLocDef.groundTiles != null && miniLocDef.groundTiles.Count > 0)
                                            {
                                                tileToPlace = miniLocDef.groundTiles[Random.Range(0, miniLocDef.groundTiles.Count)];
                                            }
                                            // Если нет ни жидкого, ни наземных тайлов (ошибка конфигурации)
                                            else
                                            {
                                                Debug.LogWarning($"MiniLocation {miniLocDef.locationName} at {tilePos} has no valid tiles (groundTiles list is empty and liquidTile is null) defined to place.");
                                            }

                                            if (tileToPlace != null)
                                            {
                                                groundTilemap.SetTile(tilePos, tileToPlace);
                                            }
                                        }
                                    }
                                }
                            }
                            currentSpawnCounts[miniLocDef]++;
                            Debug.Log($"Successfully generated circular mini-location: {miniLocDef.locationName}. Total spawned: {currentSpawnCounts[miniLocDef]}");
                        }
                        else if (miniLocDef.locationPrefab != null)
                        {
                            Vector3 spawnPosition = groundTilemap.CellToWorld(currentCell) + new Vector3(0.5f, 0.5f, 0);
                            if (Physics2D.OverlapCircle(spawnPosition, 1.0f) == null)
                            {
                                Instantiate(miniLocDef.locationPrefab, spawnPosition, Quaternion.identity, transform);
                                currentSpawnCounts[miniLocDef]++;
                                Debug.Log($"Successfully generated prefab mini-location: {miniLocDef.locationName} at ({spawnPosition.x:F2}, {spawnPosition.y:F2}). Total spawned: {currentSpawnCounts[miniLocDef]}");

                                activeMiniLocations.Add(new GeneratedMiniLocationInfo { Definition = miniLocDef, Bounds = proposedBounds }); // Используем proposedBounds
                            }
                            else
                            {
                                Debug.Log($"Failed to spawn prefab mini-location {miniLocDef.locationName} at {spawnPosition} due to overlap with other colliders.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"MiniLocation {miniLocDef.locationName} has isCircular=false but no Location Prefab assigned.");
                        }
                    }
                }
            }
        }
        Debug.Log("Finished mini-location generation. Total active mini-locations for music: " + activeMiniLocations.Count);
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
        int attempts = 200;
        for (int i = 0; i < attempts; i++)
        {
            int randomX = Random.Range(0, width);
            int randomY = Random.Range(0, height);
            Vector3Int cellPosition = new Vector3Int(randomX, randomY, 0);

            BiomeDefinition actualBiomeDef = GetBiomeDefinitionForHeight(cellPosition.y);

            if (actualBiomeDef != null && actualBiomeDef.biomeType == biome)
            {
                Vector3 worldPosition = groundTilemap.CellToWorld(cellPosition) + new Vector3(0.5f, 0.5f, 0);

                bool overlapsMiniLocation = false;
                foreach (var miniLocInfo in activeMiniLocations)
                {
                    if (miniLocInfo.Bounds.Contains(cellPosition))
                    {
                        overlapsMiniLocation = true;
                        break;
                    }
                }
                if (overlapsMiniLocation)
                {
                    continue;
                }


                float checkRadius = 0.5f;
                Collider2D hitCollider = Physics2D.OverlapCircle(worldPosition, checkRadius);

                if (hitCollider == null)
                {
                    return worldPosition;
                }
            }
        }
        Debug.LogWarning($"Не удалось найти свободную позицию для спавна в биоме {biome} после {attempts} попыток. Возможно, карта слишком плотная или настроены слишком большие OverlapCircle радиусы.");
        return null;
    }
    #endregion

    #region Музика та визначення біомів
    private void DetectAndSwitchBiomeMusic()
    {
        if (playerTransform == null || audioSource == null) return;

        Vector3Int playerCell = groundTilemap.WorldToCell(playerTransform.position);

        AudioClip intendedMusicClip = null;
        string debugSource = "Not Found";

        MiniLocationDefinition currentPlayersMiniLocDef = null;
        foreach (var miniLocInfo in activeMiniLocations)
        {
            if (miniLocInfo.Definition.isCircular)
            {
                float distanceToCenter = Vector2.Distance(new Vector2(miniLocInfo.Center.x, miniLocInfo.Center.y), new Vector2(playerCell.x, playerCell.y));
                if (distanceToCenter <= miniLocInfo.Definition.musicDetectionRadius)
                {
                    currentPlayersMiniLocDef = miniLocInfo.Definition;
                    debugSource = $"Found Mini-Location: {currentPlayersMiniLocDef.locationName} (Circular). Dist: {distanceToCenter:F1}, Music Radius: {miniLocInfo.Definition.musicDetectionRadius}.";
                    break;
                }
            }
            else
            {
                if (miniLocInfo.Bounds.Contains(playerCell))
                {
                    currentPlayersMiniLocDef = miniLocInfo.Definition;
                    debugSource = $"Found Mini-Location: {currentPlayersMiniLocDef.locationName} (Prefab). Player Cell: {playerCell}. Bounds: {miniLocInfo.Bounds}";
                    break;
                }
            }
        }

        if (currentPlayersMiniLocDef != null && currentPlayersMiniLocDef.locationMusic != null)
        {
            intendedMusicClip = currentPlayersMiniLocDef.locationMusic;
            debugSource = $"Playing Mini-Location Music: {currentPlayersMiniLocDef.locationMusic.name} for {currentPlayersMiniLocDef.locationName}";
        }
        else
        {
            BiomeDefinition currentPlayersBiomeDef = GetBiomeDefinitionForHeight(playerCell.y);
            if (currentPlayersBiomeDef != null && currentPlayersBiomeDef.biomeMusic != null)
            {
                intendedMusicClip = currentPlayersBiomeDef.biomeMusic;
                debugSource = $"Playing Main Biome Music: {currentPlayersBiomeDef.biomeMusic.name} for {currentPlayersBiomeDef.biomeType}"; // Исправлена опечатка здесь
            }
            else
            {
                debugSource = "No specific music found for current location. Playing silence.";
            }
        }

        if (currentPlayingMusicClip != intendedMusicClip)
        {
            Debug.Log($"Music switch: From '{currentPlayingMusicClip?.name ?? "None"}' to '{intendedMusicClip?.name ?? "None"}'. Source: {debugSource}.");
            currentPlayingMusicClip = intendedMusicClip;
            StartCoroutine(FadeMusic(intendedMusicClip));
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
        if (newClip != null)
        {
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
        else
        {
            audioSource.volume = 0;
        }
    }

    private BiomeDefinition GetBiomeDefinitionForHeight(int y)
    {
        float normalizedY = (float)y / height;

        if (normalizedY >= frozenOceanStartRatio) return GetBiomeDefinitionByType(BiomeType.FrozenOcean);
        if (normalizedY >= tundraStartRatio) return GetBiomeDefinitionByType(BiomeType.Tundra);
        if (normalizedY >= forestTundraStartRatio) return GetBiomeDefinitionByType(BiomeType.ForestTundra);
        if (normalizedY >= taigaStartRatio) return GetBiomeDefinitionByType(BiomeType.Taiga);
        if (normalizedY >= mixedForestStartRatio) return GetBiomeDefinitionByType(BiomeType.MixedForest);
        if (normalizedY >= broadleafForestStartRatio) return GetBiomeDefinitionByType(BiomeType.BroadleafForest);
        if (normalizedY >= steppeStartRatio) return GetBiomeDefinitionByType(BiomeType.Steppe);
        if (normalizedY >= savannaStartRatio) return GetBiomeDefinitionByType(BiomeType.Savanna);
        if (normalizedY >= jungleStartRatio) return GetBiomeDefinitionByType(BiomeType.Jungle);
        if (normalizedY >= desertStartRatio) return GetBiomeDefinitionByType(BiomeType.Desert);

        return GetBiomeDefinitionByType(BiomeType.EquatorialOcean);
    }

    private BiomeDefinition GetBiomeDefinitionByType(BiomeType type)
    {
        foreach (var biomeDef in allBiomes)
        {
            if (biomeDef.biomeType == type)
            {
                return biomeDef;
            }
        }
        Debug.LogWarning($"WorldManager: BiomeDefinition для типа {type} не найден в списке allBiomes! Убедитесь, что все BiomeDefinition ассеты добавлены в список.");
        return null;
    }
    #endregion
}