using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScoreBoard : MonoBehaviour
{
    [SerializeField] PlayerScoreBoardUIItem playerScoreBoardUIItemPrefab;
    [SerializeField] Transform ScrollviewContent;
    [SerializeField] List<PlayerScoreBoardUIItem> playerScoreBoardUIItemList;

    // Start is called before the first frame update
    void Awake()
    {
        playerScoreBoardUIItemList = new List<PlayerScoreBoardUIItem>();
    }

    void OnEnable()
    {
        UpdateScoreBoard();
    }

    void Update()
    {
        foreach (PlayerScoreBoardUIItem item in playerScoreBoardUIItemList)
        {
            item.ManualUpdate();
        }
    }

    public void UpdateScoreBoard()
    {
        Debug.Log("Start Deleted: " + playerScoreBoardUIItemList.Count);
        foreach (PlayerScoreBoardUIItem item in playerScoreBoardUIItemList)
        {
            Debug.Log("Item Deleted: " + playerScoreBoardUIItemList.Count);
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
