using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaitingForPlayersScreen : Screen
{
    [SerializeField] GameMode gameMode;
    [SerializeField] TMP_Text waitingForPlayersText;

    public override void UpdateScreen()
    {
        base.UpdateScreen();
        waitingForPlayersText.text = "Waiting for players. (" + NetworkPlayersManager.Instance.NetworkPlayerInfoNetworkList.Count + "/" + gameMode.miniumPlayerToStart + ")";
    }
}
