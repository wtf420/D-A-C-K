using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLobbyInfoUIItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI LobbyNameText, PlayerHostStatusText, PlayerLobbyStatusText;
    [SerializeField] Button kickFromLobbyButton;

    public void Initialize(PlayerLobbyInfo playerInfo)
    {
        LobbyNameText.text = playerInfo.GetNetworkPlayer().playerName.Value.ToString();
        PlayerHostStatusText.text = playerInfo.IsHost ? "Host" : "";
        PlayerLobbyStatusText.text = playerInfo.playerLobbyStatus.ToString();
        //kickFromLobbyButton.onClick.AddListener(() => Test.Instance.JoinLobby(lobby));
    }

    // Start is called before the first frame update
    void OnDestroy()
    {
        //kickFromLobbyButton.onClick.RemoveAllListeners();
    }
}
