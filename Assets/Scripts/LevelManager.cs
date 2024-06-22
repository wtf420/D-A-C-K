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

public struct SpawnOptions : INetworkSerializable, IEquatable<PlayerLevelInfo>
{
    public Vector3 position;
    public Quaternion rotation;
    public bool spawnAsSpectator;

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

    public bool Equals(PlayerLevelInfo other)
    {
        return false;
    }
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
    [SerializeField] ScoreBoard scoreBoard;
    [SerializeField] PauseMenuScreen pauseMenuScreen;

    [SerializeField] public List<GameObject> spawnPointList;

    [SerializeField] public int miniumPlayerToStart = 4;
    [SerializeField] public float playerStartingPoint = 0;
    [SerializeField] public float respawnTime = 3f;
    [SerializeField] public Weapon spawnWeapon;

    [SerializeField] public LevelStatus currentLevelStatus = LevelStatus.None;
    public NetworkVariable<short> currentNetworkLevelStatus = new NetworkVariable<short>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<PlayerLevelInfo> winner;
    public NetworkVariable<bool> GameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkList<PlayerLevelInfo> PlayerLevelInfoNetworkList;

    public UnityEvent<ulong> OnPlayerJoinedEvent, OnPlayerLeaveEvent, OnPlayerSpawnEvent, OnPlayerDeathEvent;
    public UnityEvent<PlayerLevelInfo> OnInfoChangedEvent;

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
        if (Input.GetKeyDown(KeyCode.Escape))
            if (pauseMenuScreen.isShowing) pauseMenuScreen.Hide(); 
            else pauseMenuScreen.Show();

        if (Input.GetKeyDown(KeyCode.Tab)) scoreBoard.Show(); 
        else if (Input.GetKeyUp(KeyCode.Tab)) scoreBoard.Hide();
        
