using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameOverScreen : Screen
{
    [SerializeField] TMP_Text gameOverText;

    public override void UpdateScreen()
    {
        base.UpdateScreen();
        gameOverText.text = "Game is over!\nWinner is: " + GamePlayManager.Instance.winner.Value.playerName.ToString();
    }
}