using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq; // ��� ������������ .Any()

// ������ ���� ���� ��� ��������
public enum BiomeType { None, Snow, Grass, Mountain }

// ���� ��� ���������� �������� ���� �� ���� ������������ ��� ������������ � ���������
[System.Serializable]
public class BiomeDefinition
{
    [Tooltip("��� ���� (��� ����������� ������������ �� ������).")]
    public BiomeType type;
    [Tooltip("���� ��� ��� ����� ����.")]
    public AudioClip biomeMusic;
    [Tooltip("���� ������ ���������� ����� (���������, ��� ��� �������� ����, ����� ��� ����'�����).")]
    [Range(0.0f, 1.0f)]
    public float primaryTileChance = 0.8f;
    [Tooltip("��������� ���� ��� ����� ����.")]
    public TileBase primaryTile;
    [Tooltip("��������� ���� ��� ����� ���� (���������, �����/����).")]
    public TileBase secondaryTile;
}

public class WorldManager : MonoBehaviour
{
    // --- ������������ Tilemap ---
    [Header("Tilemap")]
    [Tooltip("��������� �� Tilemap ��� ����. ������� GameObject � TilemapRenderer �� Tilemap Collider 2D.")]
    public Tilemap groundTilemap;

    // --- ������������ ��������� ���� ---
    [Header("������������ ����")]
    [Tooltip("������ ������������ ���� (� �������).")]
    public int width = 300;
    [Tooltip("������ ������������ ���� (� �������).")]
    public int height = 300;
    [Tooltip("������ �������� ���� (������ �����, � �������).")]
    public int topBiomeHeight = 100;
    [Tooltip("������ �������� ���� (����� �����, � �������).")]
    public int bottomBiomeHeight = 100;

    // --- ������������ ����� ---
    [Header("������������ ����� (�������� � ���������!)")]
    public BiomeDefinition snowBiome;
    public BiomeDefinition grassBiome;
    public BiomeDefinition mountainBiome;
    [Tooltip("�������� ��������/����� ������ ��� ��� ����.")]
    public float musicFadeSpeed = 1.0f; // �������� (� ��������)

    // ������� ����
    private AudioSource audioSource;
    private BiomeType currentBiomeType = BiomeType.None; // �������� �������� ���
    private Transform playerTransform; // ��������� �� ��'��� ������
    private PlayerController playerController; // ��������� �� ��������� ������ ��� ������������ ���
    private Dictionary<string, TileBase> assignedTilesMap; // ������� ��� ��������� ����� �� ������

    void Awake()
    {
        // �������� �� ����������� Tilemap
        if (groundTilemap == null)
        {
            groundTilemap = FindObjectOfType<Tilemap>();
            if (groundTilemap == null) { Debug.LogError("WorldManager: GroundTilemap �� �������� � ����."); return; }
        }

        // ����������� AudioSource ��� ������
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.loop = true; // ���������� ������
        audioSource.playOnAwake = false; // �� ����� ��� �������

        // ���������� ������� ����������� ����� � BiomeDefinitions
        assignedTilesMap = new Dictionary<string, TileBase>();
        AddBiomeTilesToMap(snowBiome);
        AddBiomeTilesToMap(grassBiome);
        AddBiomeTilesToMap(mountainBiome);

        // ���� ���� ����� ��� �� ������, ��������� ������
        if (assignedTilesMap.Count < 5) // ���������� ����� 5 ��������� �����
        {
            Debug.LogWarning("WorldManager: ���� ����� ����� �� ��������� � Biome Definitions. ������� �� ��������� �� ��������� ������. ������������� ���������� Assets Tile ��� ������� ��������.");
            CreateFallbackTilesProgrammatically();
        }
    }

    void Start()
    {
        // ������ ������ �� ����� "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }
        else
        {
            Debug.LogError("WorldManager: ��'��� ������ � ����� 'Player' �� ��������. ������ ���� �� ��������� ���� �� �������������.");
        }

        GenerateWorld(); // ��������� ��������� ����
    }

    void Update()
    {
        // ����������� ������ � ��������� �� ������� ������
        if (playerTransform != null)
        {
            DetectAndSwitchBiomeMusic();
        }
    }

    // ���� ����� � BiomeDefinition �� ��������
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