        if (!IsServer) return;
    }

    public override void OnNetworkSpawn()
    {
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
        base.OnNetworkDespawn();
        networkManager.OnClientDisconnectCallback -= OnDisconnectedCallback;
        networkManager.OnClientConnectedCallback -= OnConnectedCallback;
        networkManager.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
        networkManager.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
        PlayerLevelInfoNetworkList.OnListChanged -= OnPlayerLevelInfoNetworkListChanged;
        currentNetworkLevelStatus.OnValueChanged -= OnGamePhaseChanged;

        OnPlayerJoinedEvent.RemoveAllListeners();
        OnPlayerLeaveEvent.RemoveAllListeners();
        OnPlayerSpawnEvent.RemoveAllListeners();
        OnPlayerLeaveEvent.RemoveAllListeners();
        OnInfoChangedEvent.RemoveAllListeners();
    }

    private void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (clientId != NetworkManager.LocalClientId) return;
        if (!GetPlayerLevelInfoFromNetworkList(clientId).networkPlayer.TryGet(out NetworkPlayer _)) SpawnPlayerObjectRpc(clientId, PersistentPlayer.Instance.playerData);
        networkManager.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
    }

    private void OnConnectedCallback(ulong clientId)
    {
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
        Transform spawnpoint = spawnPointList[UnityEngine.Random.Range(0, spawnPointList.Count - 1)].transform;
        SpawnCharacterRpc(clientId, new SpawnOptions()
        {
            position = spawnpoint.transform.position,
            rotation = spawnpoint.transform.rotation,
            spawnAsSpectator = false
        });
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
        {
            PlayerLevelInfoNetworkList[index] = info;
            OnInfoChangedEvent.Invoke(info);
        }
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

        LobbyManager.Instance.ExitGame();
    }

    private void OnGamePhaseChanged(short previousValue, short newValue)
    {
        currentLevelStatus = (LevelStatus)currentNetworkLevelStatus.Value;
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

        int index = 0;
        for (int i = 0; i < PlayerLevelInfoNetworkList.Count; i++)
        {
            PlayerLevelInfo info = PlayerLevelInfoNetworkList[i];
            Transform spawnpoint = spawnPointList[index].transform;
            RespawnCharacterRpc(info.clientId, 0, new SpawnOptions()
            {
                position = spawnpoint.transform.position,
                rotation = spawnpoint.transform.rotation,
                spawnAsSpectator = false
            });
            index++;
            if (index >= spawnPointList.Count) index = 0;
            yield return 0; //wait for next frame
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
    public void RespawnCharacterRpc(ulong clientId, float timeToSpawn, SpawnOptions options)
    {
        Debug.Log("Respawn");
        PlayerLevelInfo info = GetPlayerLevelInfoFromNetworkList(clientId);
        if (info.character.TryGet(out Playable character))
        {
            info.character = default;
            if (character.NetworkObject.IsSpawned)
                character.NetworkObject.Despawn(true);
            else
                Destroy(character.gameObject);
        }
        UpdateNetworkList(info);
        StartCoroutine(RespawnCharacter(info.clientId, timeToSpawn, options));
    }

    IEnumerator RespawnCharacter(ulong clientId, float timeToSpawn, SpawnOptions options)
    {
        Debug.Log("Respawning");
        PlayerLevelInfo info = GetPlayerLevelInfoFromNetworkList(clientId);
        info.playerStatus = (short)PlayerStatus.Respawning;
        UpdateNetworkList(info);

        yield return new WaitForSeconds(timeToSpawn);
        SpawnCharacterRpc(clientId, options);
    }

    [Rpc(SendTo.Server)]
    public void SpawnCharacterRpc(ulong clientId, SpawnOptions options)
    {
        Debug.Log("Spawn");
        PlayerLevelInfo info = GetPlayerLevelInfoFromNetworkList(clientId);
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
        UpdateNetworkList(info);
        OnPlayerSpawn(clientId);
    }

    [Rpc(SendTo.Server)]
    public void KillCharacterRpc(ulong clientId)
    {
        Debug.Log("Kill");
        PlayerLevelInfo info = GetPlayerLevelInfoFromNetworkList(clientId);
        if (info.character.TryGet(out Playable character))
        {
            info.character = default;
            StartCoroutine(DestroyAndDespawnAfter(character, 3f));
        }
        info.playerStatus = (short)PlayerStatus.Dead;
        UpdateNetworkList(info);
        OnPlayerDeath(clientId);
    }

    IEnumerator DestroyAndDespawnAfter(Playable character, float time)
    {
        yield return new WaitForSeconds(time);
        if (character == null) yield break;
        if (character.NetworkObject.IsSpawned)
            character.NetworkObject.Despawn(true);
        else
            Destroy(character.gameObject);
    }
    #endregion

    #region CustomLogic
    private void CustomOnPlayerDeathLogicWaitingForPlayers(ulong clientId)
    {
        Transform spawnpoint = spawnPointList[UnityEngine.Random.Range(0, spawnPointList.Count - 1)].transform;
        RespawnCharacterRpc(clientId, respawnTime, new SpawnOptions()
        {
            position = spawnpoint.transform.position,
            rotation = spawnpoint.transform.rotation,
            spawnAsSpectator = false
        });
    }

    private void CustomOnPlayerDeathLogicProgress(ulong clientId)
    {
        PlayerLevelInfo info = GetPlayerLevelInfoFromNetworkList(clientId);
        if (currentLevelStatus == LevelStatus.InProgress)
        {
            info.playerScore--;
            if (info.playerScore > 0)
            {
                Transform spawnpoint = spawnPointList[UnityEngine.Random.Range(0, spawnPointList.Count - 1)].transform;
                RespawnCharacterRpc(clientId, respawnTime, new SpawnOptions()
                {
                    position = spawnpoint.transform.position,
                    rotation = spawnpoint.transform.rotation,
                    spawnAsSpectator = false
                });
            }
            else
            {
                Transform spawnpoint = spawnPointList[UnityEngine.Random.Range(0, spawnPointList.Count - 1)].transform;
                RespawnCharacterRpc(clientId, respawnTime, new SpawnOptions()
                {
                    position = spawnpoint.transform.position,
                    rotation = spawnpoint.transform.rotation,
                    spawnAsSpectator = true
                });
            }
            UpdateNetworkList(info);
        }
        else
        {
            Transform spawnpoint = spawnPointList[UnityEngine.Random.Range(0, spawnPointList.Count - 1)].transform;
            RespawnCharacterRpc(clientId, respawnTime, new SpawnOptions()
            {
                position = spawnpoint.transform.position,
                rotation = spawnpoint.transform.rotation,
                spawnAsSpectator = true
            });
        }
    }

    private void CustomOnPlayerSpawnLogicProgress(ulong clientId)
    {
        PlayerLevelInfo info = GetPlayerLevelInfoFromNetworkList(clientId);
        if (info.character.TryGet(out ThirdPersonController character))
        {
            Weapon weapon = Instantiate(spawnWeapon);
            weapon.NetworkObject.Spawn(true);
            weapon.wielderNetworkBehaviourReference.Value = character;
            character.weaponNetworkBehaviourReference.Value = weapon;
        }
    }
    #endregion
}