using UnityEngine;

public class FalloffGenerator
{
    public static float[,] GenerateFalloffMap(int size)
    {
        float [,] map = new float[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                // normalize to values between -1.0 and 1.0
                float x = i / (float)size * 2 - 1;  
                float y = j / (float)size * 2 - 1;
                
                // absolute value now represents proximity to the edge, i.e. an abs of 1 is on an edge, but 0 is dead centre
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i, j] = EvaluateFromCurve(value);
            }
        }
        
        return map;
    }

    private static float EvaluateFromCurve(float value)
    {
        // using the formula f(x) = x^a / x^a + (b - bx)^a -- this results in an ease-in-out curve
        float a = 3;
        float b = 2.2f;
        
        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
