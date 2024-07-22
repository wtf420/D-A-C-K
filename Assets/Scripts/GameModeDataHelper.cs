using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModeDataHelper : MonoBehaviour
{
    public static GameModeDataHelper Instance;
    public GameModeInfoScriptableObject MapsData;

    void Awake()
    {
        if (Instance)
            Destroy(gameObject);
        else
            Instance = this;
        DontDestroyOnLoad(this);
    }
}
