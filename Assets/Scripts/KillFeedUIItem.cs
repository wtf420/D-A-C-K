using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;


public class KillFeedUIItem : MonoBehaviour
{
    [SerializeField] Color red, green;
    [SerializeField] TextMeshProUGUI KillerNameText, VictimNameText;

    public void Initialize(ulong killerId, ulong victimId)
    {
        PlayerLevelInfo killerInfo = LevelManager.Instance.GetPlayerLevelInfoFromNetworkList(killerId);
        KillerNameText.text = killerInfo.playerName.ToString();
        KillerNameText.color = killerId == NetworkManager.Singleton.LocalClientId ? green : red;

        PlayerLevelInfo victimInfo = LevelManager.Instance.GetPlayerLevelInfoFromNetworkList(victimId);
        VictimNameText.text = victimInfo.playerName.ToString();
        VictimNameText.color = victimId == NetworkManager.Singleton.LocalClientId ? green : red;

        StartCoroutine(Countdown());
    }

    IEnumerator Countdown()
    {
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }
}
