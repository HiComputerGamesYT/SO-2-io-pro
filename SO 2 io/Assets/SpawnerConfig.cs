using UnityEngine;
using System.Collections.Generic;

// ���������� GamePhase ����� ����������� � �������� ���� GamePhase.cs

[CreateAssetMenu(fileName = "New Spawner Config", menuName = "Spawners/New Config")]
public class SpawnerConfig : ScriptableObject
{
    [Header("1. �� ��������")]
    public GameObject prefabToSpawn;

    [Header("2. �� ��������")]
    public BiomeType targetBiome;

    [Header("3. ���� ��������")]
    [Tooltip("� �� ���� ���/����/���� ��� ��'��� ���� �'��������.")]
    public GamePhase[] activePhases;

    [Header("4. ������ ��������")]
    public int minCount = 10;
    public int maxCount = 30;

    [Header("5. �� ������ ��������")]
    public float passiveRespawnTime = 60f;
}