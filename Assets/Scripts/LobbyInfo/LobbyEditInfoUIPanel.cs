using System;
using System.Collections.Generic;
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

    void Awake()
    {
        saveButton.onClick.AddListener(SaveInfo);
    }

    private async void SaveInfo()
    {
        UpdateLobbyOptions options = new UpdateLobbyOptions()
        {
            Name = lobbyNameInputField.text,
            IsPrivate = lobbyIsPrivateToggle.isOn,
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
    }
}

