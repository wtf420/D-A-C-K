using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public static Test Instance;

    [field: SerializeField] LobbyBrowser lobbyBrowser;
    [field: SerializeField] LobbyInfoUI lobbyInfo;

    public Lobby joinedLobby { 
        get {
            return UnityLobbyServiceManager.Instance.joinedLobby;
        }
        set {
            UnityLobbyServiceManager.Instance.joinedLobby = value;
        }
    }

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
        await UnityLobbyServiceManager.Instance.LeaveLobby();
        NavigateToLobbyBrowser();
    }

    // Placeholder for testing, redesign this flow later
    public void NavigateToLobbyBrowser()
    {
        lobbyBrowser.gameObject.SetActive(true);
        lobbyInfo.gameObject.SetActive(false);
    }

    // Placeholder for testing, redesign this flow later
    public void NavigateToLobbyInfo()
    {
        lobbyBrowser.gameObject.SetActive(false);
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

    void OnLobbyChanged()
    {
        lobbyInfo.UpdateLobby();
    }
}
