using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public string PlayerName;
    public Color PlayerColor;

    public void Start()
    {
        DontDestroyOnLoad(this);
    }
}
