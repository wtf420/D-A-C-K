using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum LevelStatus : short
{
    None = 0,
    WaitingForPlayers = 1,
    CountDown = 2,
    InProgress = 3,
    Done = 4
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

    public TMP_Text waitingForPlayersText;
    public TMP_Text gameOverText;
    public ScoreBoard scoreBoard;

    [SerializeField] public List<GameObject> spawnPointList;

    [SerializeField] public int miniumPlayerToStart = 4;
    [SerializeField] public LevelStatus currentLevelStatus = LevelStatus.None;

    [SerializeField] public NetworkVariable<short> currentNetworkLevelStatus = new NetworkVariable<short>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] public NetworkList<PlayerLevelInfo> PlayerLevelInfoNetworkList;
    // public UnityEvent<ThirdPersonController> OnPlayerSpawn, OnPlayerDeath;

    public bool GameStarted = false;

    void Awake()
    {
        if (Instance) Destroy(Instance.gameObject);
        Instance = this;
        PlayerLevelInfoNetworkList = new NetworkList<PlayerLevelInfo>();
    }

    // Start is called before the first frame update
    void Start()
    {
        networkManager = NetworkManager.Singleton;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Tab)) scoreBoard.gameObject.SetActive(true); else scoreBoard.gameObject.SetActive(false);
        if (!IsServer) return;
    }

    public override void OnNetworkSpawn()
    {
        networkManager = NetworkManager.Singleton;

        networkManager.OnClientDisconnectCallback += OnDisconnectedCallback;
        networkManager.OnClientConnectedCallback += OnConnectedCallback;
        networkManager.SceneManager.OnLoadComplete += OnSceneLoadComplete;
        networkManager.SceneManager.OnSynchronizeComplete += SyncDataAsLateJoiner;
        PlayerLevelInfoNetworkList.OnListChanged += OnListChanged;
        currentNetworkLevelStatus.OnValueChanged += OnGamePhaseChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        networkManager.OnClientDisconnectCallback -= OnDisconnectedCallback;
        networkManager.OnClientConnectedCallback -= OnConnectedCallback;
        networkManager.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
        networkManager.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
        PlayerLevelInfoNetworkList.OnListChanged -= OnListChanged;
        currentNetworkLevelStatus.OnValueChanged -= OnGamePhaseChanged;
    }

    private void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (clientId != NetworkManager.LocalClientId) return;
        SpawnPlayerObjectRpc(clientId, PersistentPlayer.Instance.playerData);
        networkManager.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
    }

    private void OnGamePhaseChanged(short previousValue, short newValue)
    {
        currentLevelStatus = (LevelStatus)currentNetworkLevelStatus.Value;
        Debug.Log("OnGamePhaseChanged");
    }

    private void OnListChanged(NetworkListEvent<PlayerLevelInfo> changeEvent)
    {
        PlayerNetworkListToNormalList().Clear();
        foreach (PlayerLevelInfo info in PlayerLevelInfoNetworkList)
        {
            PlayerNetworkListToNormalList().Add(info);
        }
    }

    private void OnConnectedCallback(ulong clientId)
    {
        if (networkManager.LocalClientId == clientId)
        {
            SpawnPlayerObjectRpc(clientId, PersistentPlayer.Instance.playerData);
        }
    }

    [Rpc(SendTo.Server)]
    void SpawnPlayerObjectRpc(ulong clientId, PlayerData playerData)
    {
        Debug.Log("SpawnPlayerObjectRpc: " + playerData.PlayerName + " | " + playerData.PlayerColor);
        NetworkPlayer player = Instantiate(networkPlayerPrefab, Vector3.zero, Quaternion.identity, null);
        player.playerData = playerData;
        player.NetworkObject.SpawnAsPlayerObject(clientId);
        StartCoroutine(AddnewPlayer(clientId));
    }

    IEnumerator AddnewPlayer(ulong clientId)
    {
        NetworkPlayer player = null;
        yield return new WaitUntil(() => networkManager.SpawnManager.GetPlayerNetworkObject(clientId));
        player = networkManager.SpawnManager.GetPlayerNetworkObject(clientId).GetComponent<NetworkPlayer>();
        if (player)
        {
            AddPlayer(player);
        }
        if (clientId == NetworkManager.ServerClientId) SpawnSpectatorRpc(clientId);
        else SpawnCharacterRpc(clientId);
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
    }

    void SyncDataAsLateJoiner(ulong clientId)
    {
        if (clientId != NetworkManager.LocalClientId) return;
        // manually refresh list
        OnListChanged(default);
        Debug.Log("SyncDataAsLateJoiner");
    }

    public void UpdateNetworkList(PlayerLevelInfo info)
    {
        int index = GetPlayerFromNetworkList(info.clientId);
        if (index != -1)
        {
            PlayerLevelInfoNetworkList[index] = info;
        }
    }

    public int GetPlayerFromNetworkList(ulong clientId)
    {
        for (int i = 0; i < PlayerLevelInfoNetworkList.Count; i++)
        {
            if (clientId == PlayerLevelInfoNetworkList[i].clientId)
                return i;
        }
        return -1;
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

    IEnumerator GameLoop()
    {
        yield return new WaitUntil(() => networkManager.IsServer || networkManager.IsHost);
        GameStarted = true;

        Debug.Log("Awaiting Players...");
        currentNetworkLevelStatus.Value = (short)LevelStatus.WaitingForPlayers;
        waitingForPlayersText.gameObject.SetActive(true);
        waitingForPlayersText.text = "Waiting for players. (" + PlayerNetworkListToNormalList().Count + "/" + miniumPlayerToStart + ")";
        yield return new WaitUntil(() => PlayerNetworkListToNormalList().Count >= miniumPlayerToStart);

        Debug.Log("Begining game!");
        foreach (PlayerLevelInfo info in PlayerNetworkListToNormalList().ToList())
        {
            Debug.Log("PlayerData: " + info.playerName + " | " + info.playerColor);
            yield return 0; //wait for next frame
            RespawnCharacterRpc(info.clientId, 0f);
        }
        waitingForPlayersText.gameObject.SetActive(false);
        currentNetworkLevelStatus.Value = (short)LevelStatus.InProgress;
        yield return new WaitUntil(() => GameOver());

        Debug.Log("Game Over!");
        // foreach (PlayerLevelInfo info in PlayerNetworkListToNormalList().ToList())
        // {
        //     KillCharacterRpc(info.clientId);
        // }
        waitingForPlayersText.text = "Waiting for players. (" + PlayerNetworkListToNormalList().Count + "/" + miniumPlayerToStart + ")";
        currentNetworkLevelStatus.Value = (short)LevelStatus.Done;
        yield return new WaitForSeconds(5f);

        // networkManager.SceneManager.LoadScene("LobbyScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void AddPlayer(NetworkPlayer player)
    {
        PlayerLevelInfo playerLevelInfo = new PlayerLevelInfo
        {
            clientId = player.OwnerClientId,
            playerName = player.playerName.Value,
            playerColor = player.playerColor.Value,
            networkPlayer = player,
            character = default,
            playerScore = 1
        };
        PlayerLevelInfoNetworkList.Add(playerLevelInfo);
        Debug.Log("PlayerData: " + player.playerName.Value + " | " + player.playerColor.Value);
    }

    public void RemovePlayer(NetworkPlayer player)
    {
        PlayerLevelInfo info = PlayerNetworkListToNormalList().Find(x => x.networkPlayer == player);
        RemovePlayer(info.clientId);
    }

    public void RemovePlayer(ulong clientId)
    {
        PlayerLevelInfo info = PlayerNetworkListToNormalList().Find(x => x.clientId == clientId);
        PlayerLevelInfoNetworkList.Remove(info);
    }

    public void OnPlayerSpawn(ulong clientId)
    {

    }

    public void OnPlayerDeath(ulong clientId)
    {
        PlayerLevelInfo info = PlayerNetworkListToNormalList().First(x => x.clientId == clientId);
        if (info.playerScore > 0)
        {
            info.playerScore--;
            UpdateNetworkList(info);
            RespawnCharacterRpc(clientId, 3f, false);
        } else
        {
            UpdateNetworkList(info);
        }
    }

    [Rpc(SendTo.Server)]
    public void SpawnCharacterRpc(ulong clientId)
    {
        PlayerLevelInfo info = PlayerNetworkListToNormalList().First(x => x.clientId == clientId);
        Transform spawn = spawnPointList[UnityEngine.Random.Range(0, spawnPointList.Count -1)].transform;
        ThirdPersonController character = Instantiate(characterPlayerPrefab, spawn.position, spawn.rotation, null);
        character.controlPlayerNetworkBehaviourReference.Value = info.networkPlayer;
        character.NetworkObject.SpawnWithOwnership(info.clientId, true);

        OnPlayerSpawn(clientId);
        info.character = character;
        UpdateNetworkList(info);
    }

    [Rpc(SendTo.Server)]
    public void SpawnSpectatorRpc(ulong clientId)
    {
        PlayerLevelInfo info = PlayerNetworkListToNormalList().First(x => x.clientId == clientId);
        ThirdPersonSpectatorController character = Instantiate(spectatorPlayerPrefab, null);
        character.controlPlayerNetworkBehaviourReference.Value = info.networkPlayer;
        character.NetworkObject.SpawnWithOwnership(info.clientId, true);

        OnPlayerSpawn(clientId);
        info.character = character;
        UpdateNetworkList(info);
    }

    [Rpc(SendTo.Server)]
    public void KillCharacterRpc(ulong clientId)
    {
        PlayerLevelInfo info = PlayerNetworkListToNormalList().First(x => x.clientId == clientId);
        if (info.character.TryGet(out ThirdPersonController character))
        {
            info.character = default;
            OnPlayerDeath(clientId);
            StartCoroutine(KillAndDespawnAfter(character, 3f));
        }
    }

    IEnumerator KillAndDespawnAfter(ThirdPersonController character, float time)
    {
        yield return new WaitForSeconds(time);
        if (character == null) yield break;
        if (character.NetworkObject.IsSpawned)
            character.NetworkObject.Despawn();
        else
            Destroy(character.gameObject);
    }

    [Rpc(SendTo.Server)]
    public void RespawnCharacterRpc(ulong clientId, float respawnTime = 1f, bool destroy = true)
    {
        PlayerLevelInfo info = PlayerNetworkListToNormalList().First(x => x.clientId == clientId);
        if (info.character.TryGet(out ThirdPersonController character) && destroy)
        {
            if (character.NetworkObject.IsSpawned) 
                character.NetworkObject.Despawn(); 
            else
                Destroy(character.gameObject);
        }
        StartCoroutine(RespawnCharacter(info.clientId, respawnTime));
    }

    IEnumerator RespawnCharacter(ulong clientId, float respawnTime = 1f)
    {
        yield return new WaitForSeconds(respawnTime);
        SpawnCharacterRpc(clientId);
    }

    public bool GameOver()
    {
        Debug.Log("Checking");
        int currentAlivePlayer = 0;
        PlayerLevelInfo winner = new PlayerLevelInfo();
        foreach (PlayerLevelInfo info in PlayerNetworkListToNormalList())
        {
            if (info.playerScore > 0)
            {
                currentAlivePlayer++;
                winner = info;
            }
            if (currentAlivePlayer > 1) return false;
        }
        gameOverText.gameObject.SetActive(true);
        gameOverText.text = "Game is over!\nWinner is: " + winner.playerName.ToString() + winner.clientId.ToString();
        return true;
    }
}