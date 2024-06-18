using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLobbyInfoUIItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI PlayerNameText, PlayerHostStatusText, PlayerLobbyStatusText;

    public void Initialize(Player player)
    {
        Debug.Log(player.Data["Name"].Value + " | " + player.Data["Status"].Value);
        PlayerNameText.text = player.Data["Name"].Value;
        PlayerHostStatusText.text = player.Id == LobbyManager.Instance.joinedLobby.HostId ? "Host" : "Client";
        PlayerLobbyStatusText.text = player.Data["Status"].Value;
    }
}
