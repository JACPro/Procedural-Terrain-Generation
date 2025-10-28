using System;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    private const float _viewerMoveThresholdForChunkUpdate = 25.0f;
    private const float _squareViewerMoveThresholdForChunkUpdate = _viewerMoveThresholdForChunkUpdate * _viewerMoveThresholdForChunkUpdate;
    
    [SerializeField] private LODInfo[] _detailLevels;
    private static float _maxViewDist;
    
    [SerializeField] private Transform _viewer;
    
    [SerializeField] private Material _mapMaterial;

    private static Vector2 _viewerPosition;
    private static Vector2 _viewerPositionOld;
    private static MapGenerator _mapGenerator;

    private int _chunkSize;
    private int _chunksVisibleInViewDist;

    Dictionary<Vector2, TerrainChunk> _terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> _terrainChunksVisibleLastUpdate = new List<TerrainChunk>();
    
    private void Start()
    {
        _mapGenerator = FindObjectOfType<MapGenerator>();

        _maxViewDist = _detailLevels[_detailLevels.Length - 1]._visibleDistThreshold;
        _chunkSize = MapGenerator.MapChunkSize - 1;
        _chunksVisibleInViewDist = Mathf.RoundToInt(_maxViewDist / _chunkSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        _viewerPosition = new Vector2(_viewer.position.x, _viewer.position.z);

        if ((_viewerPositionOld - _viewerPosition).sqrMagnitude > _squareViewerMoveThresholdForChunkUpdate)
        {
            _viewerPositionOld = _viewerPosition;
            UpdateVisibleChunks();
        }
    }

    private void UpdateVisibleChunks()
    {
        foreach (TerrainChunk chunk in _terrainChunksVisibleLastUpdate)
        {
            chunk.SetVisible(false);
        }
        _terrainChunksVisibleLastUpdate.Clear();
        
        int currChunkCoordX = Mathf.RoundToInt(_viewerPosition.x / _chunkSize);
        int currChunkCoordY = Mathf.RoundToInt(_viewerPosition.y / _chunkSize);

        for (int yOffset = -_chunksVisibleInViewDist; yOffset <= _chunksVisibleInViewDist; yOffset++)
        {
            for (int xOffset = -_chunksVisibleInViewDist; xOffset <= _chunksVisibleInViewDist; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currChunkCoordX + xOffset, currChunkCoordY + yOffset);

                if (_terrainChunkDict.ContainsKey(viewedChunkCoord))
                {
                    _terrainChunkDict[viewedChunkCoord].UpdateTerrainChunk();
                    if (_terrainChunkDict[viewedChunkCoord].IsVisible())
                    {
                        _terrainChunksVisibleLastUpdate.Add(_terrainChunkDict[viewedChunkCoord]);
                    }
                }
                else
                {
                    _terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, _chunkSize, _detailLevels, transform, _mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        private GameObject _meshObject;
        private Vector2 _position;
        private Bounds _bounds;

        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        private LODInfo[] _detailLevels;
        private LODMesh[] _lodMeshes;
        
        private MapData _mapData;
        private bool _mapDataReceived;
        
        private int _previousLODIndex = -1;
        
        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            _detailLevels = detailLevels;
            
            _position = coord * size;
            _bounds = new Bounds(_position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(_position.x, 0, _position.y);

            _meshObject = new GameObject("TerrainChunk");
            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshFilter = _meshObject.AddComponent<MeshFilter>();
            _meshRenderer.material = material;
            
            _meshObject.transform.position = positionV3;
            _meshObject.transform.parent = parent;
            SetVisible(false);
            
            _lodMeshes = new LODMesh[_detailLevels.Length];
            for (int i = 0; i < _detailLevels.Length; i++)
            {
                _lodMeshes[i] = new LODMesh(_detailLevels[i]._lod, UpdateTerrainChunk);
            }
            
            _mapGenerator.RequestMapData(_position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            _mapData = mapData;
            _mapDataReceived = true;
            
            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData._colorMap, MapGenerator.MapChunkSize, MapGenerator.MapChunkSize);
            _meshRenderer.material.mainTexture = texture;
            
            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (!_mapDataReceived)
            {
                return;
            }
            
            // Show/Hide this chunk if in range
            float viewerDistFromNearestEdgeSquared = _bounds.SqrDistance(_viewerPosition);
            float maxViewDistSquared = _maxViewDist * _maxViewDist;
            bool visible = viewerDistFromNearestEdgeSquared <= maxViewDistSquared;

            if (visible)
            {
                int lodIndex = 0;

                for (int i = 0; i < _detailLevels.Length - 1; i++)
                {
                    float visibleDistThresholdSquared = _detailLevels[i]._visibleDistThreshold * _detailLevels[i]._visibleDistThreshold;
                    if (viewerDistFromNearestEdgeSquared > visibleDistThresholdSquared)
                    {
                        lodIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if (lodIndex != _previousLODIndex)
                {
                    LODMesh lodMesh = _lodMeshes[lodIndex];
                    if (lodMesh._hasMesh)
                    {
                        _meshFilter.mesh = lodMesh._mesh;
                        _previousLODIndex = lodIndex;
                    }
                    else if (!lodMesh._hasRequestedMesh)
                    {
                        lodMesh.RequestMesh(_mapData);
                    }
                }
            }
            
            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            _meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return _meshObject.activeSelf;
        }
    }

    public class LODMesh
    {
        public Mesh _mesh;
        public bool _hasRequestedMesh;
        public bool _hasMesh;
        private int _lod;

        private System.Action _updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            _lod = lod;
            _updateCallback = updateCallback;
        }

        private void OnMeshDataReceived(MeshData meshData)
        {
            _mesh = meshData.CreateMesh();
            _hasMesh = true;
            _updateCallback();
        }
        
        public void RequestMesh(MapData mapData)
        {
            _hasRequestedMesh = true;
            _mapGenerator.RequestMeshData(mapData, _lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int _lod;
        public float _visibleDistThreshold; // this LOD visible when viewer is below this distance away
    }
}
