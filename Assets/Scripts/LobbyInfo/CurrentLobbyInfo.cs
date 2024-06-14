using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public enum LobbyStatus
{
    InLobby,
    InGame,
}

public enum PlayerLobbyStatus
{
    NotReady,
    Ready,
    InGame
}

#region PlayerLobbyInfo
[Serializable]
[HideInInspector]
public struct PlayerLobbyInfo : INetworkSerializable, IEquatable<PlayerLobbyInfo>
{
    public ulong clientId;
    public NetworkBehaviourReference networkPlayer;
    public PlayerLobbyStatus playerLobbyStatus;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out clientId);
            reader.ReadValueSafe(out networkPlayer);
            reader.ReadValueSafe(out playerLobbyStatus);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(clientId);
            writer.WriteValueSafe(networkPlayer);
            writer.WriteValueSafe(playerLobbyStatus);
        }
    }

    public bool Equals(PlayerLobbyInfo other)
    {
        return clientId == other.clientId;
    }
}
#endregion

public class CurrentLobbyInfo : NetworkBehaviour
{
    public static CurrentLobbyInfo Instance;
    public NetworkManager networkManager;

    [SerializeField] LobbyStatus currentLobbyStatus;

    [SerializeField] NetworkList<PlayerLobbyInfo> PlayerLobbyInfoNetworkList;
    [SerializeField] List<PlayerLobbyInfo> PlayerLobbyInfoLocalList = new List<PlayerLobbyInfo>();
    // public UnityEvent<ThirdPersonController> OnPlayerSpawn, OnPlayerDeath;

    void Awake()
    {
        if (Instance)
            Destroy(this.gameObject);
        else
            Instance = this;
        PlayerLobbyInfoNetworkList = new NetworkList<PlayerLobbyInfo>();
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
        PlayerLobbyInfoNetworkList.OnListChanged += OnListChanged;

        if (IsServer) StartCoroutine(LobbyManager.Instance.HeartbeatLobbyCoroutine(1f));
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnectedCallback;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnConnectedCallback;
        PlayerLobbyInfoNetworkList.OnListChanged -= OnListChanged;

        if (IsServer) StopAllCoroutines();
    }

    private void OnListChanged(NetworkListEvent<PlayerLobbyInfo> changeEvent)
    {
        Debug.Log("OnListChanged");
        PlayerLobbyInfoLocalList.Clear();
        foreach (PlayerLobbyInfo info in PlayerLobbyInfoNetworkList)
        {
            PlayerLobbyInfoLocalList.Add(info);
        }
    }

    private void OnConnectedCallback(ulong clientId)
    {
        if (IsServer && !PlayerLobbyInfoLocalList.Any(x => x.clientId == clientId))
        {
            AddPlayer(NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId).GetComponent<NetworkPlayer>());
        }
        else
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // since this does not require any external data, you can SyncDataAsLateJoiner here instead of NetworkManager.Singleton.SceneManager.OnSynchronizeComplete
            SyncDataAsLateJoiner();
        }
    }

    private void SyncDataAsLateJoiner()
    {
        // manually refresh list
        OnListChanged(default);
    }

    private void OnDisconnectedCallback(ulong clientId)
    {
        if (IsServer && PlayerLobbyInfoLocalList.Any(x => x.clientId == clientId))
        {
            RemovePlayer(clientId);
        }
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Player " + clientId + " disconnected.");
        }
    }

    public void AddPlayer(NetworkPlayer player)
    {
        PlayerLobbyInfo PlayerLobbyInfo = new PlayerLobbyInfo
        {
            clientId = player.OwnerClientId,
            networkPlayer = player,
            playerLobbyStatus = currentLobbyStatus == LobbyStatus.InLobby ? PlayerLobbyStatus.NotReady : PlayerLobbyStatus.InGame
        };
        PlayerLobbyInfoNetworkList.Add(PlayerLobbyInfo);
    }

    public void RemovePlayer(NetworkPlayer player)
    {
        PlayerLobbyInfo info = PlayerLobbyInfoLocalList.Find(x => x.networkPlayer == player);
        PlayerLobbyInfoNetworkList.Remove(info);
    }

    public void RemovePlayer(ulong clientId)
    {
        PlayerLobbyInfo info = PlayerLobbyInfoLocalList.Find(x => x.clientId == clientId);
        PlayerLobbyInfoNetworkList.Remove(info);
    }
}
