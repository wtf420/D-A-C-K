using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum LevelStatus : short
{
    None = 0,
    WaitingForPlayers = 1,
    CountDown = 2,
    InProgress = 3,
    Done = 4
}

public enum PlayerStatus : short
{
    Spectating,
    Alive,
    Dead,
    Respawning,
}

public struct SpawnOptions : INetworkSerializable, IEquatable<SpawnOptions>
{
    public Vector3 position;
    public Quaternion rotation;
    public bool spawnAsSpectator;

    public SpawnOptions(Transform transform, bool spawnAsSpectator = false)
    {
        position = transform.position;
        rotation = transform.rotation;
        this.spawnAsSpectator = spawnAsSpectator;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out position);
            reader.ReadValueSafe(out rotation);
            reader.ReadValueSafe(out spawnAsSpectator);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(position);
            writer.WriteValueSafe(rotation);
            writer.WriteValueSafe(spawnAsSpectator);
        }
    }

    public bool Equals(SpawnOptions other)
    {
        return false;
    }
}

// this will only update on the server
public class GamePlayManager : NetworkBehaviour
{
    public static GamePlayManager Instance;
    public NetworkManager networkManager => NetworkManager.Singleton;
    public NetworkPlayersManager networkPlayersManager => NetworkPlayersManager.Instance;
    public LevelManager levelManager => LevelManager.Instance;

    public ThirdPersonController characterPlayerPrefab;
    public ThirdPersonSpectatorController spectatorPlayerPrefab;

    [SerializeField] LevelManagerUI levelManagerUI;
    [SerializeField] ScoreBoard scoreBoard;
    [SerializeField] PauseMenuScreen pauseMenuScreen;
    [SerializeField] KillFeed killFeed;
    [SerializeField] GameMode gameMode;

    [SerializeField] public int miniumPlayerToStart = 4;
    [SerializeField] public float playerStartingPoint = 0;
    [SerializeField] public float respawnTime = 3f;

    [SerializeField] public LevelStatus currentLevelStatus = LevelStatus.None;
    public NetworkVariable<short> currentNetworkLevelStatus = new NetworkVariable<short>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<NetworkPlayerInfo> winner;
    public NetworkVariable<bool> GameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public UnityEvent<ulong> OnPlayerSpawnEvent, OnPlayerDeathEvent;
    public UnityEvent<LevelStatus> OnLevelStatusChangedEvent;

    private NetworkList<NetworkPlayerInfo> NetworkPlayerInfoNetworkList => networkPlayersManager.NetworkPlayerInfoNetworkList;

    #region Mono & NetworkBehaviour
    void Awake()
    {
        if (Instance) Destroy(Instance.gameObject);
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            if (pauseMenuScreen.isShowing) pauseMenuScreen.Hide();
            else pauseMenuScreen.Show();

        if (Input.GetKeyDown(KeyCode.Tab)) scoreBoard.Show();
        else if (Input.GetKeyUp(KeyCode.Tab)) scoreBoard.Hide();

        if (!IsServer) return;
    }

    public override void OnNetworkSpawn()
    {
        networkManager.SceneManager.OnSynchronizeComplete += SyncDataAsLateJoiner;
        if (GameStarted.Value) currentNetworkLevelStatus.OnValueChanged += OnGamePhaseChanged;

        if (IsServer || IsHost)
        {
            StartCoroutine(GameLoop());
            networkPlayersManager.OnPlayerJoinedEvent.AddListener(OnNewPlayerJoined);
        }
    }

    void OnNewPlayerJoined(ulong clientId)
    {
        SpawnCharacterRpc(clientId, new SpawnOptions(LevelManager.Instance.GetRandomSpawnPoint()));
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        networkManager.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
        currentNetworkLevelStatus.OnValueChanged -= OnGamePhaseChanged;

        OnPlayerSpawnEvent.RemoveAllListeners();
        OnPlayerDeathEvent.RemoveAllListeners();

        LobbyManager.Instance.ExitGame();
    }

    void SyncDataAsLateJoiner(ulong clientId)
    {
        if (clientId != NetworkManager.LocalClientId) return;
        OnGamePhaseChanged(default, default);
    }
    #endregion

    #region Gameloop & Gamephases
    public void InitializeGameMode(GameMode gameMode)
    {
        miniumPlayerToStart = gameMode.miniumPlayerToStart;
        respawnTime = gameMode.respawnTime;
        playerStartingPoint = gameMode.playerStartingPoint;
        gameMode.Initialize();
    }

