using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class FirstToWinProgressUI : Screen
{
    [SerializeField] FirstToWinGameMode gameMode;
    [SerializeField] TMP_Text PersonalOwnerLabel;
    [SerializeField] Slider PersonalProgressBar;
    [SerializeField] TMP_Text PersonalPerformanceDisplayText;

    [SerializeField] TMP_Text EnemyOwnerLabel;
    [SerializeField] Slider EnemyProgressBar;
    [SerializeField] TMP_Text EnemyPerformanceDisplayText;

    public override void UpdateScreen()
    {
        List<CustomFTWGameModePlayerInfo> infos = gameMode.CustomFTWGameModePlayerInfoNormalList.OrderByDescending(x => x.playerScore).ToList();
        CustomFTWGameModePlayerInfo enemyInfo = infos[0];
        for (int i = 0; i < infos.Count; i++)
        if (infos[i].playerStatus != (short)PlayerStatus.Spectating)
        {
            enemyInfo = infos[i];
            break;
        }
        EnemyProgressBar.minValue = 0;
        EnemyProgressBar.maxValue = gameMode.winTargetPoint;
        EnemyProgressBar.value = enemyInfo.playerScore;
        EnemyPerformanceDisplayText.text = enemyInfo.playerScore.ToString();

        CustomFTWGameModePlayerInfo personalInfo = infos.First(x => x.clientId == NetworkManager.Singleton.LocalClientId);
        PersonalProgressBar.minValue = 0;
        PersonalProgressBar.maxValue = gameMode.winTargetPoint;
        PersonalProgressBar.value = personalInfo.playerScore;
        PersonalPerformanceDisplayText.text = personalInfo.playerScore.ToString();
    }
}
