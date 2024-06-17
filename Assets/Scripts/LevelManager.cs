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

    public TMP_Text waitingForPlayersText;
    public TMP_Text gameOverText;

    [SerializeField] public int miniumPlayerToStart = 4;
    [SerializeField] public LevelStatus currentLevelStatus = LevelStatus.None;

    [SerializeField] NetworkVariable<short> currentNetworkLevelStatus = new NetworkVariable<short>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] NetworkList<PlayerLevelInfo> PlayerLevelInfoNetworkList;
    [SerializeField] List<PlayerLevelInfo> PlayerLevelInfoLocalList = new List<PlayerLevelInfo>();
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
        if (!IsServer) return;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        networkManager.OnClientDisconnectCallback += OnDisconnectedCallback;
        networkManager.OnClientConnectedCallback += OnConnectedCallback;
        networkManager.SceneManager.OnSynchronizeComplete += SyncDataAsLateJoiner;
        PlayerLevelInfoNetworkList.OnListChanged += OnListChanged;
        currentNetworkLevelStatus.OnValueChanged += OnGamePhaseChanged;

        if (IsServer || IsHost) 
        {
            foreach (NetworkPlayer networkPlayer in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.InstanceID))
            {
                if (!PlayerLevelInfoLocalList.Any(x => x.clientId == networkPlayer.OwnerClientId))
                    AddPlayer(networkPlayer);
            }
            StartCoroutine(GameLoop());
        }
    }

    private void OnGamePhaseChanged(short previousValue, short newValue)
    {
        currentLevelStatus = (LevelStatus)currentNetworkLevelStatus.Value;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        networkManager.OnClientDisconnectCallback -= OnDisconnectedCallback;
        networkManager.OnClientConnectedCallback -= OnConnectedCallback;
        networkManager.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
        PlayerLevelInfoNetworkList.OnListChanged -= OnListChanged;
    }

    private void OnListChanged(NetworkListEvent<PlayerLevelInfo> changeEvent)
    {
        PlayerLevelInfoLocalList.Clear();
        foreach (PlayerLevelInfo info in PlayerLevelInfoNetworkList)
        {
            PlayerLevelInfoLocalList.Add(info);
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
        Debug.Log("PlayerData: " + playerData.PlayerName + " | " + playerData.PlayerColor);
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
        // if (networkManager.IsHost) SpawnCharacterRpc(clientId);
    }

    private void OnDisconnectedCallback(ulong clientId)
    {
        if (IsServer && PlayerLevelInfoLocalList.Any(x => x.clientId == clientId) && clientId != networkManager.LocalClientId)
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
    }

    //player data synchronization complete
    IEnumerator SyncDataAsLateJoinerCouroutine(ulong clientId)
    {
        //only run on client
        if (clientId != NetworkManager.LocalClientId) yield return null;
        // manually refresh list
        OnListChanged(default);
        yield return new WaitUntil(() => networkManager.SpawnManager.GetPlayerNetworkObject(clientId));
        NetworkPlayer player = networkManager.SpawnManager.GetPlayerNetworkObject(clientId).GetComponent<NetworkPlayer>();
        if (player)
        {
            SpawnCharacterRpc(clientId);
        }
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

    IEnumerator GameLoop()
    {
        yield return new WaitUntil(() => networkManager.IsServer || networkManager.IsHost);
        GameStarted = true;

        Debug.Log("Awaiting Players...");
        currentNetworkLevelStatus.Value = (short)LevelStatus.WaitingForPlayers;
        waitingForPlayersText.gameObject.SetActive(true);
        waitingForPlayersText.text = "Waiting for players. (" + PlayerLevelInfoLocalList.Count + "/" + miniumPlayerToStart + ")";
        yield return new WaitUntil(() => PlayerLevelInfoLocalList.Count >= miniumPlayerToStart);

        Debug.Log("Begining game!");
        foreach (PlayerLevelInfo info in PlayerLevelInfoLocalList.ToList())
        {
            Debug.Log("PlayerData: " + info.playerName + " | " + info.playerColor);
            RespawnCharacterRpc(info.clientId, 0f);
        }
        waitingForPlayersText.gameObject.SetActive(false);
        currentNetworkLevelStatus.Value = (short)LevelStatus.InProgress;
        yield return new WaitUntil(() => GameOver());

        Debug.Log("Game Over!");
        foreach (PlayerLevelInfo info in PlayerLevelInfoLocalList.ToList())
        {
            KillCharacterRpc(info.clientId);
        }
        waitingForPlayersText.text = "Waiting for players. (" + PlayerLevelInfoLocalList.Count + "/" + miniumPlayerToStart + ")";
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
        PlayerLevelInfo info = PlayerLevelInfoLocalList.Find(x => x.networkPlayer == player);
        RemovePlayer(info.clientId);
    }

    public void RemovePlayer(ulong clientId)
    {
        PlayerLevelInfo info = PlayerLevelInfoLocalList.Find(x => x.clientId == clientId);
        PlayerLevelInfoNetworkList.Remove(info);
    }

    public void OnPlayerSpawn(ulong clientId)
    {

    }

    public void OnPlayerDeath(ulong clientId)
    {
        PlayerLevelInfo info = PlayerLevelInfoLocalList.First(x => x.clientId == clientId);
        info.playerScore--;
        UpdateNetworkList(info);
    }

    [Rpc(SendTo.Server)]
    public void SpawnCharacterRpc(ulong clientId)
    {
        Debug.Log("RespawnCharacter3");
        PlayerLevelInfo info = PlayerLevelInfoLocalList.First(x => x.clientId == clientId);
        ThirdPersonController character = Instantiate(characterPlayerPrefab, null);
        character.NetworkObject.SpawnWithOwnership(info.clientId, true);

        OnPlayerSpawn(clientId);
        info.character = character;
        character.controlPlayerNetworkBehaviourReference.Value = info.networkPlayer;
        UpdateNetworkList(info);
    }

    [Rpc(SendTo.Server)]
    public void KillCharacterRpc(ulong clientId, bool destroy = true)
    {
        PlayerLevelInfo info = PlayerLevelInfoLocalList.First(x => x.clientId == clientId);
        if (info.character.TryGet(out ThirdPersonController character))
        {
            character.NetworkObject.Despawn(destroy);
            info.character = default;
            OnPlayerDeath(clientId);
        }
    }

    [Rpc(SendTo.Server)]
    public void RespawnCharacterRpc(ulong clientId, float respawnTime = 1f, bool destroy = true)
    {
        PlayerLevelInfo info = PlayerLevelInfoLocalList.First(x => x.clientId == clientId);
        if (info.character.TryGet(out ThirdPersonController character))
        {
            character.NetworkObject.Despawn(destroy);
        }
        StartCoroutine(RespawnCharacter(info.clientId, respawnTime));
    }

    IEnumerator RespawnCharacter(ulong clientId, float respawnTime = 1f)
    {
        Debug.Log("RespawnCharacter2");
        yield return new WaitForSeconds(respawnTime);
        SpawnCharacterRpc(clientId);
    }

    public bool GameOver()
    {
        Debug.Log("Checking");
        int currentAlivePlayer = 0;
        PlayerLevelInfo winner = new PlayerLevelInfo();
        foreach (PlayerLevelInfo info in PlayerLevelInfoLocalList)
        {
            if (info.playerScore > 0) currentAlivePlayer++;
            if (currentAlivePlayer > 1) return false;
            winner = info;
        }
        gameOverText.gameObject.SetActive(true);
        gameOverText.text = "Game is over!\nWinner is: " + winner.playerName.ToString() + winner.clientId.ToString();
        return true;
    }
}