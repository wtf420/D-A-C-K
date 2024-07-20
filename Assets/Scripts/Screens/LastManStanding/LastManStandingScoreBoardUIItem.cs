using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LastManStandingScoreBoardUIItem : MonoBehaviour
{
    [SerializeField] Image PlayerColorDisplay;
    [SerializeField] TextMeshProUGUI LobbyNameText, PlayerHostStatusText, PlayerPerformanceText, PlayerStatusText;
    public CustomLMSGameModePlayerInfo info;

    public void Initialize(CustomLMSGameModePlayerInfo info)
    {
        NetworkPlayerInfo networkPlayerInfo = NetworkPlayersManager.Instance.GetNetworkPlayerInfoFromNetworkList(info.clientId);
        string stringcolor = networkPlayerInfo.playerColor.ToString();
        if (ColorUtility.TryParseHtmlString(stringcolor, out Color color))
            PlayerColorDisplay.color = color;
        LobbyNameText.text = networkPlayerInfo.playerName.ToString();
        PlayerHostStatusText.text = networkPlayerInfo.clientId == NetworkManager.ServerClientId ? "Host" : "Client";
        PlayerPerformanceText.text = info.playerLives.ToString();
        PlayerStatusText.text = ((PlayerStatus)info.playerStatus).ToString();
        this.info = info;
    }

    public void ManualUpdate()
    {
        if (!info.Equals(default))
        {
            NetworkPlayerInfo networkPlayerInfo = NetworkPlayersManager.Instance.GetNetworkPlayerInfoFromNetworkList(info.clientId);
            string stringcolor = networkPlayerInfo.playerColor.ToString();
            if (ColorUtility.TryParseHtmlString(stringcolor, out Color color))
                PlayerColorDisplay.color = color;
            LobbyNameText.text = networkPlayerInfo.playerName.ToString();
            PlayerHostStatusText.text = info.clientId == NetworkManager.ServerClientId ? "Host" : "Client";
            PlayerPerformanceText.text = info.playerLives.ToString();
            PlayerStatusText.text = ((PlayerStatus)info.playerStatus).ToString();
        }
    }
}
