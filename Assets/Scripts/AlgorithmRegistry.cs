using UnityEngine;
using System.Collections.Generic;

public class AlgorithmRegistry : MonoBehaviour
{
    public static AlgorithmRegistry Instance { get; private set; }
    public List<string> activeAlgorithms = new List<string>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Register(string name)
    {
        if (!activeAlgorithms.Contains(name))
            activeAlgorithms.Add(name);
    }

    public void Unregister(string name)
    {
        activeAlgorithms.Remove(name);
    }
    public AlgorithmRegistry registry;
    public List<string> GetAlgorithmList()
    {
        if (registry == null || registry.activeAlgorithms == null)
        {
            Debug.LogWarning("AlgoData: No registry found.");
            return new List<string>();
        }
        return new List<string>(registry.activeAlgorithms);
    }
}