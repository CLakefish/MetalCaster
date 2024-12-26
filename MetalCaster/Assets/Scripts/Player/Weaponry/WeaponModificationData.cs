using System.Collections.Generic;
using UnityEngine;

public class WeaponModificationData
{
    private Dictionary<string, object> data = new();
    public Dictionary<string, object> Data { 
        get => data; 
        set => data = value; 
    }

    public bool Contains(string name) {
        return Data.ContainsKey(name);
    }

    public T Get<T>(string name)
    {
        if (!Data.TryGetValue(name, out object obj)) {
            Debug.LogWarning("Cannot get data from name: " + name);
            return default;
        }

        return (T)obj;
    }

    public void Set<T>(T type, string name)
    {
        if (Data.ContainsKey(name)) {
            Data[name] = type;
            return;
        }
        else {
            Data.Add(name, type);
        }
    }
}
