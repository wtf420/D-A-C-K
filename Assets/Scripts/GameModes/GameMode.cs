using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class GameMode : ScriptableObject
{
    [SerializeField] public int miniumPlayerToStart = 4;
    [SerializeField] public float respawnTime = 3f;

    protected virtual void SyncDataAsLateJoiner(ulong clientId) { }
    protected virtual void OnGamePhaseChanged(short previousValue, short newValue) { }
}