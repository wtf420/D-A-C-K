using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum LevelStatus
{
    None,
    WaitingForPlayers,
    InProgress,
    Done
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

    [SerializeField] public int miniumPlayerToStart = 4;
    [SerializeField] LevelStatus currentLevelPhase = LevelStatus.None;

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

        NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnectedCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += OnConnectedCallback;
        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += SyncDataAsLateJoiner;
        PlayerLevelInfoNetworkList.OnListChanged += OnListChanged;

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

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnectedCallback;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnConnectedCallback;
        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
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
        if (IsServer && !PlayerLevelInfoLocalList.Any(x => x.clientId == clientId))
        {
            NetworkPlayer player = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId).GetComponent<NetworkPlayer>();
            if (player)
            {
                AddPlayer(player);
            }
        }
    }

    private void OnDisconnectedCallback(ulong clientId)
    {
        if (IsServer && PlayerLevelInfoLocalList.Any(x => x.clientId == clientId) && clientId != networkManager.LocalClientId)
        {
            RemovePlayer(clientId);
        }
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SceneManager.LoadScene("LobbyScene");
        }
    }

    //player data synchronization complete
    private void SyncDataAsLateJoiner(ulong clientId)
    {
        //only run on client
        if (clientId != NetworkManager.LocalClientId) return;
        // manually refresh list
        OnListChanged(default);
        NetworkPlayer player = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId).GetComponent<NetworkPlayer>();
        if (player)
        {
            SpawnCharacterRpc(clientId);
        }
    }

    IEnumerator GameLoop()
    {
        
        yield return new WaitUntil(() => NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost);
        GameStarted = true;

        Debug.Log("Awaiting Players...");
        currentLevelPhase = LevelStatus.WaitingForPlayers;
        foreach (PlayerLevelInfo info in PlayerLevelInfoNetworkList)
        {
            SpawnCharacterRpc(info.clientId);
        }
        yield return new WaitUntil(() => PlayerLevelInfoLocalList.Count >= miniumPlayerToStart);

        Debug.Log("Begining game!");
        currentLevelPhase = LevelStatus.InProgress;
        yield return new WaitUntil(() => GameOver());

        Debug.Log("Game Over!");
        currentLevelPhase = LevelStatus.Done;
        yield return new WaitForSeconds(5f);

        // networkManager.SceneManager.LoadScene("LobbyScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void AddPlayer(NetworkPlayer player)
    {
        PlayerLevelInfo playerLevelInfo = new PlayerLevelInfo
        {
            clientId = player.OwnerClientId,
            networkPlayer = player,
            character = default,
            playerScore = 1
        };
        PlayerLevelInfoNetworkList.Add(playerLevelInfo);
    }

    public void RemovePlayer(NetworkPlayer player)
    {
        PlayerLevelInfo info = PlayerLevelInfoLocalList.Find(x => x.networkPlayer == player);
        PlayerLevelInfoNetworkList.Remove(info);
    }

    public void RemovePlayer(ulong clientId)
    {
        PlayerLevelInfo info = PlayerLevelInfoLocalList.Find(x => x.clientId == clientId);
        PlayerLevelInfoNetworkList.Remove(info);
    }

    public void OnPlayerSpawn(NetworkPlayer player)
    {

    }

    public void OnPlayerDeath(NetworkPlayer player)
    {
        PlayerLevelInfo info = PlayerLevelInfoLocalList.Where(x => x.networkPlayer == player).FirstOrDefault();
        info.playerScore--;
    }

    [Rpc(SendTo.Server)]
    public void SpawnCharacterRpc(ulong clientId)
    {
        PlayerLevelInfo info = PlayerLevelInfoLocalList.First(x => x.clientId == clientId);
        ThirdPersonController character = Instantiate(characterPlayerPrefab, null);
        character.NetworkObject.SpawnWithOwnership(info.clientId, true);

        info.character = character;
        character.controlPlayerNetworkBehaviourReference.Value = info.networkPlayer;
    }

    [Rpc(SendTo.Server)]
    public void KillCharacterRpc(ulong clientId, bool destroy = true)
    {
        PlayerLevelInfo info = PlayerLevelInfoLocalList.First(x => x.clientId == clientId);
        if (info.character.TryGet(out ThirdPersonController character))
        {
            character.NetworkObject.Despawn(destroy);
            info.character = default;
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
        yield return new WaitForSeconds(respawnTime);
        SpawnCharacterRpc(clientId);
    }

    public bool GameOver()
    {
        int currentAlivePlayer = 0;
        foreach (PlayerLevelInfo info in PlayerLevelInfoLocalList)
        {
            if (info.playerScore > 0) currentAlivePlayer++;
            if (currentAlivePlayer > 1) return false;
        }
        return true;
    }
}