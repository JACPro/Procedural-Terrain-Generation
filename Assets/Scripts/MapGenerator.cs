using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private int _mapWidth;
    [SerializeField] private int _mapHeight;
    [SerializeField] private float _noiseScale;

    public bool _autoUpdate;
    
    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(_mapWidth, _mapHeight, _noiseScale);
        
        MapDisplay mapDisplay = GetComponent<MapDisplay>();
        mapDisplay.DrawNoiseMap(noiseMap);
    }
}
