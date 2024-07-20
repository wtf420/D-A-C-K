using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LastManStandingScoreBoard : Screen
{
    [SerializeField] LastManStandingGameMode gameMode;
    [SerializeField] LastManStandingScoreBoardUIItem lastManStandingScoreBoardUIItem;
    [SerializeField] Transform ScrollviewContent;
    [SerializeField] List<LastManStandingScoreBoardUIItem> lastManStandingScoreBoardUIItemList = new List<LastManStandingScoreBoardUIItem>();

    public void OnDestroy()
    {
        foreach (LastManStandingScoreBoardUIItem item in lastManStandingScoreBoardUIItemList)
        {
            gameMode.OnPlayerDeathEvent.RemoveListener((ulong clientId) => item.ManualUpdate());
            gameMode.OnPlayerKillEvent.RemoveListener((ulong clientId, ulong victimId) => item.ManualUpdate());
            gameMode.OnPlayerSpawnEvent.RemoveListener((ulong clientId) => item.ManualUpdate());
            Destroy(item.gameObject);
        }
    }

    public void UpdateInfo(ulong clientId)
    {
        if (lastManStandingScoreBoardUIItemList.Any(x => x.info.clientId == clientId))
        {
            lastManStandingScoreBoardUIItemList.First(x => x.info.clientId == clientId).ManualUpdate();
        }
        else
        {
            UpdateScreen();
        }
    }

    public override void UpdateScreen()
    {
        List<CustomLMSGameModePlayerInfo> infoList = gameMode.CustomLMSGameModePlayerInfoNormalList;
        foreach (LastManStandingScoreBoardUIItem item in lastManStandingScoreBoardUIItemList)
        {
            gameMode.OnPlayerDeathEvent.RemoveListener((ulong clientId) => item.ManualUpdate());
            gameMode.OnPlayerKillEvent.RemoveListener((ulong clientId, ulong victimId) => item.ManualUpdate());
            gameMode.OnPlayerSpawnEvent.RemoveListener((ulong clientId) => item.ManualUpdate());
            Destroy(item.gameObject);
        }
        lastManStandingScoreBoardUIItemList.Clear();

        foreach (CustomLMSGameModePlayerInfo info in infoList.OrderByDescending(x => x.playerLives))
        {
            LastManStandingScoreBoardUIItem InstantiateItem = Instantiate(lastManStandingScoreBoardUIItem, ScrollviewContent, false);
            InstantiateItem.Initialize(info);
            lastManStandingScoreBoardUIItemList.Add(InstantiateItem);
            Debug.Log("Item Added: " + NetworkPlayersManager.Instance.NetworkPlayerInfoNetworkList.Count);

            gameMode.OnPlayerDeathEvent.AddListener((ulong clientId) => InstantiateItem.ManualUpdate());
            gameMode.OnPlayerKillEvent.AddListener((ulong clientId, ulong victimId) => InstantiateItem.ManualUpdate());
            gameMode.OnPlayerSpawnEvent.AddListener((ulong clientId) => InstantiateItem.ManualUpdate());
        }
    }
}
