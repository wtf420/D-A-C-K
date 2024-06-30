
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;
    Coroutine heartbeatCoroutine;

    public Lobby joinedLobby
    {
        get { return UnityLobbyServiceManager.Instance.joinedLobby; }
        set { UnityLobbyServiceManager.Instance.joinedLobby = value; }
    }

    public bool isHost => UnityLobbyServiceManager.Instance.isHost;

    void Awake()
    {
        if (Instance)
            Destroy(gameObject);
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
        heartbeatCoroutine = StartCoroutine(UnityLobbyServiceManager.Instance.HeartbeatLobbyCoroutine());
        AddListenerToLobby();
    }

    public async Task ExitLobby()
    {
        if (joinedLobby != null)
        {
            if (isHost) StopCoroutine(heartbeatCoroutine);
            await UnityLobbyServiceManager.Instance.LeaveLobby();
        }
        RemoveListenerFromLobby();
    }

    public void StartGame()
    {
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("TestingScene", LoadSceneMode.Single);

        UpdateLobbyOptions lobbyoptions = new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>()
        {
            {
                LobbyDataField.Status.ToString(), new DataObject(
                    visibility: DataObject.VisibilityOptions.Public,
                    value: LobbyStatusDataValue.InGame.ToString(),
                    index: DataObject.IndexOptions.S1)
            },
            {
                LobbyDataField.IPAddress.ToString(), new DataObject(
                    visibility: DataObject.VisibilityOptions.Public,
                    value: UnityLobbyServiceManager.Instance.GetIpAddress())
            },
        }
        };
        _ = UnityLobbyServiceManager.Instance.UpdateLobbyData(lobbyoptions);

        UpdatePlayerOptions updatePlayerOptions = new UpdatePlayerOptions
        {
            Data = new Dictionary<string, PlayerDataObject>()
        {
            {
                PlayerDataField.Status.ToString(), new PlayerDataObject(
                    visibility: PlayerDataObject.VisibilityOptions.Member,
                    value: LobbyStatusDataValue.InGame.ToString())
            },
        }
        };
        _ = UnityLobbyServiceManager.Instance.UpdatePlayerData(updatePlayerOptions);
    }

    public void JoinGame()
    {
        UpdatePlayerOptions updatePlayerOptions = new UpdatePlayerOptions
        {
            Data = new Dictionary<string, PlayerDataObject>()
        {
            {
                PlayerDataField.Status.ToString(), new PlayerDataObject(
                    visibility: PlayerDataObject.VisibilityOptions.Member,
                    value: LobbyStatusDataValue.InGame.ToString())
            },
        }
        };
        _ = UnityLobbyServiceManager.Instance.UpdatePlayerData(updatePlayerOptions);
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = joinedLobby.Data[LobbyDataField.IPAddress.ToString()].Value;
        Debug.Log("IPAddress: " + joinedLobby.Data[LobbyDataField.IPAddress.ToString()].Value);
        NetworkManager.Singleton.StartClient();
    }

    public async void ExitGame()
    {
        await ExitLobby();
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
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
        Debug.Log("OnLobbyChanged");
        await UnityLobbyServiceManager.Instance.PollForLobbyUpdates();
        if (isHost) StartCoroutine(UnityLobbyServiceManager.Instance.HeartbeatLobbyCoroutine());
        if (!isHost && joinedLobby.Data[PlayerDataField.Status.ToString()].Value == LobbyStatusDataValue.InGame.ToString()) JoinGame();
    }
}
