using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class GameMode : NetworkBehaviour
{
    public static GameMode Instance;
    protected NetworkManager networkManager => NetworkManager.Singleton;
    protected GamePlayManager gamePlayManager => GamePlayManager.Instance;
    protected NetworkPlayersManager networkPlayersManager => NetworkPlayersManager.Instance;
    protected LevelManager levelManager => LevelManager.Instance;

    [SerializeField] public ulong miniumPlayerToStart = 4;
    [SerializeField] public float respawnTime = 3f;
    [SerializeField] public float playerStartingPoint = 3f;

    public UnityEvent<ulong> OnPlayerSpawnEvent, OnPlayerDeathEvent;
    public UnityEvent<ulong, ulong> OnPlayerKillEvent;

    public virtual void Initialize() { }
    public virtual void Deinitialize() { }
    public virtual bool CheckGameIsOver() { return true; }

    public virtual void OnGamePhaseChanged(short previousValue, short newValue) { }
    public virtual void OnNewPlayerJoined(ulong clientId) { }

    public virtual IEnumerator GameLoop()
    {
        yield return null;
    }

    [Rpc(SendTo.Server)]
    public virtual void RespawnCharacterRpc(ulong clientId, float timeToSpawn, SpawnOptions options) { }
    [Rpc(SendTo.Server)]
    public virtual void SpawnCharacterRpc(ulong clientId, SpawnOptions options) { }
    [Rpc(SendTo.Server)]
    public virtual void KillCharacterRpc(ulong clientId, ulong killerId, bool AddToKillFeed = false) { }

    protected virtual void Awake()
    {
        if (Instance) Destroy(Instance.gameObject);
        Instance = this;
    }
}