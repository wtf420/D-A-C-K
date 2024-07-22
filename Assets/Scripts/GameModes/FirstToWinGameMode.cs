using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

#region CustomFTWGameModePlayerInfo
[Serializable]
public struct CustomFTWGameModePlayerInfo : INetworkSerializable, IEquatable<CustomFTWGameModePlayerInfo>
{
    public ulong clientId;
    public float playerScore;
    public short playerStatus;
    public NetworkBehaviourReference character;
    public NetworkBehaviourReference networkPlayer;

    public CustomFTWGameModePlayerInfo(NetworkPlayerInfo networkPlayerInfo)
    {
        clientId = networkPlayerInfo.clientId;
        playerScore = 0;
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
            reader.ReadValueSafe(out playerScore);
            reader.ReadValueSafe(out playerStatus);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(clientId);
            writer.WriteValueSafe(playerScore);
            writer.WriteValueSafe(playerStatus);
        }
    }

    public bool Equals(CustomFTWGameModePlayerInfo other)
    {
        return clientId == other.clientId;
    }
}
#endregion

public class FirstToWinGameMode : GameMode
{
    [SerializeField] Weapon spawnWeapon;
    [SerializeField] public float winTargetPoint;

    [SerializeField] LevelManagerUI levelManagerUI;
    [SerializeField] FirstToWinScoreBoard scoreBoard;
    [SerializeField] PauseMenuScreen pauseMenuScreen;
    [SerializeField] KillFeed killFeed;

    public ThirdPersonController characterPlayerPrefab;
    public ThirdPersonSpectatorController spectatorPlayerPrefab;

    public NetworkList<CustomFTWGameModePlayerInfo> CustomFTWGameModePlayerInfoList;
    public List<CustomFTWGameModePlayerInfo> CustomFTWGameModePlayerInfoNormalList => CustomNetworkListHelper<CustomFTWGameModePlayerInfo>.ConvertToNormalList(CustomFTWGameModePlayerInfoList);

    public NetworkVariable<short> currentNetworkLevelStatus = new NetworkVariable<short>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<CustomFTWGameModePlayerInfo> winner;
    public NetworkVariable<bool> GameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public UnityEvent<int> OnLevelStatusChangedEvent;

