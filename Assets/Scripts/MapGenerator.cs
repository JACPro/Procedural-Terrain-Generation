using System;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColourMap, Mesh };
    public DrawMode _drawMode;

    private const int MapChunkSize = 241;
    
    [Range(0, 6)]
    [SerializeField] private int _levelOfDetail;
    
    [SerializeField] private float _noiseScale;
    
    [SerializeField] private int _octaves;
    [Range(0, 1)]
    [SerializeField] private float _persistence;
    [SerializeField] private float _lacunarity;
    
    [SerializeField] private int _seed;
    [SerializeField] private Vector2 _offset;
    
    [SerializeField] private float _meshHeightMultiplier;
    [SerializeField] private AnimationCurve _meshHeightCurve;

    public bool _autoUpdate;
    [SerializeField] private TerrainType[] _regions;
    
    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(MapChunkSize, MapChunkSize, _seed, _noiseScale, _octaves, _persistence, _lacunarity, _offset);

        Color[] colourMap = new Color[MapChunkSize * MapChunkSize];
        for (int y = 0; y < MapChunkSize; y++)
        {
            for (int x = 0; x < MapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < _regions.Length; i++)
                {
                    if (currentHeight <= _regions[i]._height)
                    {
                        colourMap[y * MapChunkSize + x] = _regions[i]._color;
                        break;
                    }
                }
            }
        }
        
        MapDisplay mapDisplay = GetComponent<MapDisplay>();
        if (_drawMode == DrawMode.NoiseMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        }
        else if (_drawMode == DrawMode.ColourMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, MapChunkSize, MapChunkSize));
        }
        else if (_drawMode == DrawMode.Mesh)
        {
            int levelOfSimplification = 6 - _levelOfDetail; // invert the level of detail to get the level of simplification used by the mesh generator
            mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, _meshHeightMultiplier, _meshHeightCurve, levelOfSimplification), TextureGenerator.TextureFromColourMap(colourMap, MapChunkSize, MapChunkSize));
        }
    }

    private void OnValidate()
    {
        if (_lacunarity < 1) _lacunarity = 1;
        if (_octaves < 1) _octaves = 1;
    }
}

[System.Serializable]
public struct TerrainType
{
    public string _name;
    public float _height;
    public Color _color;
}
