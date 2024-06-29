using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// this will only update on the server
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set;}

    [SerializeField] public List<Transform> SpawnPoints;

    void Awake()
    {
        if (Instance) Destroy(Instance.gameObject);
        Instance = this;
    }

    public Transform GetRandomSpawnPoint()
    {
        int index = UnityEngine.Random.Range(0, SpawnPoints.Count - 1);
        return SpawnPoints[index];
    }
}