    protected override void Awake()
    {
        base.Awake();
        CustomFTWGameModePlayerInfoList = new NetworkList<CustomFTWGameModePlayerInfo>();
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
        CustomFTWGameModePlayerInfo info = new CustomFTWGameModePlayerInfo(networkPlayerInfo);
        CustomFTWGameModePlayerInfoList.Add(info);
        SpawnCharacterRpc(clientId, new SpawnOptions(levelManager.GetRandomSpawnPoint()));
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
            if (CustomFTWGameModePlayerInfoNormalList.Any(x => x.clientId == networkPlayersManager.NetworkPlayerInfoNetworkList[i].clientId)) return;
            CustomFTWGameModePlayerInfo info = new CustomFTWGameModePlayerInfo(networkPlayersManager.NetworkPlayerInfoNetworkList[i]);
            CustomFTWGameModePlayerInfoList.Add(info);
        }
    }

    public override void Deinitialize()
    {
        currentNetworkLevelStatus.OnValueChanged -= OnGamePhaseChanged;
    }

    #region GameLoop
    // runs on host & clients machine
    public override void OnGamePhaseChanged(short previousValue, short newValue)
    {
        LevelStatus status = (LevelStatus)newValue;
        switch (status)
        {
            case LevelStatus.None:
                {
                    break;
                }
            case LevelStatus.WaitingForPlayers:
                {
                    levelManagerUI.ShowWaitingForPlayersScreen();
                    break;
                }
            case LevelStatus.CountDown:
                {
                    break;
                }
            case LevelStatus.InProgress:
                {
                    levelManagerUI.ShowGameInProgressScreen();
                    break;
                }
            case LevelStatus.Done:
                {
                    levelManagerUI.ShowGameOverScreen();
                    break;
                }
        }
    }

    // runs on host machine only
    public override IEnumerator GameLoop()
    {
        yield return new WaitUntil(() => networkManager.IsServer || networkManager.IsHost);
        // Move this to GameMode code
        networkPlayersManager.OnPlayerJoinedEvent.AddListener(OnNewPlayerJoined);

        GameStarted.Value = true;
        currentNetworkLevelStatus.Value = (short)LevelStatus.None;
        currentNetworkLevelStatus.OnValueChanged += OnGamePhaseChanged;
        UpdateScoreBoardScreenRpc();

        Debug.Log("Awaiting Players...");
        // currentNetworkLevelStatus.Value = (short)LevelStatus.WaitingForPlayers;
        // yield return new WaitUntil(() => CustomFTWGameModePlayerInfoList.Count >= miniumPlayerToStart);
        yield return StartCoroutine(WaitingForPlayers());

        Debug.Log("Begining game!");
        yield return StartCoroutine(GameInProgress());

        Debug.Log("Game Over!");
        yield return StartCoroutine(GameOver());

        StopAllCoroutines();
        LobbyManager.Instance.ExitGame();
    }

    // runs on host machine only
    protected virtual IEnumerator WaitingForPlayers()
    {
        OnPlayerDeathEvent.AddListener(CustomOnPlayerDeathLogicWaitingForPlayers);
        currentNetworkLevelStatus.Value = (short)LevelStatus.WaitingForPlayers;
        yield return new WaitUntil(() => CustomFTWGameModePlayerInfoList.Count >= (int)miniumPlayerToStart);
        OnPlayerDeathEvent.RemoveListener(CustomOnPlayerDeathLogicWaitingForPlayers);
    }

    // runs on host machine only
    protected virtual IEnumerator GameInProgress()
    {
        OnPlayerDeathEvent.AddListener(CustomOnPlayerDeathLogicProgress);
        OnPlayerSpawnEvent.AddListener(CustomOnPlayerSpawnLogicProgress);
        OnPlayerKillEvent.AddListener(CustomOnPlayerKillLogicProgress);

        int index = 0;
        for (int i = 0; i < CustomFTWGameModePlayerInfoList.Count; i++)
        {
            CustomFTWGameModePlayerInfo info = CustomFTWGameModePlayerInfoList[i];
            RespawnCharacterRpc(info.clientId, 0, new SpawnOptions(levelManager.GetRandomSpawnPoint()));
            info.playerScore = playerStartingPoint;
            CustomNetworkListHelper<CustomFTWGameModePlayerInfo>.UpdateItemToList(info, CustomFTWGameModePlayerInfoList);
            index++;
            if (index >= levelManager.SpawnPoints.Count) index = 0;
            yield return 0; //wait for next frame
        }

        if (killFeed.isActiveAndEnabled)
        {
            ClearKillFeedRpc();
        }

        currentNetworkLevelStatus.Value = (short)LevelStatus.InProgress;
        yield return new WaitUntil(() => CheckGameIsOver());

        OnPlayerDeathEvent.RemoveListener(CustomOnPlayerDeathLogicProgress);
        OnPlayerSpawnEvent.RemoveListener(CustomOnPlayerSpawnLogicProgress);
        OnPlayerKillEvent.RemoveListener(CustomOnPlayerKillLogicProgress);
    }

    // runs on host machine only
    protected virtual IEnumerator GameOver()
    {
        for (int i = 0; i < CustomFTWGameModePlayerInfoList.Count; i++)
        {
            KillCharacterRpc(CustomFTWGameModePlayerInfoList[i].clientId, CustomFTWGameModePlayerInfoList[i].clientId);
        }
        currentNetworkLevelStatus.Value = (short)LevelStatus.Done;
        yield return new WaitForSeconds(5f);
    }

    private void CustomOnPlayerKillLogicProgress(ulong clientId, ulong victimId)
    {
        if (clientId != victimId)
        {
            CustomFTWGameModePlayerInfo info = CustomFTWGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
            info.playerScore++;
            CustomNetworkListHelper<CustomFTWGameModePlayerInfo>.UpdateItemToList(info, CustomFTWGameModePlayerInfoList);
        }
    }

    private void CustomOnPlayerDeathLogicWaitingForPlayers(ulong clientId)
    {
        RespawnCharacterRpc(clientId, respawnTime, new SpawnOptions(levelManager.GetRandomSpawnPoint()));
    }

    private void CustomOnPlayerDeathLogicProgress(ulong clientId)
    {
        RespawnCharacterRpc(clientId, respawnTime, new SpawnOptions(levelManager.GetRandomSpawnPoint(), false));
    }

    private void CustomOnPlayerSpawnLogicProgress(ulong clientId)
    {
        CustomFTWGameModePlayerInfo info = CustomFTWGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
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
        int currentSpectatingPlayers = 0;
        winner.Value = CustomFTWGameModePlayerInfoList[0];
        for (int i = 0; i < CustomFTWGameModePlayerInfoList.Count; i++)
        {
            CustomFTWGameModePlayerInfo info = CustomFTWGameModePlayerInfoList[i];
            if (info.playerScore >= winTargetPoint && info.playerStatus != (short)PlayerStatus.Spectating)
            {
                winner.Value = info;
                return true;
            }
            else
            {
                currentSpectatingPlayers++;
            }
        }
        if (currentSpectatingPlayers == CustomFTWGameModePlayerInfoList.Count)
        {
            //special case where everybody is spectating
            return false;
        }
        return false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void AddToKillFeedRpc(ulong clientId, ulong killerId = default)
    {
        killFeed.AddNewItem(killerId, clientId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void ClearKillFeedRpc()
    {
        killFeed.Clear();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void UpdateScoreBoardScreenRpc()
    {
        scoreBoard.UpdateScreen();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void UpdateScoreBoardInfoRpc(ulong clientId)
    {
        scoreBoard.UpdateInfo(clientId);
    }
    #endregion

    #region Gameplay Management
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

    [Rpc(SendTo.Server)]
    public override void RespawnCharacterRpc(ulong clientId, float timeToSpawn, SpawnOptions options)
    {
        Debug.Log("Respawn");
        CustomFTWGameModePlayerInfo info = CustomFTWGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
        if (info.character.TryGet(out Playable character))
        {
            info.character = default;
            if (character.NetworkObject.IsSpawned)
                character.NetworkObject.Despawn(true);
            else
                Destroy(character.gameObject);
        }
        CustomNetworkListHelper<CustomFTWGameModePlayerInfo>.UpdateItemToList(info, CustomFTWGameModePlayerInfoList);
        StartCoroutine(RespawnCharacter(info.clientId, timeToSpawn, options));
    }

    IEnumerator RespawnCharacter(ulong clientId, float timeToSpawn, SpawnOptions options)
    {
        Debug.Log("Respawning");
        CustomFTWGameModePlayerInfo info = CustomFTWGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
        info.playerStatus = (short)PlayerStatus.Respawning;
        CustomNetworkListHelper<CustomFTWGameModePlayerInfo>.UpdateItemToList(info, CustomFTWGameModePlayerInfoList);

        yield return new WaitForSeconds(timeToSpawn);
        SpawnCharacterRpc(clientId, options);
    }

    [Rpc(SendTo.Server)]
    public override void SpawnCharacterRpc(ulong clientId, SpawnOptions options)
    {
        Debug.Log("Spawn: " + options.spawnAsSpectator);
        CustomFTWGameModePlayerInfo info = CustomFTWGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
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
        CustomNetworkListHelper<CustomFTWGameModePlayerInfo>.UpdateItemToList(info, CustomFTWGameModePlayerInfoList);

        NetworkPlayerInfo networkPlayerInfo = networkPlayersManager.GetNetworkPlayerInfoFromNetworkList(clientId);
        networkPlayerInfo.character = info.character;
        CustomNetworkListHelper<NetworkPlayerInfo>.UpdateItemToList(networkPlayerInfo, networkPlayersManager.NetworkPlayerInfoNetworkList);
        OnPlayerSpawn(clientId);
    }

    [Rpc(SendTo.Server)]
    public override void KillCharacterRpc(ulong clientId, ulong killerId, bool AddToKillFeed = false)
    {
        Debug.Log("Kill");
        CustomFTWGameModePlayerInfo info = CustomFTWGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
        if (info.character.TryGet(out Playable character))
        {
            info.character = default;
            StartCoroutine(DestroyAndDespawnAfter(character, 3f));
        }
        info.playerStatus = (short)PlayerStatus.Dead;
        CustomNetworkListHelper<CustomFTWGameModePlayerInfo>.UpdateItemToList(info, CustomFTWGameModePlayerInfoList);

        if (killFeed.isActiveAndEnabled && AddToKillFeed)
        {
            AddToKillFeedRpc(clientId, killerId);
        }
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
    #endregion
}
