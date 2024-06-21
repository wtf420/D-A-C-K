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

#region PlayerLevelInfo
[Serializable]
public struct PlayerLevelInfo : INetworkSerializable, IEquatable<PlayerLevelInfo>
{
    public ulong clientId;
    public FixedString32Bytes playerName;
    public FixedString32Bytes playerColor;
    public NetworkBehaviourReference networkPlayer;
    public NetworkBehaviourReference character;
    public float playerScore;
    public short playerStatus;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out clientId);
            reader.ReadValueSafe(out playerName);
            reader.ReadValueSafe(out playerColor);
            reader.ReadValueSafe(out networkPlayer);
            reader.ReadValueSafe(out character);
            reader.ReadValueSafe(out playerScore);
            reader.ReadValueSafe(out playerStatus);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(clientId);
            writer.WriteValueSafe(playerName);
            writer.WriteValueSafe(playerColor);
            writer.WriteValueSafe(networkPlayer);
            writer.WriteValueSafe(character);
            writer.WriteValueSafe(playerScore);
            writer.WriteValueSafe(playerStatus);
        }
    }

    public bool Equals(PlayerLevelInfo other)
    {
        return clientId == other.clientId;
    }
}
#endregion

// this will only update on the server
public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance;
    public NetworkManager networkManager;

    public NetworkPlayer networkPlayerPrefab;
    public ThirdPersonController characterPlayerPrefab;
    public ThirdPersonSpectatorController spectatorPlayerPrefab;

    [SerializeField] LevelManagerUI levelManagerUI;
    [SerializeField] public ScoreBoard scoreBoard;

    [SerializeField] public List<GameObject> spawnPointList;

    [SerializeField] public int miniumPlayerToStart = 4;
    [SerializeField] public float playerStartingPoint = 0;
    [SerializeField] public Weapon testWeapon;

    [SerializeField] public LevelStatus currentLevelStatus = LevelStatus.None;
    public NetworkVariable<short> currentNetworkLevelStatus = new NetworkVariable<short>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<PlayerLevelInfo> winner;
    public NetworkVariable<bool> GameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkList<PlayerLevelInfo> PlayerLevelInfoNetworkList;
    // Crazy idea saved for later
    // private bool playerLevelInfoNormalListRefresh = false;
    // private List<PlayerLevelInfo> playerLevelInfoNormalList;
    // public List<PlayerLevelInfo> PlayerLevelInfoNormalList
    // {
    //     get {
    //         if (!playerLevelInfoNormalListRefresh)
    //         {
    //             playerLevelInfoNormalListRefresh = true;
    //             playerLevelInfoNormalList = PlayerNetworkListToNormalList();
    //             return playerLevelInfoNormalList;
    //         } else
    //         {
    //             return playerLevelInfoNormalList;
    //         }
    //     }
    //     set {
    //         playerLevelInfoNormalList = value;
    //     }
    // }
    public UnityEvent<ulong> OnPlayerJoinedEvent, OnPlayerLeaveEvent, OnPlayerSpawnEvent, OnPlayerDeathEvent;

    #region Mono & NetworkBehaviour
    void Awake()
    {
        if (Instance) Destroy(Instance.gameObject);
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        networkManager = NetworkManager.Singleton;
        PlayerLevelInfoNetworkList = new NetworkList<PlayerLevelInfo>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) scoreBoard.Show(); 
        else if (Input.GetKeyUp(KeyCode.Tab)) scoreBoard.Hide();
        if (!IsServer) return;
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("OnNetworkSpawn");
        networkManager.OnClientDisconnectCallback += OnDisconnectedCallback;
        networkManager.OnClientConnectedCallback += OnConnectedCallback;
        networkManager.SceneManager.OnLoadComplete += OnSceneLoadComplete;
        networkManager.SceneManager.OnSynchronizeComplete += SyncDataAsLateJoiner;
        PlayerLevelInfoNetworkList.OnListChanged += OnPlayerLevelInfoNetworkListChanged;
        if (GameStarted.Value) currentNetworkLevelStatus.OnValueChanged += OnGamePhaseChanged;

        if (IsServer) StartCoroutine(GameLoop());
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log("OnNetworkDespawn");
        base.OnNetworkDespawn();
        networkManager.OnClientDisconnectCallback -= OnDisconnectedCallback;
        networkManager.OnClientConnectedCallback -= OnConnectedCallback;
        networkManager.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
        networkManager.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
        PlayerLevelInfoNetworkList.OnListChanged -= OnPlayerLevelInfoNetworkListChanged;
        currentNetworkLevelStatus.OnValueChanged -= OnGamePhaseChanged;
    }

    private void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        Debug.Log("OnSceneLoadComplete");
        if (clientId != NetworkManager.LocalClientId) return;
        if (!GetPlayerLevelInfoFromNetworkList(clientId).networkPlayer.TryGet(out NetworkPlayer _)) SpawnPlayerObjectRpc(clientId, PersistentPlayer.Instance.playerData);
        networkManager.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
    }

    private void OnConnectedCallback(ulong clientId)
    {
        Debug.Log("OnConnectedCallback");
        OnPlayerJoinedEvent?.Invoke(clientId);
        if (clientId != NetworkManager.LocalClientId) return;
        if (!GetPlayerLevelInfoFromNetworkList(clientId).networkPlayer.TryGet(out NetworkPlayer _)) SpawnPlayerObjectRpc(clientId, PersistentPlayer.Instance.playerData);
    }

    private void OnDisconnectedCallback(ulong clientId)
    {
        if (IsServer && PlayerNetworkListToNormalList().Any(x => x.clientId == clientId) && clientId != networkManager.LocalClientId)
        {
            RemovePlayer(clientId);
        }
        if (clientId == networkManager.LocalClientId)
        {
            SceneManager.LoadScene("LobbyScene");
        }
        OnPlayerLeaveEvent?.Invoke(clientId);
    }

    void SyncDataAsLateJoiner(ulong clientId)
    {
        if (clientId != NetworkManager.LocalClientId) return;
        // manually refresh
        OnPlayerLevelInfoNetworkListChanged(default);
        OnGamePhaseChanged(default, default);
        Debug.Log("SyncDataAsLateJoiner");
    }
    #endregion

    #region Player Management
    private void OnPlayerLevelInfoNetworkListChanged(NetworkListEvent<PlayerLevelInfo> changeEvent)
    {
        PlayerNetworkListToNormalList().Clear();
        foreach (PlayerLevelInfo info in PlayerLevelInfoNetworkList)
        {
            PlayerNetworkListToNormalList().Add(info);
        }
    }

    [Rpc(SendTo.Server)]
    void SpawnPlayerObjectRpc(ulong clientId, PlayerData playerData)
    {
        Debug.Log("SpawnPlayerObjectRpc: " + playerData.PlayerName + " | " + playerData.PlayerColor);
        NetworkPlayer player = Instantiate(networkPlayerPrefab, Vector3.zero, Quaternion.identity, null);
        player.playerData = playerData;
        player.NetworkObject.SpawnAsPlayerObject(clientId);
        StartCoroutine(AddNewPlayer(clientId));
    }

    IEnumerator AddNewPlayer(ulong clientId)
    {
        NetworkPlayer player = null;
        yield return new WaitUntil(() => networkManager.SpawnManager.GetPlayerNetworkObject(clientId));
        player = networkManager.SpawnManager.GetPlayerNetworkObject(clientId).GetComponent<NetworkPlayer>();
        if (player)
        {
            AddPlayerLevelInfo(player);
        }
        SpawnCharacterRpc(clientId);
    }

    public void AddPlayerLevelInfo(NetworkPlayer player)
    {
        PlayerLevelInfo playerLevelInfo = new PlayerLevelInfo
        {
            clientId = player.OwnerClientId,
            playerName = player.playerName.Value,
            playerColor = player.playerColor.Value,
            networkPlayer = player,
            character = default,
            playerScore = playerStartingPoint,
            playerStatus = (short)PlayerStatus.Spectating,
        };
        PlayerLevelInfoNetworkList.Add(playerLevelInfo);
        Debug.Log("PlayerData: " + player.playerName.Value + " | " + player.playerColor.Value);
    }

    public void RemovePlayer(ulong clientId)
    {
        int index = GetPlayerIndexFromNetworkList(clientId);
        if (index != -1)
            PlayerLevelInfoNetworkList.RemoveAt(index);
    }

    public void UpdateNetworkList(PlayerLevelInfo info)
    {
        int index = GetPlayerIndexFromNetworkList(info.clientId);
        if (index != -1)
            PlayerLevelInfoNetworkList[index] = info;
    }

    public int GetPlayerIndexFromNetworkList(ulong clientId)
    {
        for (int i = 0; i < PlayerLevelInfoNetworkList.Count; i++)
        {
            if (clientId == PlayerLevelInfoNetworkList[i].clientId)
                return i;
        }
        return -1;
    }

    public PlayerLevelInfo GetPlayerLevelInfoFromNetworkList(ulong clientId)
    {
        for (int i = 0; i < PlayerLevelInfoNetworkList.Count; i++)
        {
            if (clientId == PlayerLevelInfoNetworkList[i].clientId)
                return PlayerLevelInfoNetworkList[i];
        }
        return default;
    }

    public List<PlayerLevelInfo> PlayerNetworkListToNormalList()
    {
        List<PlayerLevelInfo> playerLevelInfos = new List<PlayerLevelInfo>();
        for (int i = 0; i < PlayerLevelInfoNetworkList.Count; i++)
        {
            playerLevelInfos.Add(PlayerLevelInfoNetworkList[i]);
        }
        return playerLevelInfos;
    }
    #endregion

    #region Gameloop & Gamephases
    IEnumerator GameLoop()
    {
        yield return new WaitUntil(() => networkManager.IsServer || networkManager.IsHost);
        GameStarted.Value = true;
        currentNetworkLevelStatus.Value = (short)LevelStatus.None;
        currentNetworkLevelStatus.OnValueChanged += OnGamePhaseChanged;

        Debug.Log("Awaiting Players...");
        // currentNetworkLevelStatus.Value = (short)LevelStatus.WaitingForPlayers;
        // yield return new WaitUntil(() => PlayerLevelInfoNetworkList.Count >= miniumPlayerToStart);
        yield return StartCoroutine(WaitingForPlayers());

        Debug.Log("Begining game!");
        yield return StartCoroutine(GameInProgress());

        Debug.Log("Game Over!");
        yield return StartCoroutine(GameOver());

        _ = LobbyManager.Instance.ExitLobby();
        networkManager.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
        networkManager.Shutdown();
    }

    private void OnGamePhaseChanged(short previousValue, short newValue)
    {
        currentLevelStatus = (LevelStatus)currentNetworkLevelStatus.Value;
        Debug.Log("OnGamePhaseChanged");

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
                    if (GetPlayerLevelInfoFromNetworkList(networkManager.LocalClientId).playerStatus != (short)PlayerStatus.Spectating)
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
        OnPlayerDeathEvent.AddListener(CustomOnPlayerDeathLogicWaitingForPlayers);
        currentNetworkLevelStatus.Value = (short)LevelStatus.WaitingForPlayers;
        yield return new WaitUntil(() => PlayerLevelInfoNetworkList.Count >= miniumPlayerToStart);
        OnPlayerDeathEvent.RemoveListener(CustomOnPlayerDeathLogicWaitingForPlayers);
    }

    protected virtual IEnumerator GameInProgress()
    {
        OnPlayerDeathEvent.AddListener(CustomOnPlayerDeathLogicProgress);
        OnPlayerSpawnEvent.AddListener(CustomOnPlayerSpawnLogicProgress);
        for (int i = 0; i < PlayerLevelInfoNetworkList.Count; i++)
        {
            PlayerLevelInfo info = PlayerLevelInfoNetworkList[i];
            Debug.Log("PlayerData: " + info.playerName + " | " + info.playerColor);
            yield return 0; //wait for next frame
            RespawnCharacterRpc(info.clientId, 0f);
        }
        currentNetworkLevelStatus.Value = (short)LevelStatus.InProgress;
        yield return new WaitUntil(() => CheckGameIsOver());
        OnPlayerDeathEvent.RemoveListener(CustomOnPlayerDeathLogicProgress);
        OnPlayerSpawnEvent.RemoveListener(CustomOnPlayerSpawnLogicProgress);
    }

    protected virtual IEnumerator GameOver()
    {
        for (int i = 0; i < PlayerLevelInfoNetworkList.Count; i++)
        {
            KillCharacterRpc(PlayerLevelInfoNetworkList[i].clientId);
        }
        currentNetworkLevelStatus.Value = (short)LevelStatus.Done;
        yield return new WaitForSeconds(5f);
    }

    public bool CheckGameIsOver()
    {
        int currentAlivePlayer = 0;
        int currentSpectatingPlayers = 0;
        winner.Value = PlayerLevelInfoNetworkList[0];
        for (int i = 0; i < PlayerLevelInfoNetworkList.Count; i++)
        {
            PlayerLevelInfo info = PlayerLevelInfoNetworkList[i];
            if (info.playerScore > 0 && info.playerStatus != (short)PlayerStatus.Spectating)
            {
                currentAlivePlayer++;
                winner.Value = info;
            } else
            {
                currentSpectatingPlayers++;
            }
            if (currentAlivePlayer > 1) return false;
        }
        if (currentSpectatingPlayers == PlayerLevelInfoNetworkList.Count)
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
    public void SpawnCharacterRpc(ulong clientId)
    {
        PlayerLevelInfo info = GetPlayerLevelInfoFromNetworkList(clientId);
        Transform spawn = spawnPointList[UnityEngine.Random.Range(0, spawnPointList.Count -1)].transform;
        ThirdPersonController character = Instantiate(characterPlayerPrefab, spawn.position, spawn.rotation, null);
        character.controlPlayerNetworkBehaviourReference.Value = info.networkPlayer;
        character.NetworkObject.SpawnWithOwnership(info.clientId, true);

        info.character = character;
        info.playerStatus = (short)PlayerStatus.Alive;
        UpdateNetworkList(info);
        OnPlayerSpawn(clientId);
    }

    [Rpc(SendTo.Server)]
    public void SpawnSpectatorRpc(ulong clientId)
    {
        PlayerLevelInfo info = GetPlayerLevelInfoFromNetworkList(clientId);
        ThirdPersonSpectatorController character = Instantiate(spectatorPlayerPrefab, null);
        character.controlPlayerNetworkBehaviourReference.Value = info.networkPlayer;
        character.NetworkObject.SpawnWithOwnership(info.clientId, true);

        info.character = character;
        info.playerStatus = (short)PlayerStatus.Spectating;
        UpdateNetworkList(info);
        OnPlayerSpawn(clientId);
    }

    [Rpc(SendTo.Server)]
    public void KillCharacterRpc(ulong clientId)
    {
        PlayerLevelInfo info = GetPlayerLevelInfoFromNetworkList(clientId);
        if (info.character.TryGet(out ThirdPersonController character))
        {
            info.character = default;
            info.playerStatus = (short)PlayerStatus.Dead;
            UpdateNetworkList(info);
            OnPlayerDeath(clientId);
            StartCoroutine(DestroyAndDespawnAfter(character, 3f));
        }
    }

    [Rpc(SendTo.Server)]
    public void RespawnCharacterRpc(ulong clientId, float respawnTime = 1f, bool respawnAsSpectator = false, bool immediatelyDestroy = true)
    {
        PlayerLevelInfo info = GetPlayerLevelInfoFromNetworkList(clientId);
        if (info.character.TryGet(out Playable character) && immediatelyDestroy)
        {
            if (character.NetworkObject.IsSpawned)
                character.NetworkObject.Despawn();
            else
                Destroy(character.gameObject);
            info.character = default;
        }
        info.playerStatus = (short)PlayerStatus.Respawning;
        UpdateNetworkList(info);
        StartCoroutine(RespawnCharacter(info.clientId, respawnTime, respawnAsSpectator));
    }

    IEnumerator DestroyAndDespawnAfter(Playable character, float time)
    {
        yield return new WaitForSeconds(time);
        if (character == null) yield break;
        if (character.NetworkObject.IsSpawned)
            character.NetworkObject.Despawn();
        else
            Destroy(character.gameObject);
    } 

    IEnumerator RespawnCharacter(ulong clientId, float respawnTime = 1f, bool respawnAsSpectator = false)
    {
        yield return new WaitForSeconds(respawnTime);
        if (respawnAsSpectator)
            SpawnSpectatorRpc(clientId);
        else
            SpawnCharacterRpc(clientId);
    }
    #endregion

    private void CustomOnPlayerDeathLogicWaitingForPlayers(ulong clientId)
    {
        RespawnCharacterRpc(clientId, 3f, false, false);
    }

    private void CustomOnPlayerDeathLogicProgress(ulong clientId)
    {
        PlayerLevelInfo info = GetPlayerLevelInfoFromNetworkList(clientId);
        if (currentLevelStatus == LevelStatus.InProgress)
        {
            info.playerScore--;
            if (info.playerScore > 0)
            {
                UpdateNetworkList(info);
                RespawnCharacterRpc(clientId, 3f, false, false);
            }
            else
            {
                UpdateNetworkList(info);
                RespawnCharacterRpc(clientId, 3f, true, false);
            }
        }
        else
        {
            RespawnCharacterRpc(clientId, 3f, true, false);
        }
    }

    private void CustomOnPlayerSpawnLogicProgress(ulong clientId)
    {
        PlayerLevelInfo info = GetPlayerLevelInfoFromNetworkList(clientId);
        if (info.character.TryGet(out ThirdPersonController character))
        {
            Weapon weapon = Instantiate(testWeapon);
            weapon.NetworkObject.Spawn(true);
            weapon.wielderNetworkBehaviourReference.Value = character;
            character.weaponNetworkBehaviourReference.Value = weapon;
        }
    }
}