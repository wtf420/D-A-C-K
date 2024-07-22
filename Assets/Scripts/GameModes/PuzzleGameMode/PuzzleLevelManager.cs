using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PuzzleLevelManager : LevelManager
{
    protected override void Awake()
    {
        base.Awake();
        SpawnPoints[1].Activated = false;
        SpawnPoints[0].Activated = true;
    }

    public void OnCheckPointReached()
    {
        SpawnPoints[0].Activated = false;
        SpawnPoints[1].Activated = true;
    }
}