using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;
    Lobby joinedLobby = null;

    [SerializeField] GameObject lobbyBrowser;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } else
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

    }

    async void InnitializeUnityAuthentication()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(Random.Range(0, 10000).ToString());
            await UnityServices.InitializeAsync(initializationOptions);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        lobbyBrowser.gameObject.SetActive(true);
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
                })
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
            } else
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

    public void JoinGameFromLobby(Lobby lobby)
    {
        //THIS IS PLACE HOLDER
        joinedLobby = lobby;
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("TestingLevel", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

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
}
