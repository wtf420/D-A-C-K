using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillFeed : MonoBehaviour
{
    [SerializeField] KillFeedUIItem killFeedUIItemPrefab;
    [SerializeField] Transform scrollViewContent;

    List<KillFeedUIItem> scrollViewUIItemList;

    void Start()
    {
        scrollViewUIItemList = new List<KillFeedUIItem>();
    }

    public void AddNewItem(ulong killerId, ulong victimId)
    {
        KillFeedUIItem instantiateItem = Instantiate(killFeedUIItemPrefab, scrollViewContent, false);
        instantiateItem.Initialize(killerId, victimId);

        scrollViewUIItemList.Add(instantiateItem);
    }

    public void Clear()
    {
        foreach (KillFeedUIItem item in scrollViewUIItemList)
        {
            item.StopAllCoroutines();
            Destroy(item.gameObject);
        }
        scrollViewUIItemList.Clear();
    }
}
