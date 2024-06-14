using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;

public class LobbyBrowser : MonoBehaviour
{
    [SerializeField] LobbyInfoUIItem LobbyInfoUIItemPrefab;
    [SerializeField] Button RefreshButton, CreateLobbyButton;
    [SerializeField] GameObject ScrollviewContent;

    List<LobbyInfoUIItem> lobbyInfoUIItemList;
    List<Lobby> lobbies;

    // Start is called before the first frame update
    void Start()
    {
        RefreshButton.onClick.AddListener(UpdateLobbbyList);
        CreateLobbyButton.onClick.AddListener(HostNewLobby);
        lobbyInfoUIItemList = new List<LobbyInfoUIItem>();
        lobbies = new List<Lobby>();
    }

    void OnDestroy()
    {
        RefreshButton.onClick.RemoveAllListeners();
        CreateLobbyButton.onClick.RemoveAllListeners();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HostNewLobby()
    {
        Test.Instance.HostLobby();
        UpdateLobbbyList();
    }

    public async void UpdateLobbbyList()
    {
        lobbies = await LobbyManager.Instance.GetAllLobbies();
        foreach (LobbyInfoUIItem lobbyInfoUIItem in lobbyInfoUIItemList)
        {
            Destroy(lobbyInfoUIItem.gameObject);
        }
        lobbyInfoUIItemList.Clear();

        if (lobbies.Count == 0) return;
        foreach (Lobby lobby in lobbies)
        {
            LobbyInfoUIItem InstantiateItem = Instantiate(LobbyInfoUIItemPrefab, ScrollviewContent.transform, false);
            InstantiateItem.Initialize(lobby);
            lobbyInfoUIItemList.Add(InstantiateItem);
        }
    }
}
