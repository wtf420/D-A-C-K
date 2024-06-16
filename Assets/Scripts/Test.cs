
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public static Test Instance;

    [field: SerializeField] PlayerDataInitializer playerDataInitializer;
    [field: SerializeField] LobbyBrowser lobbyBrowser;
    [field: SerializeField] LobbyInfoUI lobbyInfo;

    public Lobby joinedLobby
    {
        get { return UnityLobbyServiceManager.Instance.joinedLobby; }
        set { UnityLobbyServiceManager.Instance.joinedLobby = value; }
    }
    public bool isHost => UnityLobbyServiceManager.Instance.isHost;

    void Awake()
    {
        if (Instance)
            Destroy(this.gameObject);
        else
            Instance = this;
    }

    void OnDestroy()
    {
        RemoveListenerFromLobby();
    }

    public async void JoinLobby(Lobby lobby)
    {
        if (lobby != null)
            joinedLobby = await UnityLobbyServiceManager.Instance.JoinLobby(lobby);
        NavigateToLobbyInfo();
        AddListenerToLobby();
    }

    public async void HostLobby()
    {
        joinedLobby = await UnityLobbyServiceManager.Instance.CreateAndHostLobby();
        StartCoroutine(UnityLobbyServiceManager.Instance.HeartbeatLobbyCoroutine(10f));
        NavigateToLobbyInfo();
        AddListenerToLobby();
    }

    public async void ExitLobby()
    {
        if (isHost) StopCoroutine(UnityLobbyServiceManager.Instance.HeartbeatLobbyCoroutine(5f));
        await UnityLobbyServiceManager.Instance.LeaveLobby();
        NavigateToLobbyBrowser();
    }

    public void StartGame()
    {
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("TestingScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void JoinGame()
    {
        NetworkManager.Singleton.StartClient();
    }

    public void HideAllMenu()
    {
        playerDataInitializer.gameObject.SetActive(false);
        lobbyBrowser.gameObject.SetActive(false);
        lobbyInfo.gameObject.SetActive(false);
    }

    // Placeholder for testing, redesign this flow later
    public void NavigateToLobbyBrowser()
    {
        HideAllMenu();
        lobbyBrowser.gameObject.SetActive(true);
    }

    // Placeholder for testing, redesign this flow later
    public void NavigateToLobbyInfo()
    {
        HideAllMenu();
        lobbyInfo.gameObject.SetActive(true);
    }

    void AddListenerToLobby()
    {
        UnityLobbyServiceManager.Instance.OnLobbyChangedEvent.AddListener(OnLobbyChanged);
        UnityLobbyServiceManager.Instance.OnKickedFromLobbyEvent.AddListener(OnKickedFromLobby);
        UnityLobbyServiceManager.Instance.OnLobbyDeletedEvent.AddListener(OnKickedFromLobby);
    }

    void RemoveListenerFromLobby()
    {
        UnityLobbyServiceManager.Instance.OnLobbyChangedEvent.RemoveListener(OnLobbyChanged);
        UnityLobbyServiceManager.Instance.OnKickedFromLobbyEvent.RemoveListener(OnKickedFromLobby);
        UnityLobbyServiceManager.Instance.OnLobbyDeletedEvent.RemoveListener(OnKickedFromLobby);
    }

    void OnKickedFromLobby()
    {
        NavigateToLobbyBrowser();
    }

    async void OnLobbyChanged()
    {
        await lobbyInfo.UpdateLobbyAsync();
        if (isHost) StartCoroutine(UnityLobbyServiceManager.Instance.HeartbeatLobbyCoroutine(5f));
    }
}
