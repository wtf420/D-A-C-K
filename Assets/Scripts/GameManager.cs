using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public enum GamePhase
{
    WaitingForPlayers,
    InProgress,
    Done
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public NetworkManager networkManager;
    public NetworkVariable<bool> networkSpawned = new NetworkVariable<bool>(false);
    public List<NetworkPlayer> players = new List<NetworkPlayer>();

    [SerializeField] public Dictionary<NetworkPlayer, int> playerScoreDict = new Dictionary<NetworkPlayer, int>();
    [SerializeField] public int miniumPlayerToStart = 4;

    [SerializeField] GamePhase currentGamePhase;

    void Awake()
    {
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
        if (networkSpawned.Value)
        {

        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        networkSpawned.Value = true;
        if (IsServer) StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        Debug.Log("Awaiting Players...");
        currentGamePhase = GamePhase.WaitingForPlayers;
        yield return new WaitUntil(() => players.Count >= miniumPlayerToStart);

        Debug.Log("Begining game!");
        currentGamePhase = GamePhase.InProgress;
        yield return new WaitUntil(() => GameOver());

        Debug.Log("Game Over!");
        currentGamePhase = GamePhase.Done;
        yield return new WaitForSeconds(5f);

        networkManager.SceneManager.LoadScene("LobbyScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void AddPlayer(NetworkPlayer player)
    {
        players.Add(player);
        playerScoreDict.Add(player, 1);

        player.OnDeath.AddListener(OnPlayerDeath);
    }

    public void RemovePlayer(NetworkPlayer player)
    {
        players.Remove(player);
        playerScoreDict.Remove(player);

        player.OnDeath.RemoveAllListeners();
    }

    public void OnPlayerSpawn(NetworkPlayer player)
    {

    }

    public void OnPlayerDeath(NetworkPlayer player)
    {
        playerScoreDict[player] -= 1;
    }

    public bool GameOver()
    {
        int currentAlivePlayer = 0;
        foreach (KeyValuePair<NetworkPlayer, int> entry in playerScoreDict)
        {
            if (entry.Value > 0) currentAlivePlayer++;
            if (currentAlivePlayer > 1) return false;
        }
        return true;
    }
}
