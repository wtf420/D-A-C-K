using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

#region NetworkPlayerInfo
[Serializable]
public struct NetworkPlayerInfo : INetworkSerializable, IEquatable<NetworkPlayerInfo>
{
    public ulong clientId;
    public FixedString32Bytes playerName;
    public FixedString32Bytes playerColor;
    public NetworkBehaviourReference networkPlayer;
    public NetworkBehaviourReference character;

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
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(clientId);
            writer.WriteValueSafe(playerName);
            writer.WriteValueSafe(playerColor);
            writer.WriteValueSafe(networkPlayer);
            writer.WriteValueSafe(character);
        }
    }

    public bool Equals(NetworkPlayerInfo other)
    {
        return clientId == other.clientId;
    }
}
#endregion

public class NetworkPlayersManager : NetworkBehaviour
{
    public static NetworkPlayersManager Instance;
    public NetworkManager networkManager => NetworkManager.Singleton;

    [SerializeField] public NetworkPlayer networkPlayerPrefab;

    public NetworkList<NetworkPlayerInfo> NetworkPlayerInfoNetworkList;
    public UnityEvent<ulong> OnPlayerJoinedEvent, OnPlayerLeaveEvent;
    public UnityEvent<NetworkPlayerInfo> OnNetworkPlayerInfoChangedEvent;

    #region Mono & NetworkBehaviour
    void Awake()
    {
        if (Instance) Destroy(Instance.gameObject);
        Instance = this;
        NetworkPlayerInfoNetworkList = new NetworkList<NetworkPlayerInfo>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("\n\n\n");
            Debug.Log("Players count: " + NetworkPlayerInfoNetworkList.Count);
            foreach (NetworkPlayerInfo networkPlayerInfo in NetworkPlayerInfoNetworkList)
            {
                Debug.Log("\n\n\n");
                Debug.Log("clientId: " + networkPlayerInfo.clientId.ToString());
                Debug.Log("playerName: " + networkPlayerInfo.playerName.ToString());
                Debug.Log("playerColor: " + networkPlayerInfo.playerColor.ToString());
            }
        }
        if (!IsServer) return;
    }

    public override void OnNetworkSpawn()
    {
        networkManager.OnClientDisconnectCallback += OnDisconnectedCallback;
        networkManager.OnClientConnectedCallback += OnConnectedCallback;
        networkManager.SceneManager.OnLoadComplete += OnSceneLoadComplete;
        networkManager.SceneManager.OnSynchronizeComplete += SyncDataAsLateJoiner;
        NetworkPlayerInfoNetworkList.OnListChanged += OnNetworkPlayerInfoNetworkListChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        networkManager.OnClientDisconnectCallback -= OnDisconnectedCallback;
        networkManager.OnClientConnectedCallback -= OnConnectedCallback;
        networkManager.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
        networkManager.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
        NetworkPlayerInfoNetworkList.OnListChanged -= OnNetworkPlayerInfoNetworkListChanged;

        OnPlayerJoinedEvent.RemoveAllListeners();
        OnPlayerLeaveEvent.RemoveAllListeners();
        OnNetworkPlayerInfoChangedEvent.RemoveAllListeners();

        LobbyManager.Instance.ExitGame();
    }

    private void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (clientId != NetworkManager.LocalClientId) return;
        if (!GetNetworkPlayerInfoFromNetworkList(clientId).networkPlayer.TryGet(out NetworkPlayer _)) SpawnPlayerObjectRpc(clientId, PersistentPlayer.Instance.playerData);
        networkManager.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
    }

    private void OnConnectedCallback(ulong clientId)
    {
        if (clientId != NetworkManager.LocalClientId) return;
        if (!GetNetworkPlayerInfoFromNetworkList(clientId).networkPlayer.TryGet(out NetworkPlayer _)) SpawnPlayerObjectRpc(clientId, PersistentPlayer.Instance.playerData);
    }

    private void OnDisconnectedCallback(ulong clientId)
    {
        if (IsServer && PlayerNetworkListToNormalList().Any(x => x.clientId == clientId) && clientId != networkManager.LocalClientId)
        {
            RemovePlayer(clientId);
        }
        if (clientId == networkManager.LocalClientId)
        {
            LobbyManager.Instance.ExitGame();
        }
        OnPlayerLeaveEvent?.Invoke(clientId);
    }

    void SyncDataAsLateJoiner(ulong clientId)
    {
        if (clientId != NetworkManager.LocalClientId) return;
        // manually refresh
        OnNetworkPlayerInfoNetworkListChanged(default);
    }
    #endregion

    #region Player Management
    private void OnNetworkPlayerInfoNetworkListChanged(NetworkListEvent<NetworkPlayerInfo> changeEvent)
    {
        PlayerNetworkListToNormalList().Clear();
        foreach (NetworkPlayerInfo info in NetworkPlayerInfoNetworkList)
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
            AddNetworkPlayerInfo(player);
        }
        OnPlayerJoinedEvent?.Invoke(clientId);
        // Transform spawnpoint = spawnPointList[UnityEngine.Random.Range(0, spawnPointList.Count - 1)].transform;
        // SpawnCharacterRpc(clientId, new SpawnOptions()
        // {
        //     position = spawnpoint.transform.position,
        //     rotation = spawnpoint.transform.rotation,
        //     spawnAsSpectator = false
        // });
    }

    public void AddNetworkPlayerInfo(NetworkPlayer player)
    {
        NetworkPlayerInfo NetworkPlayerInfo = new NetworkPlayerInfo
        {
            clientId = player.OwnerClientId,
            playerName = player.playerName.Value,
            playerColor = player.playerColor.Value,
            networkPlayer = player,
        };
        NetworkPlayerInfoNetworkList.Add(NetworkPlayerInfo);
    }

    public void RemovePlayer(ulong clientId)
    {
        OnPlayerLeaveEvent.Invoke(clientId);
        int index = GetPlayerIndexFromNetworkList(clientId);
        if (index != -1)
            NetworkPlayerInfoNetworkList.RemoveAt(index);
    }

    public void UpdateNetworkList(NetworkPlayerInfo info)
    {
        int index = GetPlayerIndexFromNetworkList(info.clientId);
        if (index != -1)
        {
            NetworkPlayerInfoNetworkList[index] = info;
            OnNetworkPlayerInfoChangedEvent.Invoke(info);
        }
    }

    public int GetPlayerIndexFromNetworkList(ulong clientId)
    {
        for (int i = 0; i < NetworkPlayerInfoNetworkList.Count; i++)
        {
            if (clientId == NetworkPlayerInfoNetworkList[i].clientId)
                return i;
        }
        return -1;
    }

    public NetworkPlayerInfo GetNetworkPlayerInfoFromNetworkList(ulong clientId)
    {
        for (int i = 0; i < NetworkPlayerInfoNetworkList.Count; i++)
        {
            if (clientId == NetworkPlayerInfoNetworkList[i].clientId)
                return NetworkPlayerInfoNetworkList[i];
        }
        return default;
    }

    public List<NetworkPlayerInfo> PlayerNetworkListToNormalList()
    {
        List<NetworkPlayerInfo> NetworkPlayerInfos = new List<NetworkPlayerInfo>();
        for (int i = 0; i < NetworkPlayerInfoNetworkList.Count; i++)
        {
            NetworkPlayerInfos.Add(NetworkPlayerInfoNetworkList[i]);
        }
        return NetworkPlayerInfos;
    }
    #endregion
}
