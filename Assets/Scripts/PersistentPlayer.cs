using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PersistentPlayer : MonoBehaviour
{
    public static PersistentPlayer Instance;

    public PlayerData playerData;
    public PlayerInput playerInput;

    public bool initialized = false;

    void Awake()
    {
        if (Instance)
            Destroy(gameObject);
        else
            Instance = this;
        DontDestroyOnLoad(this);
    }
}
