using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NoiseData", menuName = "Scriptable Objects/NoiseData")]
public class NoiseData : UpdatableData
{
    public Noise.NormalizeMode _normalizeMode;
    
    public float _noiseScale;
    
    public int _octaves;
    [Range(0, 1)]
    public float _persistence;
    public float _lacunarity;
    
    public int _seed;
    public Vector2 _offset;

    protected override void OnValidate()
    {
        if (_lacunarity < 1)
        {
            _lacunarity = 1;
        }

        if (_octaves < 1)
        {
            _octaves = 1;
        }
        
        base.OnValidate();
    }
}
