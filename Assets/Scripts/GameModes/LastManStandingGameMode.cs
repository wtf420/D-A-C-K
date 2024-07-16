using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

#region CustomLMSGameModePlayerInfo
[Serializable]
public struct CustomLMSGameModePlayerInfo : INetworkSerializable, IEquatable<CustomLMSGameModePlayerInfo>
{
    public ulong clientId;
    public float playerLives;
    public short playerStatus;
    public NetworkBehaviourReference character;
    public NetworkBehaviourReference networkPlayer;

    public CustomLMSGameModePlayerInfo(NetworkPlayerInfo networkPlayerInfo)
    {
        clientId = networkPlayerInfo.clientId;
        playerLives = 0;
        playerStatus = (short)PlayerStatus.Spectating;
        networkPlayer = networkPlayerInfo.networkPlayer;
        character = default;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out clientId);
            reader.ReadValueSafe(out playerLives);
            reader.ReadValueSafe(out playerStatus);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(clientId);
            writer.WriteValueSafe(playerLives);
            writer.WriteValueSafe(playerStatus);
        }
    }

    public bool Equals(CustomLMSGameModePlayerInfo other)
    {
        return clientId == other.clientId;
    }
}
#endregion

public class LastManStandingGameMode : GameMode
{
    [SerializeField] Weapon spawnWeapon;
    [SerializeField] float playerLives;

    [SerializeField] LevelManagerUI levelManagerUI;
    [SerializeField] ScoreBoard scoreBoard;
    [SerializeField] PauseMenuScreen pauseMenuScreen;
    [SerializeField] KillFeed killFeed;

    public ThirdPersonController characterPlayerPrefab;
    public ThirdPersonSpectatorController spectatorPlayerPrefab;

    public NetworkList<CustomLMSGameModePlayerInfo> CustomLMSGameModePlayerInfoList;
    public List<CustomLMSGameModePlayerInfo> CustomLMSGameModePlayerInfoNormalList => CustomNetworkListHelper<CustomLMSGameModePlayerInfo>.ConvertToNormalList(CustomLMSGameModePlayerInfoList);

    public NetworkVariable<short> currentNetworkLevelStatus = new NetworkVariable<short>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<CustomLMSGameModePlayerInfo> winner;
    public NetworkVariable<bool> GameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public UnityEvent<int> OnLevelStatusChangedEvent;

    protected override void Awake()
    {
        base.Awake();
        CustomLMSGameModePlayerInfoList = new NetworkList<CustomLMSGameModePlayerInfo>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            if (pauseMenuScreen.isShowing) pauseMenuScreen.Hide();
            else pauseMenuScreen.Show();

        if (Input.GetKeyDown(KeyCode.Tab)) scoreBoard.Show();
        else if (Input.GetKeyUp(KeyCode.Tab)) scoreBoard.Hide();

        if (!IsServer) return;
    }

    public override void OnNetworkSpawn()
    {
        networkManager.SceneManager.OnSynchronizeComplete += SyncDataAsLateJoiner;
        if (GameStarted.Value) currentNetworkLevelStatus.OnValueChanged += OnGamePhaseChanged;

        StartCoroutine(GameLoop());
    }

    public override void OnNewPlayerJoined(ulong clientId)
    {
        Debug.Log("OnNewPlayerJoined");
        NetworkPlayerInfo networkPlayerInfo = networkPlayersManager.GetNetworkPlayerInfoFromNetworkList(clientId);
        CustomLMSGameModePlayerInfo info = new CustomLMSGameModePlayerInfo(networkPlayerInfo);
        CustomLMSGameModePlayerInfoList.Add(info);
        SpawnCharacterRpc(clientId, new SpawnOptions(LevelManager.Instance.GetRandomSpawnPoint()));
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        networkManager.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
        currentNetworkLevelStatus.OnValueChanged -= OnGamePhaseChanged;

        OnPlayerSpawnEvent.RemoveAllListeners();
        OnPlayerDeathEvent.RemoveAllListeners();

        LobbyManager.Instance.ExitGame();
    }

