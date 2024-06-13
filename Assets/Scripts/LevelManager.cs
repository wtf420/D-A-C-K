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
public class PlayerLevelInfo
{
    public NetworkPlayer _networkPlayer;
    public ThirdPersonController _character;
    public float _playerScore;

    public NetworkPlayer networkPlayer {
        get { return _networkPlayer; }
        set { _networkPlayer = value; }
    }
    public ThirdPersonController character
    {
        get { return _character; }
        set { _character = value; Debug.Log("Value changed to " + value); }
    }
    public float playerScore
    {
        get { return _playerScore; }
        set { _playerScore = value; }
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

    [SerializeField] List<PlayerLevelInfo> PlayerLevelInfoList;

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
        if (IsServer) StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        yield return new WaitUntil(() => networkManager.IsServer);
        foreach (NetworkPlayer networkPlayer in GameObject.FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.InstanceID))
        {
            AddPlayer(networkPlayer);
        }

        Debug.Log("Awaiting Players...");
        currentLevelPhase = LevelPhase.WaitingForPlayers;
        yield return new WaitUntil(() => PlayerLevelInfoList.Count >= miniumPlayerToStart);

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
            networkPlayer = player,
            character = null,
            playerScore = 1
        };
        PlayerLevelInfoList.Add(playerLevelInfo);
    }

    public void RemovePlayer(NetworkPlayer player)
    {
        PlayerLevelInfo info = PlayerLevelInfoList.Where(x => x.networkPlayer == player).FirstOrDefault();
        PlayerLevelInfoList.Remove(info);
    }

    public void OnPlayerSpawn(NetworkPlayer player)
    {

    }

    public void OnPlayerDeath(NetworkPlayer player)
    {
        PlayerLevelInfo info = PlayerLevelInfoList.Where(x => x.networkPlayer == player).FirstOrDefault();
        info.playerScore--;
    }

    [Rpc(SendTo.Server)]
    public void SpawnCharacterRpc(NetworkBehaviourReference playerReference)
    {
        PlayerLevelInfo info = PlayerLevelInfoList.Where(x => x.networkPlayer == playerReference).FirstOrDefault();
        ThirdPersonController character = Instantiate(characterPlayerPrefab, null);
        character.NetworkObject.SpawnWithOwnership(this.OwnerClientId, true);

        info.character = character;
        info.networkPlayer.currentCharacterNetworkBehaviourReference.Value = character;
        info.character.controlPlayerNetworkBehaviourReference.Value = playerReference;
    }

    [Rpc(SendTo.Server)]
    public void KillCharacterRpc(NetworkBehaviourReference playerReference, bool destroy = true)
    {
        PlayerLevelInfo info = PlayerLevelInfoList.Where(x => x.networkPlayer == playerReference).FirstOrDefault();
        if (info.character)
        {
            info.character.NetworkObject.Despawn(destroy);
            info.character = null;
        }
    }

    [Rpc(SendTo.Server)]
    public void RespawnCharacterRpc(NetworkBehaviourReference playerReference, float respawnTime = 1f, bool destroy = true)
    {
        Debug.Log("RespawnCharacterRpc");
        PlayerLevelInfo info = PlayerLevelInfoList.Where(x => x.networkPlayer == playerReference).FirstOrDefault();
        if (info.character)
        {
            info.character.NetworkObject.Despawn(destroy);
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
        foreach (PlayerLevelInfo info in PlayerLevelInfoList)
        {
            if (info.playerScore > 0) currentAlivePlayer++;
            if (currentAlivePlayer > 1) return false;
        }
        return true;
    }

    // [Rpc(SendTo.Server)]
    // public void SpawnCharacterOnServerRpc()
    // {
    //     // if (LevelManager.Instance.playerScoreDict[this] > 0)
    //     {
    //         ThirdPersonController currentCharacter = Instantiate(characterPlayerPrefab, null);
    //         currentCharacter.NetworkObject.SpawnWithOwnership(this.OwnerClientId, true);
    //         // Initialize data
    //         // currentCharacterNetworkBehaviourReference.Value = currentCharacter;
    //         // currentCharacter.controlPlayerNetworkBehaviourReference.Value = this;
    //     }
    // }

    // [Rpc(SendTo.Server)]
    // public void KillCharacterRpc()
    // {
    //     // currentCharacterNetworkBehaviourReference.Value = default;
    //     // currentCharacter.NetworkObject.Despawn(true);
    //     StartCoroutine(RespawnCharacter());
    // }
}