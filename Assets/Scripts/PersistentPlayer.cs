using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PersistentPlayer : MonoBehaviour
{
    public static PersistentPlayer Instance;

    public PlayerData playerData;
    public PlayerInput playerInput;

    void Awake()
    {
        if (Instance)
            Destroy(this.gameObject);
        else
            Instance = this;
    }
}
