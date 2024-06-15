using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public static Test Instance;

    [field: SerializeField] GameObject lobbyBrowser;
    [field: SerializeField] GameObject lobbyInfo;

    void Awake()
    {
        if (Instance)
            Destroy(this.gameObject);
        else
            Instance = this;
    }

    public void JoinLobby(Lobby lobby)
    {
        if (lobby != null)
            UnityLobbyServiceManager.Instance.JoinLobby(lobby);
        NetworkManager.Singleton.StartClient();
        NavigateToLobbyInfo();
    }

    public async void HostLobby()
    {
        NetworkManager.Singleton.StartHost();
        await UnityLobbyServiceManager.Instance.CreateAndHostLobby();
        NavigateToLobbyInfo();
    }

    // Placeholder for testing, redesign this flow later
    public void NavigateToLobbyBrowser()
    {
        lobbyBrowser.SetActive(true);
        lobbyInfo.SetActive(false);
    }

    // Placeholder for testing, redesign this flow later
    public void NavigateToLobbyInfo()
    {
        lobbyBrowser.SetActive(false);
        lobbyInfo.SetActive(true);
        LobbyInfoUI.Instance.UpdateLobby(CurrentLobbyInfo.Instance);
    }
}
