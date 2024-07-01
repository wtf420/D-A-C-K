using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScoreBoard : Screen
{
    [SerializeField] PlayerScoreBoardUIItem playerScoreBoardUIItemPrefab;
    [SerializeField] Transform ScrollviewContent;
    [SerializeField] List<PlayerScoreBoardUIItem> playerScoreBoardUIItemList;

    // Start is called before the first frame update
    void Awake()
    {
        playerScoreBoardUIItemList = new List<PlayerScoreBoardUIItem>();
        NetworkPlayersManager.Instance.OnNetworkPlayerInfoChangedEvent.AddListener(OnInfoChanged);
    }

    private void OnInfoChanged(NetworkPlayerInfo index)
    {
        // temporary fix, replace later
        UpdateScreen();
    }

    public override void UpdateScreen()
    {
        // foreach (PlayerScoreBoardUIItem item in playerScoreBoardUIItemList)
        // {
        //     Destroy(item.gameObject);
        // }
        // playerScoreBoardUIItemList.Clear();

        // foreach (NetworkPlayerInfo info in NetworkPlayersManager.Instance.PlayerNetworkListToNormalList().OrderByDescending(x => x.playerScore))
        // {
        //     PlayerScoreBoardUIItem InstantiateItem = Instantiate(playerScoreBoardUIItemPrefab, ScrollviewContent, false);
        //     InstantiateItem.Initialize(info);
        //     playerScoreBoardUIItemList.Add(InstantiateItem);
        //     Debug.Log("Item Added: " + NetworkPlayersManager.Instance.NetworkPlayerInfoNetworkList.Count);
        // }
    }
}
