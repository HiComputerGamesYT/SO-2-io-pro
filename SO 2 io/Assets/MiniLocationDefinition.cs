// SO 2 io/Assets/MiniLocationDefinition.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

// �����: ����������, ��� ����� �������������� �������� ������ ����-�������
public enum LiquidPlacementStyle
{
    None, // ��� ������������� ��������� ��������, ��� ������� ����� ���
    CentralLake, // �������� � ������ (��� ����, ������� ������)
    IrregularPatches // �������� � ���� ������������ ����� (��� ������)
}

[CreateAssetMenu(fileName = "New Mini Location", menuName = "ScriptableObjects/Mini Location Definition")]
public class MiniLocationDefinition : ScriptableObject
{
    public string locationName; // �������� ����-�������

    [Header("�������� ���������")]
    public GameObject locationPrefab; // ������ ��� ����-�������. ������������, ���� isCircular = false.

    [Header("��������� ������ ������� (��� isCircular = true)")]
    public List<TileBase> groundTiles; // ������ �������� ������ ��� ���� ����-�������
    public TileBase liquidTile;       // ���� �������� (����/������/����), ���� ���������
    [Range(0.0f, 1.0f)]
    public float liquidTileChance = 0.1f; // ��������� ������� ����� (��� CentralLake) ��� ����� (��� IrregularPatches)
    [Range(0.0f, 0.1f)]
    public float miniLocationNoiseScale = 0.05f; // ������� ���� ��� ������������� ������ ������ ����-������� (��� ��������/���������)
    public int miniLocationNoiseSeedOffset = 54321; // ��� ��� ���� ������ ����-�������

    // --- ����� ����: ����� ���������� �������� ---
    public LiquidPlacementStyle liquidPlacementStyle = LiquidPlacementStyle.None;

    // --- ����� ���� ��� ����� �������� (��� IrregularPatches) ---
    [Header("��������� ����� �������� (��� IrregularPatches)")]
    [Range(0.0f, 0.2f)]
    public float liquidShapeNoiseScale = 0.08f; // ������� ���� ��� ���������� ����� ��������
    public int liquidShapeNoiseSeedOffset = 67890; // ��� ��� ���� ����� ��������
    [Range(0.0f, 1.0f)]
    public float liquidShapeThreshold = 0.5f; // ����� ���� ��� ����������� ����� �������� (0.5 = ��������, ������ = ����� "�������")

    [Header("��������� ���������")] // ���� ��������� ������ �����
    public bool isCircular = true;    // ������������ �� ������� ������ (������� �����). ���� false, ��������� locationPrefab.
    public int minSize = 5;           // ����������� ������/������ ��� �������� �������
    public int maxSize = 15;          // ������������ ������/������

    // ���� ����-������� ����� ���������� ������ � ������������ ������
    public List<BiomeType> allowedBiomes; // ������ ������, ��� ��� ����-������� ����� ���������.

    [Header("�������������� ��������")]
    public List<SpawnableResourceConfig> spawnableResources;
    public List<SpawnableMobConfig> spawnableMobs;
    public AudioClip locationMusic;

    [Header("��������� �����")]
    [Range(0, 50)]
    public int musicDetectionRadius = 10; // ������ (� �������) ������ ������ �������� ����-������� ��� ��������� ������


    [Range(0f, 1f)]
    public float randomSpawnChance = 0.01f;
    public int maxRandomSpawns = 3;
}