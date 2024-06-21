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
    }

    void LateUpdate()
    {
        if (gameObject.activeInHierarchy) UpdateScreen();
    }

    public override void UpdateScreen()
    {
        foreach (PlayerScoreBoardUIItem item in playerScoreBoardUIItemList)
        {
            Destroy(item.gameObject);
        }
        playerScoreBoardUIItemList.Clear();

        foreach (PlayerLevelInfo info in LevelManager.Instance.PlayerNetworkListToNormalList().OrderByDescending(x => x.playerScore))
        {
            PlayerScoreBoardUIItem InstantiateItem = Instantiate(playerScoreBoardUIItemPrefab, ScrollviewContent, false);
            InstantiateItem.Initialize(info);
            playerScoreBoardUIItemList.Add(InstantiateItem);
            Debug.Log("Item Added: " + LevelManager.Instance.PlayerLevelInfoNetworkList.Count);
        }
    }
}
