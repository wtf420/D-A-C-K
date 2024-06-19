using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LobbyInfoUI : MonoBehaviour
{
    public static LobbyInfoUI Instance;

    [field: SerializeField] Button startButton;
    [field: SerializeField] Button joinButton;
    [field: SerializeField] Button exitButton;

    [field: SerializeField] TMP_Text lobbyIdDisplayText;
    [field: SerializeField] TMP_Text lobbyNameTitleText;
    [field: SerializeField] TMP_InputField lobbyNameDisplayInputField;
    [field: SerializeField] Toggle lobbyIsPrivateToggle;

    [field: SerializeField] GameObject playerLobbyInfoScrollviewContent;
    [field: SerializeField] PlayerLobbyInfoUIItem playerLobbyInfoUIItemPrefab;

    public Lobby joinedLobby {
        get { return UnityLobbyServiceManager.Instance.joinedLobby; }
        set { UnityLobbyServiceManager.Instance.joinedLobby = value; }
    }
    List<PlayerLobbyInfoUIItem> playerLobbyInfoUIItemList;

    void Awake()
    {
        if (Instance)
            Destroy(this.gameObject);
        else
            Instance = this;

        playerLobbyInfoUIItemList = new List<PlayerLobbyInfoUIItem>();

        joinButton.onClick.AddListener(() => LobbyManager.Instance.JoinGame());
        startButton.onClick.AddListener(() => LobbyManager.Instance.StartGame());
        exitButton.onClick.AddListener(ExitLobby);

        lobbyNameDisplayInputField.onSubmit.AddListener(OnLobbyNameSubmit);
        lobbyIsPrivateToggle.onValueChanged.AddListener(OnLobbyIsPrivateToggle);
    }

    private void OnLobbyIsPrivateToggle(bool arg0)
    {
        if (!UnityLobbyServiceManager.Instance.isHost) return;
        UpdateLobbyOptions options = new UpdateLobbyOptions()
        {
            IsPrivate = arg0
        };
        _ = UnityLobbyServiceManager.Instance.UpdateLobbyData(options);
        UpdateLobbyAsync();
    }

    private void OnLobbyNameSubmit(string arg0)
    {
        if (!UnityLobbyServiceManager.Instance.isHost) return;
        UpdateLobbyOptions options = new UpdateLobbyOptions(){
            Name = arg0
        };
        _ = UnityLobbyServiceManager.Instance.UpdateLobbyData(options);
        UpdateLobbyAsync();
    }

    void OnEnable()
    {
        UpdateLobbyAsync();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            UpdateLobbyAsync();
        }
    }

    void OnDestroy()
    {
        startButton.onClick.RemoveAllListeners();
        joinButton.onClick.RemoveAllListeners();
        exitButton.onClick.RemoveAllListeners();
    }

    public async void UpdateLobbyAsync()
    {
        await UnityLobbyServiceManager.Instance.PollForLobbyUpdates();
        Debug.Log("UpdateLobby: " + joinedLobby.Players.Count);
        foreach (PlayerLobbyInfoUIItem item in playerLobbyInfoUIItemList)
        {
            Destroy(item.gameObject);
        }
        playerLobbyInfoUIItemList.Clear();

        lobbyIdDisplayText.text = joinedLobby.Id;
        lobbyNameDisplayInputField.text = joinedLobby.Name;
        if (UnityLobbyServiceManager.Instance.isHost) lobbyNameDisplayInputField.interactable = true; else lobbyNameDisplayInputField.interactable = false;
        lobbyNameTitleText.text = joinedLobby.Name;
        lobbyIsPrivateToggle.isOn = joinedLobby.IsPrivate;
        if (UnityLobbyServiceManager.Instance.isHost) lobbyIsPrivateToggle.interactable = true; else lobbyIsPrivateToggle.interactable = false;
        if (joinedLobby.Players.Count == 0) return;
        foreach (Player player in joinedLobby.Players)
        {
            PlayerLobbyInfoUIItem InstantiateItem = Instantiate(playerLobbyInfoUIItemPrefab, playerLobbyInfoScrollviewContent.transform, false);
            InstantiateItem.Initialize(player);
            playerLobbyInfoUIItemList.Add(InstantiateItem);
        }
    }

    public void ExitLobby()
    {
        MainMenuUI.Instance.ExitLobby();
    }
}