    public void DeInitializeGameMode(GameMode gameMode)
    {
        gameMode.DeInitialize();
    }

    IEnumerator GameLoop()
    {
        InitializeGameMode(gameMode);
        yield return new WaitUntil(() => networkManager.IsServer || networkManager.IsHost);
        GameStarted.Value = true;
        currentNetworkLevelStatus.Value = (short)LevelStatus.None;
        currentNetworkLevelStatus.OnValueChanged += OnGamePhaseChanged;

        Debug.Log("Awaiting Players...");
        // currentNetworkLevelStatus.Value = (short)LevelStatus.WaitingForPlayers;
        // yield return new WaitUntil(() => NetworkPlayerInfoNetworkList.Count >= miniumPlayerToStart);
        yield return StartCoroutine(WaitingForPlayers());

        Debug.Log("Begining game!");
        yield return StartCoroutine(GameInProgress());

        Debug.Log("Game Over!");
        yield return StartCoroutine(GameOver());

        StopAllCoroutines();
        DeInitializeGameMode(gameMode);
        LobbyManager.Instance.ExitGame();
    }

    private void OnGamePhaseChanged(short previousValue, short newValue)
    {
        currentLevelStatus = (LevelStatus)currentNetworkLevelStatus.Value;
        OnLevelStatusChangedEvent.Invoke(currentLevelStatus);
        Debug.Log("OnGamePhaseChanged: " + currentLevelStatus.ToString());

        switch (currentLevelStatus)
        {
            case LevelStatus.None:
                {
                    break;
                }
            case LevelStatus.WaitingForPlayers:
                {
                    levelManagerUI.ShowWaitingForPlayersScreen();
                    break;
                }
            case LevelStatus.CountDown:
                {
                    break;
                }
            case LevelStatus.InProgress:
                {
                    if (NetworkPlayersManager.Instance.GetNetworkPlayerInfoFromNetworkList(networkManager.LocalClientId).playerStatus != (short)PlayerStatus.Spectating)
                        levelManagerUI.ShowGameInProgressScreen();
                    else
                        levelManagerUI.HideAllMenu();
                    break;
                }
            case LevelStatus.Done:
                {
                    levelManagerUI.ShowGameOverScreen();
                    break;
                }
        }
    }

    // this thing is cursed, do not touch yet
    protected virtual IEnumerator WaitingForPlayers()
    {
        currentNetworkLevelStatus.Value = (short)LevelStatus.WaitingForPlayers;
        yield return new WaitUntil(() => NetworkPlayerInfoNetworkList.Count >= miniumPlayerToStart);
    }

    protected virtual IEnumerator GameInProgress()
    {
        if (killFeed.isActiveAndEnabled)
        {
            ClearKillFeedRpc();
        }

        currentNetworkLevelStatus.Value = (short)LevelStatus.InProgress;
        yield return new WaitUntil(() => CheckGameIsOver());
    }

    protected virtual IEnumerator GameOver()
    {
        for (int i = 0; i < NetworkPlayerInfoNetworkList.Count; i++)
        {
            KillCharacterRpc(NetworkPlayerInfoNetworkList[i].clientId);
        }
        currentNetworkLevelStatus.Value = (short)LevelStatus.Done;
        yield return new WaitForSeconds(5f);
    }

    public bool CheckGameIsOver()
    {
        int currentAlivePlayer = 0;
        int currentSpectatingPlayers = 0;
        winner.Value = NetworkPlayerInfoNetworkList[0];
        for (int i = 0; i < NetworkPlayerInfoNetworkList.Count; i++)
        {
            NetworkPlayerInfo info = NetworkPlayerInfoNetworkList[i];
            if (info.playerScore > 0 && info.playerStatus != (short)PlayerStatus.Spectating)
            {
                currentAlivePlayer++;
                winner.Value = info;
            }
            else
            {
                currentSpectatingPlayers++;
            }
            if (currentAlivePlayer > 1) return false;
        }
        if (currentSpectatingPlayers == NetworkPlayerInfoNetworkList.Count)
        {
            //special case where everybody is spectating
            return false;
        }
        if (currentAlivePlayer == 1) return true;
        return false;
    }
    #endregion

    #region Gameplay Management
    public void OnPlayerSpawn(ulong clientId)
    {
        OnPlayerSpawnEvent?.Invoke(clientId);
    }

    public void OnPlayerDeath(ulong clientId)
    {
        OnPlayerDeathEvent?.Invoke(clientId);
    }

