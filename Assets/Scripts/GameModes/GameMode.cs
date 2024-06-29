using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class GameMode : ScriptableObject
{
    [SerializeField] public int miniumPlayerToStart = 4;
    [SerializeField] public float respawnTime = 3f;
    [SerializeField] public float playerStartingPoint = 3f;

    public virtual void Initialize() {}
    public virtual void DeInitialize() { }
    public virtual bool CheckGameIsOver() { return true; }
    protected virtual void OnGamePhaseChanged(LevelStatus status) { }
}