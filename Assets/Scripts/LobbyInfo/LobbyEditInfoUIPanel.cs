using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyEditInfoUIPanel : Screen
{
    public static LobbyInfoUI Instance;

    [field: SerializeField] Button saveButton;

    [field: SerializeField] TMP_Text lobbyIdDisplayText;
    [field: SerializeField] TMP_InputField lobbyNameInputField;
    [field: SerializeField] TMP_Text lobbyStatusDisplayText;
    [field: SerializeField] Toggle lobbyIsPrivateToggle;
    [field: SerializeField] TMP_Dropdown lobbyGameModeDropDown;
    [field: SerializeField] TMP_Dropdown lobbyGameMapDropDown;

    void Awake()
    {
        saveButton.onClick.AddListener(SaveInfo);
        
        lobbyGameModeDropDown.options.Clear();
        foreach (GameModeData gameModeData in GameModeDataHelper.Instance.MapsData.gameModeDatas)
        {
            lobbyGameModeDropDown.options.Add(new TMP_Dropdown.OptionData(gameModeData.GameModeName));
        }
        lobbyGameModeDropDown.onValueChanged.AddListener(OnGameModeDropDownValueChanged);

    }

    private async void SaveInfo()
    {
        UpdateLobbyOptions options = new UpdateLobbyOptions()
        {
            Name = lobbyNameInputField.text,
            IsPrivate = lobbyIsPrivateToggle.isOn,
            Data = new Dictionary<string, DataObject>()
            {
                {
                    LobbyDataField.GameMode.ToString(), new DataObject(
                        visibility: DataObject.VisibilityOptions.Public,
                        value: lobbyGameModeDropDown.options[lobbyGameModeDropDown.value].text)
                },
                {
                    LobbyDataField.GameMap.ToString(), new DataObject(
                        visibility: DataObject.VisibilityOptions.Public,
                        value: lobbyGameMapDropDown.options[lobbyGameMapDropDown.value].text)
                },
            }
        };
        await UnityLobbyServiceManager.Instance.UpdateLobbyData(options);
        LobbyInfoUI.Instance.Show();
        Hide();
    }

    void OnDestroy()
    {
        saveButton.onClick.RemoveAllListeners();
    }

    public override void UpdateScreen()
    {
        Lobby joinedLobby = LobbyInfoUI.Instance.joinedLobby;
        lobbyIdDisplayText.text = joinedLobby.Id;
        lobbyNameInputField.text = joinedLobby.Name;
        lobbyStatusDisplayText.text = joinedLobby.Data[LobbyDataField.Status.ToString()].Value;
        lobbyIsPrivateToggle.isOn = joinedLobby.IsPrivate;

        lobbyGameModeDropDown.options.Clear();
        foreach (GameModeData gameModeData in GameModeDataHelper.Instance.MapsData.gameModeDatas)
        {
            lobbyGameModeDropDown.options.Add(new TMP_Dropdown.OptionData(gameModeData.GameModeName));
        }
        if (lobbyGameModeDropDown.options.Any(x => x.text == joinedLobby.Data[LobbyDataField.GameMode.ToString()].Value))
        {
            TMP_Dropdown.OptionData data = lobbyGameModeDropDown.options.First(x => x.text == joinedLobby.Data[LobbyDataField.GameMode.ToString()].Value);
            lobbyGameModeDropDown.value = lobbyGameModeDropDown.options.IndexOf(data);
        }
        else
        {
            lobbyGameModeDropDown.value = 0;
        }

        lobbyGameMapDropDown.options.Clear();
        foreach (string scene in GameModeDataHelper.Instance.MapsData.gameModeDatas[lobbyGameModeDropDown.value].AvailableScene)
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

    private void OnGameModeDropDownValueChanged(int arg0)
    {
        lobbyGameMapDropDown.options.Clear();
        foreach (string scene in GameModeDataHelper.Instance.MapsData.gameModeDatas[arg0].AvailableScene)
        {
            lobbyGameMapDropDown.options.Add(new TMP_Dropdown.OptionData(scene));
        }
        lobbyGameMapDropDown.value = 0;
    }
}