    void SyncDataAsLateJoiner(ulong clientId)
    {
        if (clientId != NetworkManager.LocalClientId) return;
        OnGamePhaseChanged(default, default);
    }

    public override void Initialize()
    {
        currentNetworkLevelStatus.OnValueChanged += OnGamePhaseChanged;

        for (int i = 0; i < networkPlayersManager.NetworkPlayerInfoNetworkList.Count; i++)
        {
            if (CustomLMSGameModePlayerInfoNormalList.Any(x => x.clientId == networkPlayersManager.NetworkPlayerInfoNetworkList[i].clientId)) return;
            CustomLMSGameModePlayerInfo info = new CustomLMSGameModePlayerInfo(networkPlayersManager.NetworkPlayerInfoNetworkList[i]);
            CustomLMSGameModePlayerInfoList.Add(info);
        }
    }

    public override void Deinitialize()
    {
        currentNetworkLevelStatus.OnValueChanged -= OnGamePhaseChanged;
    }

    public override void OnGamePhaseChanged(short previousValue, short newValue)
    {
        LevelStatus levelStatus = (LevelStatus)newValue;
        switch (levelStatus)
        {
            case LevelStatus.None:
                {
                    break;
                }
            case LevelStatus.WaitingForPlayers:
                {
                    break;
                }
            case LevelStatus.CountDown:
                {
                    break;
                }
            case LevelStatus.InProgress:
                {
                    break;
                }
            case LevelStatus.Done:
                {
                    break;
                }
        }
    }

    public override IEnumerator GameLoop()
    {
        yield return new WaitUntil(() => networkManager.IsServer || networkManager.IsHost);
        // Move this to GameMode code
        networkPlayersManager.OnPlayerJoinedEvent.AddListener(OnNewPlayerJoined);

        GameStarted.Value = true;
        currentNetworkLevelStatus.Value = (short)LevelStatus.None;
        currentNetworkLevelStatus.OnValueChanged += OnGamePhaseChanged;

        Debug.Log("Awaiting Players...");
        // currentNetworkLevelStatus.Value = (short)LevelStatus.WaitingForPlayers;
        // yield return new WaitUntil(() => CustomLMSGameModePlayerInfoList.Count >= miniumPlayerToStart);
        yield return StartCoroutine(WaitingForPlayers());

        Debug.Log("Begining game!");
        yield return StartCoroutine(GameInProgress());

        Debug.Log("Game Over!");
        yield return StartCoroutine(GameOver());

        StopAllCoroutines();
        LobbyManager.Instance.ExitGame();
    }

    protected virtual IEnumerator WaitingForPlayers()
    {
        OnPlayerDeathEvent.AddListener(CustomOnPlayerDeathLogicWaitingForPlayers);
        currentNetworkLevelStatus.Value = (short)LevelStatus.WaitingForPlayers;
        levelManagerUI.ShowWaitingForPlayersScreen();
        yield return new WaitUntil(() => CustomLMSGameModePlayerInfoList.Count >= (int)miniumPlayerToStart);
        OnPlayerDeathEvent.RemoveListener(CustomOnPlayerDeathLogicWaitingForPlayers);
    }

    protected virtual IEnumerator GameInProgress()
    {
        OnPlayerDeathEvent.AddListener(CustomOnPlayerDeathLogicProgress);
        OnPlayerSpawnEvent.AddListener(CustomOnPlayerSpawnLogicProgress);

        int index = 0;
        for (int i = 0; i < CustomLMSGameModePlayerInfoList.Count; i++)
        {
            CustomLMSGameModePlayerInfo info = CustomLMSGameModePlayerInfoList[i];
            RespawnCharacterRpc(info.clientId, 0, new SpawnOptions(levelManager.SpawnPoints[index]));
            info.playerLives = playerLives;
            CustomNetworkListHelper<CustomLMSGameModePlayerInfo>.UpdateItemToList(info, CustomLMSGameModePlayerInfoList);
            index++;
            if (index >= levelManager.SpawnPoints.Count) index = 0;
            yield return 0; //wait for next frame
        }

        if (killFeed.isActiveAndEnabled)
        {
            ClearKillFeedRpc();
        }

        currentNetworkLevelStatus.Value = (short)LevelStatus.InProgress;
        levelManagerUI.ShowGameInProgressScreen();
        yield return new WaitUntil(() => CheckGameIsOver());

        OnPlayerDeathEvent.RemoveListener(CustomOnPlayerDeathLogicProgress);
        OnPlayerSpawnEvent.RemoveListener(CustomOnPlayerSpawnLogicProgress);
    }

