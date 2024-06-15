using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyInfoUI : MonoBehaviour
{
    public static LobbyInfoUI Instance;

    [field: SerializeField] Button startButton;
    [field: SerializeField] Button joinButton;
    [field: SerializeField] Button exitButton;

    [field: SerializeField] GameObject playerLobbyInfoScrollviewContent;
    [field: SerializeField] PlayerLobbyInfoUIItem playerLobbyInfoUIItemPrefab;

    CurrentLobbyInfo currentLobbyInfo;
    List<PlayerLobbyInfoUIItem> playerLobbyInfoUIItemList = new List<PlayerLobbyInfoUIItem>();

    void Awake()
    {
        if (Instance)
            Destroy(this.gameObject);
        else
            Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        startButton.onClick.AddListener(async () =>
        {
            await UnityLobbyServiceManager.Instance.CreateAndHostLobby();
            NetworkManager.Singleton.StartHost();
        });

        joinButton.onClick.AddListener(() => NetworkManager.Singleton.StartClient());

        exitButton.onClick.AddListener(ExitLobby);
    }

    void Update()
    {
        if (NetworkManager.Singleton.IsHost && Input.GetKeyDown(KeyCode.Space))
        {
            NetworkManager.Singleton.SceneManager.LoadScene("TestingScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    void OnDestroy()
    {
        startButton.onClick.RemoveAllListeners();
        joinButton.onClick.RemoveAllListeners();
        exitButton.onClick.RemoveAllListeners();
    }

    public void UpdateLobby(CurrentLobbyInfo currentLobbyInfo)
    {
        this.currentLobbyInfo = currentLobbyInfo;
        foreach (PlayerLobbyInfoUIItem item in playerLobbyInfoUIItemList)
        {
            Destroy(item);
        }
        playerLobbyInfoUIItemList.Clear();

        if (currentLobbyInfo.PlayerLobbyInfoLocalList.Count == 0) return;
        foreach (PlayerLobbyInfo player in currentLobbyInfo.PlayerLobbyInfoLocalList)
        {
            PlayerLobbyInfoUIItem InstantiateItem = Instantiate(playerLobbyInfoUIItemPrefab, playerLobbyInfoScrollviewContent.transform, false);
            InstantiateItem.Initialize(player);
            playerLobbyInfoUIItemList.Add(InstantiateItem);
        }
    }

    public void ExitLobby()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            UnityLobbyServiceManager.Instance.DeleteLobby();
        NetworkManager.Singleton.Shutdown();

        Test.Instance.NavigateToLobbyBrowser();
    }
}
