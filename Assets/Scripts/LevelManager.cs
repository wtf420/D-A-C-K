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

public enum LevelStatus
{
    WaitingForPlayers,
    InProgress,
    Done
}

#region PlayerLevelInfo
[Serializable]
public struct PlayerLevelInfo : INetworkSerializable, IEquatable<PlayerLevelInfo>
{
    public ulong clientId;
    public NetworkBehaviourReference networkPlayer;
    public NetworkBehaviourReference character;
    public float playerScore;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out clientId);
            reader.ReadValueSafe(out networkPlayer);
            reader.ReadValueSafe(out character);
            reader.ReadValueSafe(out playerScore);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(clientId);
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
    [SerializeField] LevelStatus currentLevelPhase;

    [SerializeField] NetworkList<PlayerLevelInfo> PlayerLevelInfoNetworkList = new NetworkList<PlayerLevelInfo>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] List<PlayerLevelInfo> PlayerLevelInfoLocalList;
    // public UnityEvent<ThirdPersonController> OnPlayerSpawn, OnPlayerDeath;

    void Awake()
    {
        if (Instance) Destroy(Instance.gameObject);
        Instance = this;
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
        PlayerLevelInfoNetworkList.OnListChanged += OnListChanged;

        if (IsServer) StartCoroutine(GameLoop());
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnectedCallback;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnConnectedCallback;
        PlayerLevelInfoNetworkList.OnListChanged -= OnListChanged;
        base.OnNetworkDespawn();
    }

    private void OnListChanged(NetworkListEvent<PlayerLevelInfo> changeEvent)
    {
        Debug.Log("OnListChanged");
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
            AddPlayer(NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId).GetComponent<NetworkPlayer>());
        }
        else
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            OnListChanged(default);
        }
    }

    private void OnDisconnectedCallback(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Player " + clientId + " disconnected.");
        }
    }

    IEnumerator GameLoop()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost);
        // level is loaded after begin host, get players
        foreach (NetworkPlayer networkPlayer in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.InstanceID))
        {
            if (!PlayerLevelInfoLocalList.Any(x => x.clientId == networkPlayer.OwnerClientId))
            AddPlayer(networkPlayer);
        }

        Debug.Log("Awaiting Players...");
        currentLevelPhase = LevelStatus.WaitingForPlayers;
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

    public void OnPlayerSpawn(NetworkPlayer player)
    {

    }

    public void OnPlayerDeath(NetworkPlayer player)
    {
        PlayerLevelInfo info = PlayerLevelInfoLocalList.Where(x => x.networkPlayer == player).FirstOrDefault();
        info.playerScore--;
    }

    [Rpc(SendTo.Server)]
    public void SpawnCharacterRpc(NetworkBehaviourReference playerReference)
    {
        PlayerLevelInfo info = PlayerLevelInfoLocalList.First(x => x.networkPlayer.Equals(playerReference));
        ThirdPersonController character = Instantiate(characterPlayerPrefab, null);
        character.NetworkObject.SpawnWithOwnership(info.clientId, true);

        info.character = character;
        info.networkPlayer = character;
        info.character = playerReference;

        character.controlPlayerNetworkBehaviourReference.Value = playerReference;
    }

    [Rpc(SendTo.Server)]
    public void KillCharacterRpc(NetworkBehaviourReference playerReference, bool destroy = true)
    {
        PlayerLevelInfo info = PlayerLevelInfoLocalList.First(x => x.networkPlayer.Equals(playerReference));
        if (info.character.TryGet(out ThirdPersonController character))
        {
            character.NetworkObject.Despawn(destroy);
            info.character = default;
        }
    }

    [Rpc(SendTo.Server)]
    public void RespawnCharacterRpc(NetworkBehaviourReference playerReference, float respawnTime = 1f, bool destroy = true)
    {
        Debug.Log("RespawnCharacterRpc");
        PlayerLevelInfo info = PlayerLevelInfoLocalList.First(x => x.networkPlayer.Equals(playerReference));
        if (info.character.TryGet(out ThirdPersonController character))
        {
            character.NetworkObject.Despawn(destroy);
        }
        StartCoroutine(RespawnCharacter(info.networkPlayer, respawnTime));
    }

    IEnumerator RespawnCharacter(NetworkBehaviourReference playerReference, float respawnTime = 1f)
    {
        yield return new WaitForSeconds(respawnTime);
        SpawnCharacterRpc(playerReference);
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