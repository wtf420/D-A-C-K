using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class WinTrigger : NetworkBehaviour
{
    public PuzzleGameMode puzzleGameMode;

    public override void OnNetworkSpawn()
    {
        StartCoroutine(Initialize());
    }

    IEnumerator Initialize()
    {
        yield return new WaitUntil(() => GameMode.Instance != null);
        if (GameMode.Instance is PuzzleGameMode)
        {
            puzzleGameMode = GameMode.Instance as PuzzleGameMode;
        }
        else
        {
            puzzleGameMode = null;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsServer && puzzleGameMode)
        {
            ThirdPersonController character = other.GetComponent<ThirdPersonController>();
            if (character != null)
            {
                PuzzleGameModePlayerInfo info = puzzleGameMode.PuzzleGameModePlayerInfoNormalList.Where(x => x.clientId == character.OwnerClientId).FirstOrDefault();
                info.goalReached = true;
                CustomNetworkListHelper<PuzzleGameModePlayerInfo>.UpdateItemToList(info, puzzleGameMode.PuzzleGameModePlayerInfoList);
            }
        }
    }
}
