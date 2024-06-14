using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public static Test Instance;

    [field: SerializeField] Button startButton;
    [field: SerializeField] Button joinButton;
    [field: SerializeField] Button exitButton;

    [field: SerializeField] GameObject lobbyBrowser;
    [field: SerializeField] GameObject lobbyInfo;

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
        startButton.onClick.AddListener(async () => {
            await LobbyManager.Instance.CreateAndHostLobby();
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

    public void JoinLobby(Lobby lobby)
    {
        if (lobby != null)
            LobbyManager.Instance.JoinLobby(lobby);
        NetworkManager.Singleton.StartClient();

        lobbyBrowser.SetActive(false);
        lobbyInfo.SetActive(true);
    }

    public async void HostLobby()
    {
        NetworkManager.Singleton.StartHost();
        await LobbyManager.Instance.CreateAndHostLobby();

        lobbyBrowser.SetActive(false);
        lobbyInfo.SetActive(true);
    }

    public void ExitLobby()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) 
            LobbyManager.Instance.DeleteLobby();
        NetworkManager.Singleton.Shutdown();

        lobbyBrowser.SetActive(true);
        lobbyInfo.SetActive(false);
    }
}
