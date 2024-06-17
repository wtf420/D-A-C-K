using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyInfoUIItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI LobbyNameText, LobbyPlayerCountText, LobbyStatusText, LobbyTypeText;
    [SerializeField] Button joinLobbyButton;

    public void Initialize(Lobby lobby)
    {
        LobbyNameText.text = lobby.Name;
        LobbyPlayerCountText.text = lobby.Players.Count.ToString() + "/" + lobby.MaxPlayers.ToString();
        LobbyStatusText.text = lobby.Id;
        LobbyTypeText.text = lobby.IsPrivate.ToString();
        joinLobbyButton.onClick.AddListener(() => MainMenuUI.Instance.JoinLobby(lobby));
    }

    // Start is called before the first frame update
    void OnDestroy()
    {
        joinLobbyButton.onClick.RemoveAllListeners();
    }
}
