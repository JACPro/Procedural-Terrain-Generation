using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColourMap, Mesh, FalloffMap };
    [SerializeField] private DrawMode _drawMode;
    
    public TerrainData _terrainData;
    public NoiseData _noiseData;
    
    [FormerlySerializedAs("_levelOfDetail")]
    [Range(0, 6)]
    [SerializeField] private int _editorPreviewLOD;
    
    public bool _autoUpdate;
    [SerializeField] private TerrainType[] _regions;
    private static MapGenerator _instance;

    private float[,] _falloffMap;
    
    private Queue<MapThreadInfo<MapData>> _mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> _meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private void Awake()
    {
        _falloffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize);
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    public static int MapChunkSize
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MapGenerator>();
            }
            
            if (_instance._terrainData._useFlatShading)
            {
                return 95;
            }
            else
            {
                return 239;
            }
        }
    }


    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        
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
            int levelOfSimplification = 6 - _editorPreviewLOD; // invert the level of detail to get the level of simplification used by the mesh generator
            mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData._heightMap, _terrainData._meshHeightMultiplier, _terrainData._meshHeightCurve, levelOfSimplification, _terrainData._useFlatShading), 
                TextureGenerator.TextureFromColourMap(mapData._colorMap, MapChunkSize, MapChunkSize));
        }
        else if (_drawMode == DrawMode.FalloffMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(MapChunkSize)));
        }
    }
    
    public void RequestMapData(Vector2 centre, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(centre, callback);
        };
        
        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 centre, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(centre);
        lock (_mapDataThreadInfoQueue)
        {
            _mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }
    
    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };
        
        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData._heightMap, _terrainData._meshHeightMultiplier, _terrainData._meshHeightCurve, lod, _terrainData._useFlatShading);
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

    private MapData GenerateMapData(Vector2 centre)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(MapChunkSize + 2, MapChunkSize + 2, _noiseData._seed, _noiseData._noiseScale, _noiseData._octaves, 
            _noiseData._persistence, _noiseData._lacunarity, centre + _noiseData._offset, _noiseData._normalizeMode);

        Color[] colourMap = new Color[MapChunkSize * MapChunkSize];
        for (int y = 0; y < MapChunkSize; y++)
        {
            for (int x = 0; x < MapChunkSize; x++)
            {
                if (_terrainData._applyFalloff)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - _falloffMap[x,y]);
                }
                
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < _regions.Length; i++)
                {
                    if (currentHeight >= _regions[i]._height)
                    {
                        colourMap[y * MapChunkSize + x] = _regions[i]._color;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        
        return new MapData(noiseMap, colourMap);
    }

    private void OnValidate()
    {
        if (_terrainData != null)
        {
            _terrainData.OnValuesUpdated -= OnValuesUpdated;
            _terrainData.OnValuesUpdated += OnValuesUpdated;
        }
        
        if (_noiseData != null)
        {
            _noiseData.OnValuesUpdated -= OnValuesUpdated;
            _noiseData.OnValuesUpdated += OnValuesUpdated;
        }
        
        _falloffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize);
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