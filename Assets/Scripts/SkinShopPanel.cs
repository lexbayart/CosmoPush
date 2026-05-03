using UnityEngine;
using System.Collections.Generic;

public class SkinShopPanel : MonoBehaviour
{
    // Singleton pattern for easy access
    public static SkinShopPanel Instance { get; private set; }

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
        Debug.Log("SkinShopPanel initialized");
    }
}
