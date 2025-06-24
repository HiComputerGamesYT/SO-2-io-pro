using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class BiomeDefinition
{
    public BiomeType type;
    public AudioClip biomeMusic;
    [Range(0.0f, 1.0f)]
    public float primaryTileChance = 0.8f;
    public TileBase primaryTile;
    public TileBase secondaryTile;
}