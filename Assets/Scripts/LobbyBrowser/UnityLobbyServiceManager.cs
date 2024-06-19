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
using UnityEngine.Events;

// For managing Lobby from Unity Lobby Package
public class UnityLobbyServiceManager : MonoBehaviour
{
    public static UnityLobbyServiceManager Instance;
    public bool isHost = false;
    public Lobby joinedLobby = null;
    public UnityEvent OnLobbyChangedEvent, OnLobbyDeletedEvent, OnKickedFromLobbyEvent;

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
        DontDestroyOnLoad(this);
        InnitializeUnityAuthentication();
    }

    void OnApplicationQuit()
    {
        _ = LeaveLobby();
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
                            "Name", new PlayerDataObject(
                                visibility: PlayerDataObject.VisibilityOptions.Member, // Visible only to members of the lobby.
                                value: PersistentPlayer.Instance.playerData.PlayerName)
                        },
                        {
                            "Status", new PlayerDataObject(
                                visibility: PlayerDataObject.VisibilityOptions.Member, // Visible only to members of the lobby.
                                value: "InLobby")
                        },
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
            var callbacks = new LobbyEventCallbacks();
            callbacks.LobbyChanged += OnLobbyChanged;
            callbacks.KickedFromLobby += OnKickedFromLobby;
            AddListenerToLobby(lobby, callbacks);
            Debug.Log(PersistentPlayer.Instance.playerData.PlayerName);
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

    public async Task<Lobby> JoinLobby(Lobby lobby)
    {
        try
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
            {
                // Ensure you sign-in before calling Authentication Instance.
                // See IAuthenticationService interface.
                Player = new Player(
                    id: AuthenticationService.Instance.PlayerId,
                    data: new Dictionary<string, PlayerDataObject>()
                    {
                        {
                            "Name", new PlayerDataObject(
                                visibility: PlayerDataObject.VisibilityOptions.Member, // Visible only to members of the lobby.
                                value: PersistentPlayer.Instance.playerData.PlayerName)
                        },
                        {
                            "Status", new PlayerDataObject(
                                visibility: PlayerDataObject.VisibilityOptions.Member, // Visible only to members of the lobby.
                                value: "InLobby")
                        },
                    })
            };
            Lobby resultLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, options);

            var callbacks = new LobbyEventCallbacks();
            callbacks.LobbyChanged += OnLobbyChanged;
            callbacks.KickedFromLobby += OnKickedFromLobby;
            AddListenerToLobby(lobby, callbacks);
            return resultLobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }

    public async Task<Lobby> JoinLobbyViaLobbyCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
            {
                // Ensure you sign-in before calling Authentication Instance.
                // See IAuthenticationService interface.
                Player = new Player(
                    id: AuthenticationService.Instance.PlayerId,
                    data: new Dictionary<string, PlayerDataObject>()
                    {
                        {
                            "Name", new PlayerDataObject(
                                visibility: PlayerDataObject.VisibilityOptions.Member, // Visible only to members of the lobby.
                                value: PersistentPlayer.Instance.playerData.PlayerName)
                        },
                        {
                            "Status", new PlayerDataObject(
                                visibility: PlayerDataObject.VisibilityOptions.Member, // Visible only to members of the lobby.
                                value: "InLobby")
                        },
                    })
            };
            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
            var callbacks = new LobbyEventCallbacks();
            callbacks.LobbyChanged += OnLobbyChanged;
            callbacks.KickedFromLobby += OnKickedFromLobby;
            AddListenerToLobby(lobby, callbacks);
            return lobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            return null;
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

    public async void AddListenerToLobby(Lobby lobby, LobbyEventCallbacks lobbyEventCallbacks)
    {
        try
        {
            ILobbyEvents lobbyChanges = await Lobbies.Instance.SubscribeToLobbyEventsAsync(lobby.Id, lobbyEventCallbacks);
            Debug.Log("Added callback successful");
        }
        catch (LobbyServiceException ex)
        {
            switch (ex.Reason)
            {
                case LobbyExceptionReason.AlreadySubscribedToLobby: Debug.LogWarning($"Already subscribed to lobby[{lobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); break;
                case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Debug.LogError($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); throw;
                case LobbyExceptionReason.LobbyEventServiceConnectionError: Debug.LogError($"Failed to connect to lobby events. Exception Message: {ex.Message}"); throw;
                default: throw;
            }
        }
    }

    public async Task LeaveLobby()
    {
        string playerId = AuthenticationService.Instance.PlayerId;
        await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
        joinedLobby = null;
    }

    public async Task RemovePlayerFromLobby(string playerId)
    {
        if (isHost)
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
        Debug.Log("Removed!");
    }

    public async Task UpdatePlayerData(UpdatePlayerOptions options)
    {
        try
        {
            var lobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, options);

            //...
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async Task UpdateLobbyData(UpdateLobbyOptions options)
    {
        try
        {
            var lobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, options);
            //...
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public IEnumerator HeartbeatLobbyCoroutine()
    {
        yield return new WaitUntil(() => joinedLobby != null);
        var delay = new WaitForSecondsRealtime(10f);

        while (true)
        {
            yield return delay;
            //yield return PollForLobbyUpdates();
            LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            Debug.Log("HeartbeatLobby");
        }
    }

    public async Task PollForLobbyUpdates()
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
            joinedLobby = lobby;
            isHost = GetClientId() == joinedLobby.HostId;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private void OnKickedFromLobby()
    {
        Debug.Log("OnKickedFromLobby");
        OnKickedFromLobbyEvent?.Invoke();
    }

    private void OnLobbyChanged(ILobbyChanges changes)
    {
        Debug.Log("OnLobbyChanged: " + changes);
        if (changes.LobbyDeleted)
        {
            joinedLobby = null;
            OnLobbyDeletedEvent.Invoke();
        }
        else
        {
            changes.ApplyToLobby(joinedLobby);
            OnLobbyChangedEvent.Invoke();
        }
    }

    public string GetClientId()
    {
        return AuthenticationService.Instance.PlayerId;
    }

    public bool GetIsHost(string playerId)
    {
        isHost = playerId == joinedLobby.HostId;
        return isHost;
    }
}
