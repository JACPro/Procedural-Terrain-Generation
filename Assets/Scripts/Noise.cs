using UnityEngine;

public static class Noise
{
    public enum NormalizeMode { Local, Global };
    
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed,
        float scale, int octaves, float persistence, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random rng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        
        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;
        
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = rng.Next(-100000, 100000) + offset.x;
            float offsetY = rng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
            
            maxPossibleHeight += amplitude;
            amplitude *= persistence;
        }
        
        if (scale <= 0)
        {
            scale = 0.0001f;
        }
        
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;
        
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;
        
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;
                
                for (int oct = 0; oct < octaves; oct++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[oct].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[oct].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; // range -1.0 to 1.0
                    noiseHeight += perlinValue * amplitude;
                    
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }

                if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }
                
                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else
                {
                    // need to multiply raw Perlin value by max possible height, but we shifted the noise map value * 2 -1 to get the range -1.0 to 1.0
                    // so we first revert this operation
                    float rawPerlinValue = (noiseMap[x, y] + 1) / 2f;
                    float normalizedHeight = rawPerlinValue / maxPossibleHeight;
                    // as we will likely never come close to the maximum possible height, all our normalized values are now very low
                    // to account for this, we will multiply the height by some constant factor
                    float heightMultiplier = 2.0f;
                    normalizedHeight *= heightMultiplier;
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }

        return noiseMap;
    }
}
