using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

// this will only update on the server
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set;}

    [SerializeField] public List<SpawnPoint> SpawnPoints;

    protected virtual void Awake()
    {
        if (Instance) Destroy(Instance.gameObject);
        Instance = this;
    }

    public virtual Transform GetRandomSpawnPoint()
    {
        List<SpawnPoint> availableSpawnPoints = SpawnPoints.Where(x => x.Activated == true).ToList();
        int index = UnityEngine.Random.Range(0, availableSpawnPoints.Count - 1);
        return availableSpawnPoints[index].transform;
    }
}