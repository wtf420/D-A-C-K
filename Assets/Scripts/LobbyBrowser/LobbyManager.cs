using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using UnityEngine.UI;

// For managing Lobby from Unity Lobby Package
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;
    public Lobby joinedLobby = null;

    void Awake()
    {
        if (Instance == null)
        {
            if (Instance) Destroy(Instance.gameObject);
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this);

        InnitializeUnityAuthentication();
    }

    void OnDestroy()
    {
        if (joinedLobby != null)
        {
            DeleteLobby();
        }
    }

    async void InnitializeUnityAuthentication()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(Random.Range(0, 10000).ToString());
            await UnityServices.InitializeAsync(initializationOptions);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Unity Authentication Successful");
        }
    }

    public async Task<Lobby> CreateAndHostLobby()
    {
        try
        {
            string lobbyName = "New lobby";
            int maxPlayers = 8;
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                // Ensure you sign-in before calling Authentication Instance.
                // See IAuthenticationService interface.
                Player = new Player(
                    id: AuthenticationService.Instance.PlayerId,
                    data: new Dictionary<string, PlayerDataObject>()
                    {
                        {
                            "ExampleMemberPlayerData", new PlayerDataObject(
                                visibility: PlayerDataObject.VisibilityOptions.Member, // Visible only to members of the lobby.
                                value: "ExampleMemberPlayerData")
                        }
                }),
                Data = new Dictionary<string, DataObject>()
                {
                    {
                        "IP Address", new DataObject(
                            visibility: DataObject.VisibilityOptions.Public,
                            value: NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address)
                    },
                    {
                        "Status", new DataObject(
                            visibility: DataObject.VisibilityOptions.Public,
                            value: "InLobby",
                            index: DataObject.IndexOptions.S1)
                    },
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            joinedLobby = lobby;
            //NetworkManager.Singleton.StartHost();
            //NetworkManager.Singleton.SceneManager.LoadScene("TestingLevel", UnityEngine.SceneManagement.LoadSceneMode.Single);
            Debug.Log("Lobby created successfully!");
            return lobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }

    public async Task<List<Lobby>> GetAllLobbies()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();

            QueryResponse lobbies = await Lobbies.Instance.QueryLobbiesAsync(options);

            if (lobbies.Results.Count > 0)
            {
                Debug.Log("Lobbies found: " + lobbies.Results);
                foreach (Lobby lobby in lobbies.Results)
                {
                    Debug.Log("ID: " + lobby.Id + "| Name: " + lobby.Name + "| isPrivate: " + lobby.IsPrivate + "| Players: " + lobby.Players.Count + "/" + lobby.MaxPlayers);
                }
            }
            else
            {
                Debug.Log("No lobbies found");
            }
            return lobbies.Results;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }

    public async void JoinLobby(Lobby lobby)
    {
        if (joinedLobby == null && lobby == null)
        {
            try
            {
                await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void DeleteLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
                Debug.Log("Lobby Deleted successfully!");
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    // public void JoinGameFromLobby(Lobby lobby)
    // {
    //     //THIS IS PLACE HOLDER
    //     joinedLobby = lobby;
    //     NetworkManager.Singleton.StartHost();
    //     NetworkManager.Singleton.SceneManager.LoadScene("TestingLevel", UnityEngine.SceneManagement.LoadSceneMode.Single);
    // }

    public async void JoinLobbyViaLobbyCode(string lobbyCode)
    {
        if (joinedLobby == null)
        {
            try
            {
                await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public IEnumerator HeartbeatLobbyCoroutine(float waitTimeSeconds)
    {
        yield return new WaitUntil(() => joinedLobby != null);
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            Debug.Log("HeartbeatLobby");
            yield return delay;
        }
    }
}