    protected virtual IEnumerator GameOver()
    {
        for (int i = 0; i < CustomLMSGameModePlayerInfoList.Count; i++)
        {
            KillCharacterRpc(CustomLMSGameModePlayerInfoList[i].clientId, CustomLMSGameModePlayerInfoList[i].clientId);
        }
        currentNetworkLevelStatus.Value = (short)LevelStatus.Done;
        levelManagerUI.ShowGameOverScreen();
        yield return new WaitForSeconds(5f);
    }

    public void OnPlayerSpawn(ulong clientId)
    {
        OnPlayerSpawnEvent?.Invoke(clientId);
    }

    public void OnPlayerDeath(ulong clientId)
    {
        OnPlayerDeathEvent?.Invoke(clientId);
    }

    public void OnPlayerKill(ulong clientId, ulong victimId)
    {
        OnPlayerKillEvent?.Invoke(clientId, victimId);
    }

    private void CustomOnPlayerDeathLogicWaitingForPlayers(ulong clientId)
    {
        RespawnCharacterRpc(clientId, respawnTime, new SpawnOptions(LevelManager.Instance.GetRandomSpawnPoint()));
    }

    private void CustomOnPlayerDeathLogicProgress(ulong clientId)
    {
        CustomLMSGameModePlayerInfo info = CustomLMSGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
        info.playerLives--;
        if (info.playerLives > 0)
        {
            RespawnCharacterRpc(clientId, respawnTime, new SpawnOptions(levelManager.GetRandomSpawnPoint(), false));
        }
        else
        {
            RespawnCharacterRpc(clientId, respawnTime, new SpawnOptions(levelManager.GetRandomSpawnPoint(), true));
        }
        CustomNetworkListHelper<CustomLMSGameModePlayerInfo>.UpdateItemToList(info, CustomLMSGameModePlayerInfoList);
    }

    private void CustomOnPlayerSpawnLogicProgress(ulong clientId)
    {
        CustomLMSGameModePlayerInfo info = CustomLMSGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
        if (info.character.TryGet(out ThirdPersonController character))
        {
            Weapon weapon = Instantiate(spawnWeapon);
            weapon.NetworkObject.Spawn(true);
            weapon.wielderNetworkBehaviourReference.Value = character;
            character.weaponNetworkBehaviourReference.Value = weapon;
        }
    }

    public override bool CheckGameIsOver()
    {
        int currentAlivePlayer = 0;
        int currentSpectatingPlayers = 0;
        winner.Value = CustomLMSGameModePlayerInfoList[0];
        for (int i = 0; i < CustomLMSGameModePlayerInfoList.Count; i++)
        {
            CustomLMSGameModePlayerInfo info = CustomLMSGameModePlayerInfoList[i];
            if (info.playerLives > 0 && info.playerStatus != (short)PlayerStatus.Spectating)
            {
                currentAlivePlayer++;
                winner.Value = info;
            }
            else
            {
                currentSpectatingPlayers++;
            }
            if (currentAlivePlayer > 1) return false;
        }
        if (currentSpectatingPlayers == CustomLMSGameModePlayerInfoList.Count)
        {
            //special case where everybody is spectating
            return false;
        }
        if (currentAlivePlayer == 1) return true;
        return false;
    }

    [Rpc(SendTo.Everyone)]
    void AddToKillFeedRpc(ulong clientId, ulong killerId = default)
    {
        killFeed.AddNewItem(killerId, clientId);
    }


    [Rpc(SendTo.Everyone)]
    void ClearKillFeedRpc()
    {
        killFeed.Clear();
    }

