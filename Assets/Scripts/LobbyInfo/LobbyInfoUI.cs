using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyInfoUI : Screen
{
    public static LobbyInfoUI Instance;

    [field: SerializeField] Button startButton;
    [field: SerializeField] Button joinButton;
    [field: SerializeField] Button exitButton;
    [field: SerializeField] Button editLobbyButton;

    [field: SerializeField] TMP_Text lobbyNameTitleText;

    [field: SerializeField] TMP_Text lobbyIdDisplayText;
    [field: SerializeField] TMP_Text lobbyNameDisplayText;
    [field: SerializeField] TMP_Text lobbyStatusDisplayText;
    [field: SerializeField] Toggle lobbyIsPrivateToggle;
    [field: SerializeField] TMP_Dropdown lobbyGameModeDropDown;
    [field: SerializeField] TMP_Dropdown lobbyGameMapDropDown;

    [field: SerializeField] GameObject playerLobbyInfoScrollviewContent;
    [field: SerializeField] LobbyEditInfoUIPanel lobbyEditInfoUIPanel;
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
        editLobbyButton.onClick.AddListener(() => lobbyEditInfoUIPanel.Show());

        lobbyGameModeDropDown.options.Clear();
        foreach (GameModeData gameModeData in GameModeDataHelper.Instance.MapsData.gameModeDatas)
        {
            lobbyGameModeDropDown.options.Add(new TMP_Dropdown.OptionData(gameModeData.GameModeName));
        }
        lobbyGameModeDropDown.onValueChanged.AddListener(OnGameModeDropDownValueChanged);

        // lobbyNameDisplayInputField.onSubmit.AddListener(OnLobbyNameSubmit);
        //lobbyIsPrivateToggle.onValueChanged.AddListener(OnLobbyIsPrivateToggle);
    }

    private void OnGameModeDropDownValueChanged(int arg0)
    {
        lobbyGameMapDropDown.options.Clear();
        foreach (string scene in GameModeDataHelper.Instance.MapsData.gameModeDatas[arg0].AvailableScene)
        {
            lobbyGameMapDropDown.options.Add(new TMP_Dropdown.OptionData(scene));
        }
        if (lobbyGameMapDropDown.options.Any(x => x.text == joinedLobby.Data[LobbyDataField.GameMap.ToString()].Value))
        {
            TMP_Dropdown.OptionData data = lobbyGameMapDropDown.options.First(x => x.text == joinedLobby.Data[LobbyDataField.GameMap.ToString()].Value);
            lobbyGameMapDropDown.value = lobbyGameModeDropDown.options.IndexOf(data);
        }
        else
        {
            lobbyGameMapDropDown.value = 0;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            UpdateScreen();
        }
    }

    void OnDestroy()
    {
        startButton.onClick.RemoveAllListeners();
        joinButton.onClick.RemoveAllListeners();
        exitButton.onClick.RemoveAllListeners();
        editLobbyButton.onClick.RemoveAllListeners();
    }

    public override void Show()
    {
        base.Show();
    }

    public async override void UpdateScreen()
    {
        if (LobbyManager.Instance.joinedLobby == null) return;
        await UnityLobbyServiceManager.Instance.PollForLobbyUpdates();
        Debug.Log("UpdateLobby: " + joinedLobby.Players.Count);
        foreach (PlayerLobbyInfoUIItem item in playerLobbyInfoUIItemList)
        {
            Destroy(item.gameObject);
        }
        playerLobbyInfoUIItemList.Clear();

        lobbyNameTitleText.text = joinedLobby.Name;
        lobbyIdDisplayText.text = joinedLobby.Id;
        lobbyNameDisplayText.text = joinedLobby.Name;
        lobbyStatusDisplayText.text = joinedLobby.Data[LobbyDataField.Status.ToString()].Value;
        lobbyIsPrivateToggle.isOn = joinedLobby.IsPrivate;

        if (lobbyGameModeDropDown.options.Any(x => x.text == joinedLobby.Data[LobbyDataField.GameMode.ToString()].Value))
        {
            TMP_Dropdown.OptionData data = lobbyGameModeDropDown.options.First(x => x.text == joinedLobby.Data[LobbyDataField.GameMode.ToString()].Value);
            lobbyGameModeDropDown.value = lobbyGameModeDropDown.options.IndexOf(data);
        }
        else
        {
            lobbyGameModeDropDown.value = 0;
        }
        OnGameModeDropDownValueChanged(lobbyGameModeDropDown.value);

        // if (Enum.TryParse(joinedLobby.Data[LobbyDataField.GameMode.ToString()].Value, out AvailableGameMode gameMode))
        // {
        //     lobbyGameModeDropDown.value = (short)gameMode;
        // }
        // else
        // {
        //     lobbyGameModeDropDown.value = 0;
        // }

        editLobbyButton.gameObject.SetActive(UnityLobbyServiceManager.Instance.isHost);
        startButton.gameObject.SetActive(UnityLobbyServiceManager.Instance.isHost);
        joinButton.gameObject.SetActive(joinedLobby.Data[LobbyDataField.Status.ToString()].Value == LobbyStatusDataValue.InGame.ToString());
        
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
