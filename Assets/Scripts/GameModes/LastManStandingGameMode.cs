// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class LastManStandingGameMode : GameMode
// {
//     [SerializeField] Weapon spawnWeapon;

//     LevelManager levelManager => LevelManager.Instance;
//     GamePlayManager gamePlayManager => GamePlayManager.Instance;

//     public override void Initialize()
//     {
//         gamePlayManager.OnLevelStatusChangedEvent.AddListener(OnGamePhaseChanged);
//     }

//     public override void Deinitialize()
//     {
//         gamePlayManager.OnLevelStatusChangedEvent.RemoveListener(OnGamePhaseChanged);
//     }

//     protected override void OnGamePhaseChanged(LevelStatus status)
//     {
//         gamePlayManager.OnPlayerDeathEvent.RemoveListener(CustomOnPlayerDeathLogicWaitingForPlayers);
//         gamePlayManager.OnPlayerDeathEvent.RemoveListener(CustomOnPlayerDeathLogicProgress);
//         gamePlayManager.OnPlayerSpawnEvent.RemoveListener(CustomOnPlayerSpawnLogicProgress);
//         switch (status)
//         {
//             case LevelStatus.None:
//                 {
//                     break;
//                 }
//             case LevelStatus.WaitingForPlayers:
//                 {
//                     gamePlayManager.OnPlayerDeathEvent.AddListener(CustomOnPlayerDeathLogicWaitingForPlayers);
//                     break;
//                 }
//             case LevelStatus.CountDown:
//                 {
//                     break;
//                 }
//             case LevelStatus.InProgress:
//                 {
//                     gamePlayManager.OnPlayerDeathEvent.AddListener(CustomOnPlayerDeathLogicProgress);
//                     gamePlayManager.OnPlayerSpawnEvent.AddListener(CustomOnPlayerSpawnLogicProgress);
//                     gamePlayManager.StartCoroutine(Custom1());
//                     break;
//                 }
//             case LevelStatus.Done:
//                 {
//                     break;
//                 }
//         }
//     }

//     IEnumerator Custom1()
//     {
//         int index = 0;
//         for (int i = 0; i < NetworkPlayersManager.Instance.NetworkPlayerInfoNetworkList.Count; i++)
//         {
//             NetworkPlayerInfo info = NetworkPlayersManager.Instance.NetworkPlayerInfoNetworkList[i];
//             gamePlayManager.RespawnCharacterRpc(info.clientId, 0, new SpawnOptions(levelManager.SpawnPoints[index]));
//             info.playerScore = playerStartingPoint;
//             NetworkPlayersManager.Instance.UpdateNetworkList(info);
//             index++;
//             if (index >= levelManager.SpawnPoints.Count) index = 0;
//             yield return 0; //wait for next frame
//         }
//     }

//     private void CustomOnPlayerDeathLogicWaitingForPlayers(ulong clientId)
//     {
//         gamePlayManager.RespawnCharacterRpc(clientId, respawnTime, new SpawnOptions(LevelManager.Instance.GetRandomSpawnPoint()));
//     }

//     private void CustomOnPlayerDeathLogicProgress(ulong clientId)
//     {
//         NetworkPlayerInfo info = NetworkPlayersManager.Instance.GetNetworkPlayerInfoFromNetworkList(clientId);
//         info.playerScore--;
//         if (info.playerScore > 0)
//         {
//             gamePlayManager.RespawnCharacterRpc(clientId, respawnTime, new SpawnOptions(levelManager.GetRandomSpawnPoint(), false));
//         }
//         else
//         {
//             gamePlayManager.RespawnCharacterRpc(clientId, respawnTime, new SpawnOptions(levelManager.GetRandomSpawnPoint(), true));
//         }
//         NetworkPlayersManager.Instance.UpdateNetworkList(info);
//     }

//     private void CustomOnPlayerSpawnLogicProgress(ulong clientId)
//     {
//         NetworkPlayerInfo info = NetworkPlayersManager.Instance.GetNetworkPlayerInfoFromNetworkList(clientId);
//         if (info.character.TryGet(out ThirdPersonController character))
//         {
//             Weapon weapon = Instantiate(spawnWeapon);
//             weapon.NetworkObject.Spawn(true);
//             weapon.wielderNetworkBehaviourReference.Value = character;
//             character.weaponNetworkBehaviourReference.Value = weapon;
//         }
//     }

//     public override bool CheckGameIsOver()
//     {
//         int currentAlivePlayer = 0;
//         int currentSpectatingPlayers = 0;
//         gamePlayManager.winner.Value = NetworkPlayersManager.Instance.NetworkPlayerInfoNetworkList[0];
//         for (int i = 0; i < NetworkPlayersManager.Instance.NetworkPlayerInfoNetworkList.Count; i++)
//         {
//             NetworkPlayerInfo info = NetworkPlayersManager.Instance.NetworkPlayerInfoNetworkList[i];
//             if (info.playerScore > 0 && info.playerStatus != (short)PlayerStatus.Spectating)
//             {
//                 currentAlivePlayer++;
//                 gamePlayManager.winner.Value = info;
//             }
//             else
//             {
//                 currentSpectatingPlayers++;
//             }
//             if (currentAlivePlayer > 1) return false;
//         }
//         if (currentSpectatingPlayers == NetworkPlayersManager.Instance.NetworkPlayerInfoNetworkList.Count)
//         {
//             //special case where everybody is spectating
//             return false;
//         }
//         if (currentAlivePlayer == 1) return true;
//         return false;
//     }
// }