    [Rpc(SendTo.Server)]
    public override void RespawnCharacterRpc(ulong clientId, float timeToSpawn, SpawnOptions options)
    {
        Debug.Log("Respawn");
        CustomLMSGameModePlayerInfo info = CustomLMSGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
        if (info.character.TryGet(out Playable character))
        {
            info.character = default;
            if (character.NetworkObject.IsSpawned)
                character.NetworkObject.Despawn(true);
            else
                Destroy(character.gameObject);
        }
        CustomNetworkListHelper<CustomLMSGameModePlayerInfo>.UpdateItemToList(info, CustomLMSGameModePlayerInfoList);
        StartCoroutine(RespawnCharacter(info.clientId, timeToSpawn, options));
    }

    IEnumerator RespawnCharacter(ulong clientId, float timeToSpawn, SpawnOptions options)
    {
        Debug.Log("Respawning");
        CustomLMSGameModePlayerInfo info = CustomLMSGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
        info.playerStatus = (short)PlayerStatus.Respawning;
        CustomNetworkListHelper<CustomLMSGameModePlayerInfo>.UpdateItemToList(info, CustomLMSGameModePlayerInfoList);

        yield return new WaitForSeconds(timeToSpawn);
        SpawnCharacterRpc(clientId, options);
    }

    [Rpc(SendTo.Server)]
    public override void SpawnCharacterRpc(ulong clientId, SpawnOptions options)
    {
        Debug.Log("Spawn: " + options.spawnAsSpectator);
        CustomLMSGameModePlayerInfo info = CustomLMSGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
        if (info.character.TryGet(out Playable character))
        {
            if (character.NetworkObject.IsSpawned)
                character.NetworkObject.Despawn(true);
            else
                Destroy(character.gameObject);
        }
        if (options.spawnAsSpectator)
        {
            ThirdPersonSpectatorController spawnCharacter = Instantiate(spectatorPlayerPrefab, null);
            spawnCharacter.controlPlayerNetworkBehaviourReference.Value = info.networkPlayer;
            spawnCharacter.NetworkObject.SpawnWithOwnership(info.clientId, true);

            info.character = spawnCharacter;
            info.playerStatus = (short)PlayerStatus.Spectating;
        }
        else
        {
            ThirdPersonController spawnCharacter = Instantiate(characterPlayerPrefab, null);
            spawnCharacter.controlPlayerNetworkBehaviourReference.Value = info.networkPlayer;

            spawnCharacter.NetworkObject.SpawnWithOwnership(info.clientId, true);
            spawnCharacter.InitialzeRpc(options.position, options.rotation);

            info.character = spawnCharacter;
            info.playerStatus = (short)PlayerStatus.Alive;
        }
        CustomNetworkListHelper<CustomLMSGameModePlayerInfo>.UpdateItemToList(info, CustomLMSGameModePlayerInfoList);
        OnPlayerSpawn(clientId);
    }

    [Rpc(SendTo.Server)]
    public override void KillCharacterRpc(ulong clientId, ulong killerId, bool AddToKillFeed = false)
    {
        Debug.Log("Kill");
        CustomLMSGameModePlayerInfo info = CustomLMSGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
        if (info.character.TryGet(out Playable character))
        {
            info.character = default;
            StartCoroutine(DestroyAndDespawnAfter(character, 3f));
        }
        info.playerStatus = (short)PlayerStatus.Dead;

        if (killFeed.isActiveAndEnabled && AddToKillFeed)
        {
            AddToKillFeedRpc(clientId, killerId);
        }
        CustomNetworkListHelper<CustomLMSGameModePlayerInfo>.UpdateItemToList(info, CustomLMSGameModePlayerInfoList);
        OnPlayerDeath(clientId);
        OnPlayerKill(killerId, clientId);
    }

    IEnumerator DestroyAndDespawnAfter(Playable character, float time)
    {
        if (character is ThirdPersonController)
        {
            ((ThirdPersonController)character).KillRpc();
        }
        yield return new WaitForSeconds(time);
        if (character == null) yield break;
        if (character.NetworkObject.IsSpawned)
            character.NetworkObject.Despawn(true);
        else
            Destroy(character.gameObject);
    }
}
