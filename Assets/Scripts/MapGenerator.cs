using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private int _mapWidth;
    [SerializeField] private int _mapHeight;
    [SerializeField] private float _noiseScale;
    
    [SerializeField] private int _octaves;
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
}
