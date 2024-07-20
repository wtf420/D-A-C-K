using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FirstToWinScoreBoard : Screen
{
    [SerializeField] FirstToWinGameMode gameMode;
    [SerializeField] FirstToWinScoreBoardUIItem firstToWinScoreBoardUIItem;
    [SerializeField] Transform ScrollviewContent;
    [SerializeField] List<FirstToWinScoreBoardUIItem> firstToWinScoreBoardUIItemList = new List<FirstToWinScoreBoardUIItem>();

    public void UpdateInfo(ulong clientId)
    {
        if (firstToWinScoreBoardUIItemList.Any(x => x.info.clientId == clientId))
        {
            firstToWinScoreBoardUIItemList.First(x => x.info.clientId == clientId).ManualUpdate();
        } else
        {
            UpdateScreen();
        }
    }

    public override void UpdateScreen()
    {
        List<CustomFTWGameModePlayerInfo> infoList = gameMode.CustomFTWGameModePlayerInfoNormalList;
        foreach (FirstToWinScoreBoardUIItem item in firstToWinScoreBoardUIItemList)
        {
            Destroy(item.gameObject);
        }
        firstToWinScoreBoardUIItemList.Clear();

        foreach (CustomFTWGameModePlayerInfo info in infoList.OrderByDescending(x => x.playerScore))
        {
            FirstToWinScoreBoardUIItem InstantiateItem = Instantiate(firstToWinScoreBoardUIItem, ScrollviewContent, false);
            InstantiateItem.Initialize(info);
            firstToWinScoreBoardUIItemList.Add(InstantiateItem);
            Debug.Log("Item Added: " + NetworkPlayersManager.Instance.NetworkPlayerInfoNetworkList.Count);
        }
    }
}
