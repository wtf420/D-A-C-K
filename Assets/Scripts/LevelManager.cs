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

public enum LevelPhase
{
    WaitingForPlayers,
    InProgress,
    Done
}

[Serializable]
[HideInInspector]
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
            reader.ReadValueSafe(out networkPlayer);
            reader.ReadValueSafe(out character);
            reader.ReadValueSafe(out playerScore);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(networkPlayer);
            writer.WriteValueSafe(character);
            writer.WriteValueSafe(playerScore);
        }
    }

    public bool Equals(PlayerLevelInfo other)
    {
        return false;
    }
}

/// Bare minimum example of generic NetworkVariableBase derived class
[Serializable]
[GenerateSerializationForGenericParameter(0)]
public class MyCustomGenericNetworkVariable<T> : NetworkVariableBase
{
    /// Managed list of class instances
    public List<T> SomeDataToSynchronize = new List<T>();

    /// <summary>
    /// Writes the complete state of the variable to the writer
    /// </summary>
    /// <param name="writer">The stream to write the state to</param>
    public override void WriteField(FastBufferWriter writer)
    {
        // Serialize the data we need to synchronize
        writer.WriteValueSafe(SomeDataToSynchronize.Count);
        for (var i = 0; i < SomeDataToSynchronize.Count; ++i)
        {
            var dataEntry = SomeDataToSynchronize[i];
            // NetworkVariableSerialization<T> is used for serializing generic types
            NetworkVariableSerialization<T>.Write(writer, ref dataEntry);
        }
    }

    /// <summary>
    /// Reads the complete state from the reader and applies it
    /// </summary>
    /// <param name="reader">The stream to read the state from</param>
    public override void ReadField(FastBufferReader reader)
    {
        // De-Serialize the data being synchronized
        var itemsToUpdate = (int)0;
        reader.ReadValueSafe(out itemsToUpdate);
        SomeDataToSynchronize.Clear();
        for (int i = 0; i < itemsToUpdate; i++)
        {
            T newEntry = default;
            // NetworkVariableSerialization<T> is used for serializing generic types
            NetworkVariableSerialization<T>.Read(reader, ref newEntry);
            SomeDataToSynchronize.Add(newEntry);
        }
    }

    public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
    {
        // Do nothing for this example
    }

    public override void WriteDelta(FastBufferWriter writer)
    {
        // Do nothing for this example
    }
}

// this will only update on the server
public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance;
    public NetworkManager networkManager;

    public NetworkPlayer networkPlayerPrefab;
    public ThirdPersonController characterPlayerPrefab;

    [SerializeField] public int miniumPlayerToStart = 4;
    [SerializeField] LevelPhase currentLevelPhase;

    [SerializeField] MyCustomGenericNetworkVariable<PlayerLevelInfo> PlayerLevelInfoList;

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
        NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnectedCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += OnConnectedCallback;
    }

    private void OnConnectedCallback(ulong clientId)
    {
        if (IsServer && !PlayerLevelInfoList.SomeDataToSynchronize.Any(x => x.clientId == clientId))
        {
            AddPlayer(NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId).GetComponent<NetworkPlayer>());
        }
        else
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {

        }
    }

    private void OnDisconnectedCallback(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Player " + clientId + " disconnected.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer) StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost);
        // level is loaded after begin host, get players
        foreach (NetworkPlayer networkPlayer in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.InstanceID))
        {
            AddPlayer(networkPlayer);
        }

        Debug.Log("Awaiting Players...");
        currentLevelPhase = LevelPhase.WaitingForPlayers;
        yield return new WaitUntil(() => PlayerLevelInfoList.SomeDataToSynchronize.Count >= miniumPlayerToStart);

        Debug.Log("Begining game!");
        currentLevelPhase = LevelPhase.InProgress;
        yield return new WaitUntil(() => GameOver());

        Debug.Log("Game Over!");
        currentLevelPhase = LevelPhase.Done;
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
        PlayerLevelInfoList.SomeDataToSynchronize.Add(playerLevelInfo);
    }

    public void RemovePlayer(NetworkPlayer player)
    {
        PlayerLevelInfo info = PlayerLevelInfoList.SomeDataToSynchronize.Find(x => x.networkPlayer == player);
        PlayerLevelInfoList.SomeDataToSynchronize.Remove(info);
    }

    public void OnPlayerSpawn(NetworkPlayer player)
    {

    }

    public void OnPlayerDeath(NetworkPlayer player)
    {
        PlayerLevelInfo info = PlayerLevelInfoList.SomeDataToSynchronize.Where(x => x.networkPlayer == player).FirstOrDefault();
        info.playerScore--;
    }

    [Rpc(SendTo.Server)]
    public void SpawnCharacterRpc(NetworkBehaviourReference playerReference)
    {
        PlayerLevelInfo info = PlayerLevelInfoList.SomeDataToSynchronize.Single(x => x.networkPlayer.Equals(playerReference));
        ThirdPersonController character = Instantiate(characterPlayerPrefab, null);
        character.NetworkObject.SpawnWithOwnership(info.clientId, true);

        info.character = character;
        info.networkPlayer = character;
        info.character = playerReference;
    }

    [Rpc(SendTo.Server)]
    public void KillCharacterRpc(NetworkBehaviourReference playerReference, bool destroy = true)
    {
        PlayerLevelInfo info = PlayerLevelInfoList.SomeDataToSynchronize.Where(x => x.networkPlayer.Equals(playerReference)).FirstOrDefault();
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
        PlayerLevelInfo info = PlayerLevelInfoList.SomeDataToSynchronize.Where(x => x.networkPlayer.Equals(playerReference)).FirstOrDefault();
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
        foreach (PlayerLevelInfo info in PlayerLevelInfoList.SomeDataToSynchronize)
        {
            if (info.playerScore > 0) currentAlivePlayer++;
            if (currentAlivePlayer > 1) return false;
        }
        return true;
    }
}