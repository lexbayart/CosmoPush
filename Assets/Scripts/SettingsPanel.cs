using UnityEngine;
using System.Collections.Generic;

public class SettingsPanel : MonoBehaviour
{
    // Singleton pattern for easy access
    public static SettingsPanel Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Initialize()
    {
        Debug.Log("SettingsPanel initialized");
    }
}
