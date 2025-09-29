using System;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
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
    
    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(_mapWidth, _mapHeight, _seed, _noiseScale, _octaves, _persistence, _lacunarity, _offset);
        
        MapDisplay mapDisplay = GetComponent<MapDisplay>();
        mapDisplay.DrawNoiseMap(noiseMap);
    }

    private void OnValidate()
    {
        if (_mapWidth < 1) _mapWidth = 1;
        if (_mapHeight < 1) _mapHeight = 1;
        if (_lacunarity < 1) _lacunarity = 1;
        if (_octaves < 1) _octaves = 1;
    }
}
