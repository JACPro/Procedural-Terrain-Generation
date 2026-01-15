using UnityEngine;

[CreateAssetMenu(fileName = "TerrainData", menuName = "Scriptable Objects/TerrainData")]
public class TerrainData : UpdatableData
{
    public float _uniformScale = 1.0f;

    public bool _useFlatShading;
    public bool _applyFalloff;
    
    public float _meshHeightMultiplier;
    public AnimationCurve _meshHeightCurve;
}
