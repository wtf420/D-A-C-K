using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LastManStandingGameOverScreen : Screen
{
    [SerializeField] LastManStandingGameMode gameMode;
    [SerializeField] TMP_Text gameOverText;

    public override void UpdateScreen()
    {
        base.UpdateScreen();
        NetworkPlayerInfo networkPlayerInfo = NetworkPlayersManager.Instance.GetNetworkPlayerInfoFromNetworkList(gameMode.winner.Value.clientId);
        gameOverText.text = "Game is over!\nWinner is: " + networkPlayerInfo.playerName.ToString();
    }
}