    // ������� �������� �����, ���� �� �� ���������� (�� fallback)
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
        // �� ������� �����, ���� BiomeDefinitions �� ��������
        if (!assignedTilesMap.ContainsKey("SnowTile")) createAndAssignTile("SnowTile", Color.white, (t) => snowBiome.primaryTile = t);
        if (!assignedTilesMap.ContainsKey("GrassTile")) createAndAssignTile("GrassTile", Color.green, (t) => grassBiome.primaryTile = t);
        if (!assignedTilesMap.ContainsKey("DirtTile")) createAndAssignTile("DirtTile", new Color(0.5f, 0.3f, 0.1f), (t) => grassBiome.secondaryTile = t);
        if (!assignedTilesMap.ContainsKey("StoneTile")) createAndAssignTile("StoneTile", new Color(0.5f, 0.5f, 0.5f), (t) => mountainBiome.primaryTile = t);
        if (!assignedTilesMap.ContainsKey("MountainBaseTile")) createAndAssignTile("MountainBaseTile", new Color(0.2f, 0.2f, 0.2f), (t) => mountainBiome.secondaryTile = t);
    }

    // ��������� ����
    void GenerateWorld()
    {
        groundTilemap.ClearAllTiles(); // ������� �������� �����

        // �������� �������� ����� ��� Perlin Noise
        float globalOffsetX = Random.Range(0f, 99999f);
        float globalOffsetY = Random.Range(0f, 99999f);

        // ���������� ��� ���� � ������� �����������
        // ����� Tilemap � Unity �������� ����������� � (0,0) ������� �����������
        float worldMinX = groundTilemap.origin.x + groundTilemap.cellSize.x / 2f;
        float worldMaxX = groundTilemap.origin.x + groundTilemap.size.x * groundTilemap.cellSize.x - groundTilemap.cellSize.x / 2f;
        float worldMinY = groundTilemap.origin.y + groundTilemap.cellSize.y / 2f;
        float worldMaxY = groundTilemap.origin.y + groundTilemap.size.y * groundTilemap.cellSize.y - groundTilemap.cellSize.y / 2f;

        // �������� ���������� ���, ���� Tilemap �� �������� ���������� �������
        // ��� ���� ���� �� ����������� � 0,0.
        // ������ this.width/2f, ������������� ������� ������ Tilemap � ������� ��������.
        float mapBoundsMinX = groundTilemap.CellToWorld(new Vector3Int(0, 0, 0)).x;
        float mapBoundsMaxX = groundTilemap.CellToWorld(new Vector3Int(width, 0, 0)).x;
        float mapBoundsMinY = groundTilemap.CellToWorld(new Vector3Int(0, 0, 0)).y;
        float mapBoundsMaxY = groundTilemap.CellToWorld(new Vector3Int(0, height, 0)).y;

        // �������� ��� ����� �������
        if (playerController != null)
        {
            playerController.SetMapBounds(mapBoundsMinX, mapBoundsMaxX, mapBoundsMinY, mapBoundsMaxY);
            Debug.Log($"��� ����� �����������: X ({mapBoundsMinX:F2}, {mapBoundsMaxX:F2}), Y ({mapBoundsMinY:F2}, {mapBoundsMaxY:F2})");
        }

        // ����� ������ � ����'����� ���
        if (playerTransform != null)
        {
            // ���������� Y-�������� ��� ����'����� ���� � ������� �����������
            // ����������, �� ����'���� ��� ����������� �� ����� �� ������ ������
            float grassBiomeWorldYStart = groundTilemap.CellToWorld(new Vector3Int(0, bottomBiomeHeight, 0)).y;
            float grassBiomeWorldYEnd = groundTilemap.CellToWorld(new Vector3Int(0, height - topBiomeHeight, 0)).y;

            // ����������, �� � ������ ����'���� ���
            if (grassBiomeWorldYStart < grassBiomeWorldYEnd)
            {
                // ��������� ������� � ����� ����'����� ����
                float spawnX = Random.Range(mapBoundsMinX, mapBoundsMaxX);
                float spawnY = Random.Range(grassBiomeWorldYStart, grassBiomeWorldYEnd);
                playerTransform.position = new Vector3(spawnX, spawnY, 0); // ������� Z �� ���� 0 ��� 2D
                Debug.Log($"������� ����������� � ����'����� ��� �� ({spawnX:F2}, {spawnY:F2})");
            }
            else
            {
                Debug.LogWarning("WorldManager: ����'���� ��� ������� ��� ������� �����. ������� ������������ � ����� ����.");
                playerTransform.position = Vector3.zero;
            }
        }


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileBase tileToPlace = null;
                // ��� ��� ������������ � ����� ����
                float terrainNoiseValue = Mathf.PerlinNoise((float)x / width * 20f + globalOffsetX, (float)y / height * 20f + globalOffsetY);

                // ���������� ���� �� Y-����������� ������
                // ������ (x, y) � ��������� ����������� Tilemap
                // � ��� � fixed-zones, ���� ������ ������������� y ��� ���������� ����.
                if (y >= height - topBiomeHeight) // ������ ���� (����)
                {
                    if (snowBiome != null && snowBiome.primaryTile != null && snowBiome.secondaryTile != null)
                    {
                        tileToPlace = terrainNoiseValue > (1f - snowBiome.primaryTileChance) ? snowBiome.primaryTile : snowBiome.secondaryTile;
                    }
                }
                else if (y < bottomBiomeHeight) // ����� ���� (����/�����)
                {
                    if (mountainBiome != null && mountainBiome.primaryTile != null && mountainBiome.secondaryTile != null)
                    {
                        tileToPlace = terrainNoiseValue > (1f - mountainBiome.primaryTileChance) ? mountainBiome.primaryTile : mountainBiome.secondaryTile;
                    }
                }
                else // ������� ���� (�����)
                {
                    if (grassBiome != null && grassBiome.primaryTile != null && grassBiome.secondaryTile != null)
                    {
                        tileToPlace = terrainNoiseValue > (1f - grassBiome.primaryTileChance) ? grassBiome.primaryTile : grassBiome.secondaryTile;
                    }
                }

                // ������������ ���� �� Tilemap
                if (tileToPlace != null)
                {
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), tileToPlace);
                }
                else
                {
                    // ���� ���� �� ��� �����������, ����� ��������� ���������, ���������, �����
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), grassBiome?.primaryTile ?? null);
                }
            }
        }
        Debug.Log($"��� ������� {width}x{height} �����������.");

        // ϳ��� ��������� ���� �� ������ ������, ���������� ������ ����
        if (playerTransform != null)
        {
            DetectAndSwitchBiomeMusic();
        }
    }

    // ������� �������� ��� ������ �� �������� ������
    private void DetectAndSwitchBiomeMusic()
    {
        BiomeType newBiome = BiomeType.None;
        float playerWorldY = playerTransform.position.y;

        // ������������ ������ Y-���������� ������ � Y-���������� ������
        // �� ��������� ���������, � ����� ��� (���) ����������� �������
        // groundTilemap.WorldToCell(playerTransform.position).y ������� Y-������ ������
        int playerTileY = groundTilemap.WorldToCell(playerTransform.position).y;

        // ����������, � ��� ��� ����������� ������� �� ���� Y-����������� ������
        if (playerTileY >= height - topBiomeHeight) // ������ ��� (���)
        {
            newBiome = BiomeType.Snow;
        }
        else if (playerTileY < bottomBiomeHeight) // ����� ���� (����)
        {
            newBiome = BiomeType.Mountain;
        }
        else // ������� ���� (�����)
        {
            newBiome = BiomeType.Grass;
        }

        // ���� ��� �������, ���������� ������
        if (newBiome != currentBiomeType)
        {
            currentBiomeType = newBiome;
            SwitchBiomeMusic(currentBiomeType);
        }
    }

    // �������� �������� ��� ��� ��������� ����
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
            case BiomeType.Mountain: // ����������: Type ������ None.
                if (mountainBiome != null) targetClip = mountainBiome.biomeMusic;
                break;
        }

        if (targetClip != null && audioSource.clip != targetClip)
        {
            StartCoroutine(FadeMusic(audioSource, targetClip, musicFadeSpeed));
            Debug.Log($"������ ������ �� ���: {biome}");
        }
        else if (targetClip == null && audioSource.isPlaying)
        {
            StartCoroutine(FadeMusic(audioSource, null, musicFadeSpeed));
            Debug.LogWarning($"������ ��� ���� {biome} �� ����������. ������ ��������.");
        }
    }

    // �������� ��� �������� �������� �� ����� ������
    private System.Collections.IEnumerator FadeMusic(AudioSource audioSource, AudioClip newClip, float fadeDuration)
    {
        // �������� ��������� �������, ��� ����������� �� ��
        float initialVolume = 1f; // ����������, �� ����� ������� - 1
        if (audioSource.clip != null && audioSource.isPlaying)
        {
            initialVolume = audioSource.volume; // ���� ������ ��� ���, ������ �� ������� �������
            // �������� ������� ������
            float timer = 0f;
            while (timer < fadeDuration && audioSource.volume > 0)
            {
                audioSource.volume = Mathf.Lerp(initialVolume, 0, timer / fadeDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            audioSource.Stop();
        }

        audioSource.volume = 0; // ������������ ������� �� 0 ����� �������� ���� ���
        audioSource.clip = newClip;

        if (newClip != null)
        {
            audioSource.Play();
            // ����� ���� ������
            float timer = 0f;
            while (timer < fadeDuration && audioSource.volume < 1) // ����������, �� ������ ������� �� ���� 1
            {
                audioSource.volume = Mathf.Lerp(0, 1, timer / fadeDuration); // ������ �������� �� 1
                timer += Time.deltaTime;
                yield return null;
            }
            audioSource.volume = 1f; // ������������ �� ����� �������
        }
    }
}
