using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class PlayerScoreBoardUIItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI LobbyNameText, PlayerHostStatusText, PlayerPerformanceText, PlayerStatusText;

    public void Initialize(PlayerLevelInfo playerInfo)
    {
        LobbyNameText.text = playerInfo.playerName.ToString();
        PlayerHostStatusText.text = playerInfo.clientId == NetworkManager.ServerClientId ? "Host" : "Client";
        PlayerPerformanceText.text = playerInfo.playerScore.ToString();
        PlayerStatusText.text = playerInfo.character.TryGet(out ThirdPersonController character) ? "Alive" : "Dead";
    }
}
