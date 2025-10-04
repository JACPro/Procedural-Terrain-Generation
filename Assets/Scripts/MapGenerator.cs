using System;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColourMap, Mesh };
    public DrawMode _drawMode;
    
    [SerializeField] private int _mapWidth;
    [SerializeField] private int _mapHeight;
    [SerializeField] private float _noiseScale;
    
    [SerializeField] private int _octaves;
    [Range(0, 1)]
    [SerializeField] private float _persistence;
    [SerializeField] private float _lacunarity;
    
    [SerializeField] private int _seed;
    [SerializeField] private Vector2 _offset;

    public bool _autoUpdate;
    [SerializeField] private TerrainType[] _regions;
    
    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(_mapWidth, _mapHeight, _seed, _noiseScale, _octaves, _persistence, _lacunarity, _offset);

        Color[] colourMap = new Color[_mapWidth * _mapHeight];
        for (int y = 0; y < _mapHeight; y++)
        {
            for (int x = 0; x < _mapWidth; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < _regions.Length; i++)
                {
                    if (currentHeight <= _regions[i]._height)
                    {
                        colourMap[y * _mapWidth + x] = _regions[i]._color;
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
            mapDisplay.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, _mapWidth, _mapHeight));
        }
        else if (_drawMode == DrawMode.Mesh)
        {
            mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap), TextureGenerator.TextureFromColourMap(colourMap, _mapWidth, _mapHeight));
        }
    }

    private void OnValidate()
    {
        if (_mapWidth < 1) _mapWidth = 1;
        if (_mapHeight < 1) _mapHeight = 1;
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
