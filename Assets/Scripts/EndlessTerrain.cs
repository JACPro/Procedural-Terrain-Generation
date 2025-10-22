using System;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    private const float _maxViewDist = 450.0f;
    [SerializeField] private Transform _viewer;

    private static Vector2 _viewerPosition;

    private int _chunkSize;
    private int _chunksVisibleInViewDist;

    Dictionary<Vector2, TerrainChunk> _terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> _terrainChunksVisibleLastUpdate = new List<TerrainChunk>();
    
    private void Start()
    {
        _chunkSize = MapGenerator.MapChunkSize - 1;
        _chunksVisibleInViewDist = Mathf.RoundToInt(_maxViewDist / _chunkSize);
    }

    private void Update()
    {
        _viewerPosition = new Vector2(_viewer.position.x, _viewer.position.z);
        UpdateVisibleChunks();
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
                    _terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, _chunkSize, transform));
                }
            }
        }
    }

    public class TerrainChunk
    {
        private GameObject _meshObject;
        private Vector2 _position;
        private Bounds _bounds;
        
        public TerrainChunk(Vector2 coord, int size, Transform parent)
        {
            _position = coord * size;
            _bounds = new Bounds(_position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(_position.x, 0, _position.y);
            
            _meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _meshObject.transform.position = positionV3;
            _meshObject.transform.localScale = Vector3.one * size / 10.0f; // size of primitive plane is 10.0 units by default
            _meshObject.transform.parent = parent;
            SetVisible(false);
        }

        public void UpdateTerrainChunk()
        {
            // Show/Hide this chunk if in range
            float viewerDistFromNearestEdgeSquared = _bounds.SqrDistance(_viewerPosition);
            float maxViewDistSquared = _maxViewDist * _maxViewDist;
            bool visible = viewerDistFromNearestEdgeSquared <= maxViewDistSquared;
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
}
