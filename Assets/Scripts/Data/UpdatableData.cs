using System;
using UnityEngine;

public class UpdatableData : ScriptableObject
{
    public event System.Action OnValuesUpdated;
    public bool autoUpdate;

    protected virtual void OnValidate()
    {
        if (autoUpdate)
        {
            NotifyValuesUpdated();
        }
    }

    public void NotifyValuesUpdated()
    {
        OnValuesUpdated?.Invoke();
    }
}
