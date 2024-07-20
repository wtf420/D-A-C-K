using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LastManStandingProgressUI : Screen
{
    [SerializeField] LastManStandingGameMode gameMode;
    [SerializeField] TMP_Text PersonalOwnerLabel;
    [SerializeField] Slider PersonalProgressBar;
    [SerializeField] TMP_Text PersonalPerformanceDisplayText;

    [SerializeField] TMP_Text EnemyOwnerLabel;
    [SerializeField] Slider EnemyProgressBar;
    [SerializeField] TMP_Text EnemyPerformanceDisplayText;

    public override void UpdateScreen()
    {
        StartCoroutine(UpdateScreenAsync());
    }

    IEnumerator UpdateScreenAsync()
    {
        yield return new WaitUntil(() => gameMode.CustomLMSGameModePlayerInfoNormalList.Any(x => x.clientId == NetworkManager.Singleton.LocalClientId));
        List<CustomLMSGameModePlayerInfo> infos = gameMode.CustomLMSGameModePlayerInfoNormalList.OrderByDescending(x => x.playerLives).ToList();
        CustomLMSGameModePlayerInfo enemyInfo = infos[0];
        for (int i = 0; i < infos.Count; i++)
            if (infos[i].playerStatus != (short)PlayerStatus.Spectating)
            {
                enemyInfo = infos[i];
                break;
            }
        EnemyProgressBar.minValue = 0;
        EnemyProgressBar.maxValue = gameMode.playerStartingPoint;
        EnemyProgressBar.value = enemyInfo.playerLives;
        EnemyPerformanceDisplayText.text = enemyInfo.playerLives.ToString();

        CustomLMSGameModePlayerInfo personalInfo = infos.First(x => x.clientId == NetworkManager.Singleton.LocalClientId);
        PersonalProgressBar.minValue = 0;
        PersonalProgressBar.maxValue = gameMode.playerStartingPoint;
        PersonalProgressBar.value = personalInfo.playerLives;
        PersonalPerformanceDisplayText.text = personalInfo.playerLives.ToString();
    }
}
