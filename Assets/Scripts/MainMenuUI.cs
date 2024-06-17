
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public static MainMenuUI Instance;

    [field: SerializeField] PlayerDataInitializer playerDataInitializer;
    [field: SerializeField] LobbyBrowser lobbyBrowser;
    [field: SerializeField] LobbyInfoUI lobbyInfo;

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
        await LobbyManager.Instance.JoinLobby(lobby);
        AddListenerToLobby();
        NavigateToLobbyInfo();
    }

    public async void HostLobby()
    {
        await LobbyManager.Instance.HostLobby();
        AddListenerToLobby();
        NavigateToLobbyInfo();
    }

    public async void ExitLobby()
    {
        await LobbyManager.Instance.ExitLobby();
        RemoveListenerFromLobby();
        NavigateToLobbyBrowser();
    }

    public void HideAllMenu()
    {
        playerDataInitializer.gameObject.SetActive(false);
        lobbyBrowser.gameObject.SetActive(false);
        lobbyInfo.gameObject.SetActive(false);
    }

    // Placeholder for LobbyManagering, redesign this flow later
    public void NavigateToLobbyBrowser()
    {
        HideAllMenu();
        lobbyBrowser.gameObject.SetActive(true);
    }

    // Placeholder for LobbyManagering, redesign this flow later
    public void NavigateToLobbyInfo()
    {
        HideAllMenu();
        lobbyInfo.gameObject.SetActive(true);
    }

    void AddListenerToLobby()
    {
        UnityLobbyServiceManager.Instance.OnKickedFromLobbyEvent.RemoveListener(OnKickedFromLobby);
        UnityLobbyServiceManager.Instance.OnLobbyDeletedEvent.RemoveListener(OnKickedFromLobby);
    }

    void RemoveListenerFromLobby()
    {
        UnityLobbyServiceManager.Instance.OnKickedFromLobbyEvent.RemoveListener(OnKickedFromLobby);
        UnityLobbyServiceManager.Instance.OnLobbyDeletedEvent.RemoveListener(OnKickedFromLobby);
    }

    void OnKickedFromLobby()
    {
        NavigateToLobbyBrowser();
    }
}
