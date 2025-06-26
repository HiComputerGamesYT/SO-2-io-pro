// SO 2 io/Assets/BiomeDefinition.cs
using UnityEngine;
using System.Collections.Generic; // ���������, ���� ����� ���
using UnityEngine.Tilemaps; // ���������, ���� ����� ���

// ���� ������� ��������� ��� ��������� BiomeDefinition ��� ����� Asset � ���� "Create" Unity.
[CreateAssetMenu(fileName = "New Biome Definition", menuName = "ScriptableObjects/Biome Definition")]
public class BiomeDefinition : ScriptableObject // <--- ����� �����: ����� ������ ����������� �� ScriptableObject
{
    public BiomeType biomeType; // ��� �����, ��������� �� BiomeType.cs

    [Header("Tile Settings")] // ��������� � ���������� ��� ��������
    [Range(0.0f, 1.0f)]
    public float primaryTileChance = 0.8f; // ���� ��������� ��������� �����
    public TileBase primaryTile; // �������� ���� ��� ����� �����
    public TileBase secondaryTile; // �������������� ���� ��� ����� �����

    [Header("Audio")] // ��������� � ���������� ��� ��������
    public AudioClip biomeMusic; // ������ ��� ����� �����

    [Header("Resources")] // ��������� � ���������� ��� ��������
    public List<SpawnableResourceConfig> spawnableResources; // ������ ��������, ������� ����� ���������

    [Header("Mobs")] // ��������� � ���������� ��� ��������
    public List<SpawnableMobConfig> spawnableMobs; // ������ �����, ������� ����� ���������
}

// ��� ������ ����� ��� ��������� �������� � ����� � ����������.
// ��� ������ ���� Serializable, ����� Unity ��� �� ���������.
[System.Serializable]
public class SpawnableResourceConfig
{
    public GameObject worldItemPrefab; // ������ �������, ������� �������� � ���� (��������, ������, ������)
    public Item itemData; // ������ �� ��� ScriptableObject Item (�� ��� ��� ����������)
    [Range(0f, 1f)]
    public float spawnChance = 0.1f; // ���� ��������� �������
    public int initialAmount = 1; // ���������� �������, ������� ����� �������
}

[System.Serializable]
public class SpawnableMobConfig
{
    public GameObject mobPrefab; // ������ ����
    public int initialSpawnCount = 1; // ������� ����� �������� ��� ��������� ����
    public int maxMobsInBiome = 5; // ������������ ���������� ����� ����� ���� � �����
}