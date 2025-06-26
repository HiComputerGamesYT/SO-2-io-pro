// SO 2 io/Assets/BiomeDefinition.cs
using UnityEngine;
using System.Collections.Generic; // Добавлено, если этого нет
using UnityEngine.Tilemaps; // Добавлено, если этого нет

// Этот атрибут позволяет вам создавать BiomeDefinition как новый Asset в меню "Create" Unity.
[CreateAssetMenu(fileName = "New Biome Definition", menuName = "ScriptableObjects/Biome Definition")]
public class BiomeDefinition : ScriptableObject // <--- ОЧЕНЬ ВАЖНО: Класс должен наследовать от ScriptableObject
{
    public BiomeType biomeType; // Тип биома, выбранный из BiomeType.cs

    [Header("Tile Settings")] // Заголовок в инспекторе для удобства
    [Range(0.0f, 1.0f)]
    public float primaryTileChance = 0.8f; // Шанс появления основного тайла
    public TileBase primaryTile; // Основной тайл для этого биома
    public TileBase secondaryTile; // Второстепенный тайл для этого биома

    [Header("Audio")] // Заголовок в инспекторе для удобства
    public AudioClip biomeMusic; // Музыка для этого биома

    [Header("Resources")] // Заголовок в инспекторе для удобства
    public List<SpawnableResourceConfig> spawnableResources; // Список ресурсов, которые могут появиться

    [Header("Mobs")] // Заголовок в инспекторе для удобства
    public List<SpawnableMobConfig> spawnableMobs; // Список мобов, которые могут появиться
}

// Эти классы нужны для настройки ресурсов и мобов в инспекторе.
// Они должны быть Serializable, чтобы Unity мог их сохранять.
[System.Serializable]
public class SpawnableResourceConfig
{
    public GameObject worldItemPrefab; // Префаб объекта, который появится в мире (например, дерево, камень)
    public Item itemData; // Ссылка на ваш ScriptableObject Item (мы это уже исправляли)
    [Range(0f, 1f)]
    public float spawnChance = 0.1f; // Шанс появления ресурса
    public int initialAmount = 1; // Количество ресурса, которое можно собрать
}

[System.Serializable]
public class SpawnableMobConfig
{
    public GameObject mobPrefab; // Префаб моба
    public int initialSpawnCount = 1; // Сколько мобов спавнить при генерации мира
    public int maxMobsInBiome = 5; // Максимальное количество мобов этого типа в биоме
}