    [Rpc(SendTo.Server)]
    public void RespawnCharacterRpc(ulong clientId, float timeToSpawn, SpawnOptions options)
    {
        Debug.Log("Respawn");
        NetworkPlayerInfo info = NetworkPlayersManager.Instance.GetNetworkPlayerInfoFromNetworkList(clientId);
        if (info.character.TryGet(out Playable character))
        {
            info.character = default;
            if (character.NetworkObject.IsSpawned)
                character.NetworkObject.Despawn(true);
            else
                Destroy(character.gameObject);
        }
        NetworkPlayersManager.Instance.UpdateNetworkList(info);
        StartCoroutine(RespawnCharacter(info.clientId, timeToSpawn, options));
    }

    IEnumerator RespawnCharacter(ulong clientId, float timeToSpawn, SpawnOptions options)
    {
        Debug.Log("Respawning");
        NetworkPlayerInfo info = NetworkPlayersManager.Instance.GetNetworkPlayerInfoFromNetworkList(clientId);
        info.playerStatus = (short)PlayerStatus.Respawning;
        NetworkPlayersManager.Instance.UpdateNetworkList(info);

        yield return new WaitForSeconds(timeToSpawn);
        SpawnCharacterRpc(clientId, options);
    }

    [Rpc(SendTo.Server)]
    public void SpawnCharacterRpc(ulong clientId, SpawnOptions options)
    {
        Debug.Log("Spawn: " + options.spawnAsSpectator);
        NetworkPlayerInfo info = NetworkPlayersManager.Instance.GetNetworkPlayerInfoFromNetworkList(clientId);
        if (info.character.TryGet(out Playable character))
        {
            if (character.NetworkObject.IsSpawned)
                character.NetworkObject.Despawn(true);
            else
                Destroy(character.gameObject);
        }
        if (options.spawnAsSpectator)
        {
            ThirdPersonSpectatorController spawnCharacter = Instantiate(spectatorPlayerPrefab, null);
            spawnCharacter.controlPlayerNetworkBehaviourReference.Value = info.networkPlayer;
            spawnCharacter.NetworkObject.SpawnWithOwnership(info.clientId, true);

            info.character = spawnCharacter;
            info.playerStatus = (short)PlayerStatus.Spectating;
        }
        else
        {
            ThirdPersonController spawnCharacter = Instantiate(characterPlayerPrefab, null);
            spawnCharacter.controlPlayerNetworkBehaviourReference.Value = info.networkPlayer;

            spawnCharacter.NetworkObject.SpawnWithOwnership(info.clientId, true);
            spawnCharacter.InitialzeRpc(options.position, options.rotation);

            info.character = spawnCharacter;
            info.playerStatus = (short)PlayerStatus.Alive;
        }
        NetworkPlayersManager.Instance.UpdateNetworkList(info);
        OnPlayerSpawn(clientId);
    }

    [Rpc(SendTo.Server)]
    public void KillCharacterRpc(ulong clientId, ulong killerId = default, bool AddToKillFeed = false)
    {
        Debug.Log("Kill");
        NetworkPlayerInfo info = NetworkPlayersManager.Instance.GetNetworkPlayerInfoFromNetworkList(clientId);
        if (info.character.TryGet(out Playable character))
        {
            info.character = default;
            StartCoroutine(DestroyAndDespawnAfter(character, 3f));
        }
        info.playerStatus = (short)PlayerStatus.Dead;

        if (killFeed.isActiveAndEnabled && AddToKillFeed)
        {
            AddToKillFeedRpc(clientId, killerId);
        }
        NetworkPlayersManager.Instance.UpdateNetworkList(info);
        OnPlayerDeath(clientId);
    }

    IEnumerator DestroyAndDespawnAfter(Playable character, float time)
    {
        if (character is ThirdPersonController)
        {
            ((ThirdPersonController)character).KillRpc();
        }
        yield return new WaitForSeconds(time);
        if (character == null) yield break;
        if (character.NetworkObject.IsSpawned)
            character.NetworkObject.Despawn(true);
        else
            Destroy(character.gameObject);
    }
    #endregion

    [Rpc(SendTo.Everyone)]
    void AddToKillFeedRpc(ulong clientId, ulong killerId = default)
    {
        killFeed.AddNewItem(killerId, clientId);
    }

    [Rpc(SendTo.Everyone)]
    void ClearKillFeedRpc()
    {
        killFeed.Clear();
    }
}
