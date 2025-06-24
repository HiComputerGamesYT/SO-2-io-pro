using UnityEngine;
using System.Collections.Generic;

// Визначення GamePhase тепер знаходиться в окремому файлі GamePhase.cs

[CreateAssetMenu(fileName = "New Spawner Config", menuName = "Spawners/New Config")]
public class SpawnerConfig : ScriptableObject
{
    [Header("1. Що спавнити")]
    public GameObject prefabToSpawn;

    [Header("2. Де спавнити")]
    public BiomeType targetBiome;

    [Header("3. Коли спавнити")]
    [Tooltip("В які фази дня/ночі/подій цей об'єкт може з'являтися.")]
    public GamePhase[] activePhases;

    [Header("4. Скільки спавнити")]
    public int minCount = 10;
    public int maxCount = 30;

    [Header("5. Як швидко спавнити")]
    public float passiveRespawnTime = 60f;
}