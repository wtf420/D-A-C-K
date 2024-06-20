using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameProgressScreen : Screen
{
    [SerializeField] TMP_Text PersonalOwnerLabel;
    [SerializeField] Slider PersonalProgressBar;
    [SerializeField] TMP_Text PersonalPerformanceDisplayText;

    [SerializeField] TMP_Text EnemyOwnerLabel;
    [SerializeField] Slider EnemyProgressBar;
    [SerializeField] TMP_Text EnemyPerformanceDisplayText;

    public override void UpdateScreen()
    {
        base.UpdateScreen();
        List<PlayerLevelInfo> infos = LevelManager.Instance.PlayerNetworkListToNormalList().OrderByDescending(x => x.playerScore).ToList();
        PlayerLevelInfo enemyInfo = infos[0];
        for (int i = 0; i < infos.Count; i++)
        if (infos[i].playerStatus != (short)PlayerStatus.Spectating)
        {
            enemyInfo = infos[i];
            break;
        }
        EnemyProgressBar.minValue = 0;
        EnemyProgressBar.maxValue = LevelManager.Instance.playerStartingPoint;
        EnemyProgressBar.value = enemyInfo.playerScore;
        EnemyPerformanceDisplayText.text = enemyInfo.playerScore.ToString();

        PlayerLevelInfo personalInfo = LevelManager.Instance.GetPlayerLevelInfoFromNetworkList(NetworkManager.Singleton.LocalClientId);
        PersonalProgressBar.minValue = 0;
        PersonalProgressBar.maxValue = LevelManager.Instance.playerStartingPoint;
        PersonalProgressBar.value = personalInfo.playerScore;
        PersonalPerformanceDisplayText.text = personalInfo.playerScore.ToString();

    }
}
