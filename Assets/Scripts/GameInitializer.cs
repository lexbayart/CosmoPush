using UnityEngine;
using System.Collections.Generic;

public class GameInitializer : MonoBehaviour
{
    // Singleton pattern for easy access
    public static GameInitializer Instance { get; private set; }

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
        Debug.Log("GameInitializer initialized");
    }
}
