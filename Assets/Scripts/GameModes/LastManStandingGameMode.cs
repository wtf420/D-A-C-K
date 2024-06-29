using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LastManStandingGameMode : GameMode
{
    [SerializeField] Weapon spawnWeapon;

    LevelManager levelManager => LevelManager.Instance;
    GamePlayManager gamePlayManager => GamePlayManager.Instance;

    private void CustomOnPlayerDeathLogicWaitingForPlayers(ulong clientId)
    {
        gamePlayManager.RespawnCharacterRpc(clientId, respawnTime, new SpawnOptions(LevelManager.Instance.GetRandomSpawnPoint()));
    }

    private void CustomOnPlayerDeathLogicProgress(ulong clientId)
    {
        NetworkPlayerInfo info = NetworkPlayersManager.Instance.GetNetworkPlayerInfoFromNetworkList(clientId);
        info.playerScore--;
        if (info.playerScore > 0)
        {
            gamePlayManager.RespawnCharacterRpc(clientId, respawnTime, new SpawnOptions(levelManager.GetRandomSpawnPoint(), false));
        }
        else
        {
            gamePlayManager.RespawnCharacterRpc(clientId, respawnTime, new SpawnOptions(levelManager.GetRandomSpawnPoint(), true));
        }
        NetworkPlayersManager.Instance.UpdateNetworkList(info);
    }

    private void CustomOnPlayerSpawnLogicProgress(ulong clientId)
    {
        NetworkPlayerInfo info = NetworkPlayersManager.Instance.GetNetworkPlayerInfoFromNetworkList(clientId);
        if (info.character.TryGet(out ThirdPersonController character))
        {
            Weapon weapon = Instantiate(spawnWeapon);
            weapon.NetworkObject.Spawn(true);
            weapon.wielderNetworkBehaviourReference.Value = character;
            character.weaponNetworkBehaviourReference.Value = weapon;
        }
    }
}
