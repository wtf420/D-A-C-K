using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerScoreBoardUIItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI LobbyNameText, PlayerHostStatusText, PlayerPerformanceText, PlayerStatusText;
    PlayerLevelInfo info;

    public void Initialize(PlayerLevelInfo playerInfo)
    {
        LobbyNameText.text = playerInfo.playerName.ToString();
        PlayerHostStatusText.text = playerInfo.clientId == NetworkManager.ServerClientId ? "Host" : "Client";
        PlayerPerformanceText.text = playerInfo.playerScore.ToString();
        PlayerStatusText.text = ((PlayerStatus)playerInfo.playerStatus).ToString();
        info = playerInfo;
    }

    public void ManualUpdate()
    {
        if (!info.Equals(default))
        {
            LobbyNameText.text = info.playerName.ToString();
            PlayerHostStatusText.text = info.clientId == NetworkManager.ServerClientId ? "Host" : "Client";
            PlayerPerformanceText.text = info.playerScore.ToString();
            PlayerStatusText.text = ((PlayerStatus)info.playerStatus).ToString();
        }
    }
}
