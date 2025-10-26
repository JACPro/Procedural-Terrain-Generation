using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColourMap, Mesh };
    public DrawMode _drawMode;

    public const int MapChunkSize = 241;
    
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
    
    private Queue<MapThreadInfo<MapData>> _mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> _meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData();
        
        MapDisplay mapDisplay = GetComponent<MapDisplay>();
        if (_drawMode == DrawMode.NoiseMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData._heightMap));
        }
        else if (_drawMode == DrawMode.ColourMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromColourMap(mapData._colorMap, MapChunkSize, MapChunkSize));
        }
        else if (_drawMode == DrawMode.Mesh)
        {
            int levelOfSimplification = 6 - _levelOfDetail; // invert the level of detail to get the level of simplification used by the mesh generator
            mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData._heightMap, _meshHeightMultiplier, _meshHeightCurve, levelOfSimplification), 
                TextureGenerator.TextureFromColourMap(mapData._colorMap, MapChunkSize, MapChunkSize));
        }
    }
    
    public void RequestMapData(Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(callback);
        };
        
        new Thread(threadStart).Start();
    }

    void MapDataThread(Action<MapData> callback)
    {
        MapData mapData = GenerateMapData();
        lock (_mapDataThreadInfoQueue)
        {
            _mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }
    
    public void RequestMeshData(MapData mapData, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, callback);
        };
        
        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData._heightMap, _meshHeightMultiplier, _meshHeightCurve, 6 - _levelOfDetail);
        lock (_meshDataThreadInfoQueue)
        {
            _meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
        if (_mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < _mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = _mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        
        if (_meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < _meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = _meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    private MapData GenerateMapData()
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
        
        return new MapData(noiseMap, colourMap);
    }

    private void OnValidate()
    {
        if (_lacunarity < 1) _lacunarity = 1;
        if (_octaves < 1) _octaves = 1;
    }

    public struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType
{
    public string _name;
    public float _height;
    public Color _color;
}

public struct MapData
{
    public readonly float[,] _heightMap;
    public readonly Color[] _colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        _heightMap = heightMap;
        _colorMap = colorMap;
    }
}