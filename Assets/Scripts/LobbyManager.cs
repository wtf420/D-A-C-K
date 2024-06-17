
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

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
        DontDestroyOnLoad(this);
    }

    void OnDestroy()
    {
        RemoveListenerFromLobby();
    }

    public async Task JoinLobby(Lobby lobby)
    {
        if (lobby != null)
            joinedLobby = await UnityLobbyServiceManager.Instance.JoinLobby(lobby);
        AddListenerToLobby();
    }

    public async Task HostLobby()
    {
        joinedLobby = await UnityLobbyServiceManager.Instance.CreateAndHostLobby();
        StartCoroutine(UnityLobbyServiceManager.Instance.HeartbeatLobbyCoroutine(10f));
        AddListenerToLobby();
    }

    public async Task ExitLobby()
    {
        if (isHost) StopCoroutine(UnityLobbyServiceManager.Instance.HeartbeatLobbyCoroutine(5f));
        await UnityLobbyServiceManager.Instance.LeaveLobby();
        RemoveListenerFromLobby();
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

    void AddListenerToLobby()
    {
        UnityLobbyServiceManager.Instance.OnLobbyChangedEvent.AddListener(OnLobbyChanged);
    }

    void RemoveListenerFromLobby()
    {
        UnityLobbyServiceManager.Instance.OnLobbyChangedEvent.RemoveListener(OnLobbyChanged);
    }

    async void OnLobbyChanged()
    {
        await UnityLobbyServiceManager.Instance.PollForLobbyUpdates();
        if (isHost) StartCoroutine(UnityLobbyServiceManager.Instance.HeartbeatLobbyCoroutine(5f));
    }
}
