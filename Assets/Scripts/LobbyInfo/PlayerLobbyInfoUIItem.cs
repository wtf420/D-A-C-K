using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLobbyInfoUIItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI LobbyNameText, PlayerHostStatusText, PlayerLobbyStatusText;
    [SerializeField] Button kickFromLobbyButton;

    public void Initialize(Player player)
    {
        player.Data.TryGetValue("Name", out PlayerDataObject playerDataObject);
        LobbyNameText.text = playerDataObject.Value;

        PlayerHostStatusText.text = player.Id == Test.Instance.joinedLobby.HostId ? "Host" : "Client";

        player.Data.TryGetValue("Status", out playerDataObject);
        PlayerLobbyStatusText.text = playerDataObject.Value;

        if (UnityLobbyServiceManager.Instance.isHost && !UnityLobbyServiceManager.Instance.GetIsHost(player.Id))
        {
            kickFromLobbyButton.gameObject.SetActive(true);
            kickFromLobbyButton.onClick.AddListener(() => UnityLobbyServiceManager.Instance.RemovePlayerFromLobby(player.Id));
        } else
        {
            kickFromLobbyButton.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (kickFromLobbyButton.gameObject.activeInHierarchy)
        kickFromLobbyButton.onClick.RemoveAllListeners();
    }
}
