// SO 2 io/Assets/MiniLocationDefinition.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

// НОВОЕ: Определяет, как будет генерироваться жидкость внутри мини-локации
public enum LiquidPlacementStyle
{
    None, // Нет специфической генерации жидкости, или жидкого тайла нет
    CentralLake, // Жидкость в центре (как лава, текущая логика)
    IrregularPatches // Жидкость в виде нерегулярных пятен (как болото)
}

[CreateAssetMenu(fileName = "New Mini Location", menuName = "ScriptableObjects/Mini Location Definition")]
public class MiniLocationDefinition : ScriptableObject
{
    public string locationName; // Название мини-локации

    [Header("Основные настройки")]
    public GameObject locationPrefab; // Префаб для мини-локации. Используется, если isCircular = false.

    [Header("Настройки тайлов локации (для isCircular = true)")]
    public List<TileBase> groundTiles; // Список наземных тайлов для этой мини-локации
    public TileBase liquidTile;       // Тайл жидкости (воды/болота/лавы), если применимо
    [Range(0.0f, 1.0f)]
    public float liquidTileChance = 0.1f; // Пропорция жидкого тайла (для CentralLake) или порог (для IrregularPatches)
    [Range(0.0f, 0.1f)]
    public float miniLocationNoiseScale = 0.05f; // Масштаб шума для распределения тайлов внутри мини-локации (для кучности/плавности)
    public int miniLocationNoiseSeedOffset = 54321; // Сид для шума внутри мини-локации

    // --- НОВОЕ ПОЛЕ: Стиль размещения жидкости ---
    public LiquidPlacementStyle liquidPlacementStyle = LiquidPlacementStyle.None;

    // --- НОВЫЕ ПОЛЯ ДЛЯ ФОРМЫ ЖИДКОСТИ (для IrregularPatches) ---
    [Header("Настройки формы жидкости (для IrregularPatches)")]
    [Range(0.0f, 0.2f)]
    public float liquidShapeNoiseScale = 0.08f; // Масштаб шума для деформации формы жидкости
    public int liquidShapeNoiseSeedOffset = 67890; // Сид для шума формы жидкости
    [Range(0.0f, 1.0f)]
    public float liquidShapeThreshold = 0.5f; // Порог шума для определения формы жидкости (0.5 = стандарт, меньше = более "дырявая")

    [Header("Настройки генерации")] // Этот заголовок теперь здесь
    public bool isCircular = true;    // Генерируется ли локация кругом (цельная форма). Если false, спавнится locationPrefab.
    public int minSize = 5;           // Минимальный радиус/размер для круговой локации
    public int maxSize = 15;          // Максимальный радиус/размер

    // Если мини-локация может спавниться только в определенных биомах
    public List<BiomeType> allowedBiomes; // Список биомов, где эта мини-локация МОЖЕТ появиться.

    [Header("Дополнительные элементы")]
    public List<SpawnableResourceConfig> spawnableResources;
    public List<SpawnableMobConfig> spawnableMobs;
    public AudioClip locationMusic;

    [Header("Настройки звука")]
    [Range(0, 50)]
    public int musicDetectionRadius = 10; // Радиус (в клетках) вокруг центра круговой мини-локации для активации музыки


    [Range(0f, 1f)]
    public float randomSpawnChance = 0.01f;
    public int maxRandomSpawns = 3;
}