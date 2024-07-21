using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

#region PuzzleGameModePlayerInfo
[Serializable]
public struct PuzzleGameModePlayerInfo : INetworkSerializable, IEquatable<PuzzleGameModePlayerInfo>
{
    public ulong clientId;
    public short playerStatus;
    public bool goalReached;
    public NetworkBehaviourReference character;
    public NetworkBehaviourReference networkPlayer;

    public PuzzleGameModePlayerInfo(NetworkPlayerInfo networkPlayerInfo)
    {
        clientId = networkPlayerInfo.clientId;
        playerStatus = (short)PlayerStatus.Spectating;
        goalReached = false;
        networkPlayer = networkPlayerInfo.networkPlayer;
        character = default;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out clientId);
            reader.ReadValueSafe(out playerStatus);
            reader.ReadValueSafe(out goalReached);
            reader.ReadValueSafe(out character);
            reader.ReadValueSafe(out networkPlayer);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(clientId);
            writer.WriteValueSafe(playerStatus);
            writer.WriteValueSafe(goalReached);
            writer.WriteValueSafe(character);
            writer.WriteValueSafe(networkPlayer);
        }
    }

    public bool Equals(PuzzleGameModePlayerInfo other)
    {
        return clientId == other.clientId;
    }
}
#endregion

public class PuzzleGameMode : GameMode
{
    [SerializeField] LevelManagerUI levelManagerUI;
    [SerializeField] PauseMenuScreen pauseMenuScreen;

    public ThirdPersonController characterPlayerPrefab;
    public ThirdPersonSpectatorController spectatorPlayerPrefab;

    public NetworkList<PuzzleGameModePlayerInfo> PuzzleGameModePlayerInfoList;
    public List<PuzzleGameModePlayerInfo> PuzzleGameModePlayerInfoNormalList => CustomNetworkListHelper<PuzzleGameModePlayerInfo>.ConvertToNormalList(PuzzleGameModePlayerInfoList);

    public NetworkVariable<short> currentNetworkLevelStatus = new NetworkVariable<short>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> GameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public UnityEvent<int> OnLevelStatusChangedEvent;

    protected override void Awake()
    {
        base.Awake();
        PuzzleGameModePlayerInfoList = new NetworkList<PuzzleGameModePlayerInfo>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            if (pauseMenuScreen.isShowing) pauseMenuScreen.Hide();
            else pauseMenuScreen.Show();

        // if (Input.GetKeyDown(KeyCode.Tab)) scoreBoard.Show();
        // else if (Input.GetKeyUp(KeyCode.Tab)) scoreBoard.Hide();

        if (!IsServer) return;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            networkPlayersManager.OnPlayerJoinedEvent.AddListener(OnNewPlayerJoined);
        }
        networkManager.SceneManager.OnSynchronizeComplete += SyncDataAsLateJoiner;
        if (GameStarted.Value) currentNetworkLevelStatus.OnValueChanged += OnGamePhaseChanged;

        StartCoroutine(GameLoop());
    }

    public override void OnNewPlayerJoined(ulong clientId)
    {
        Debug.Log("OnNewPlayerJoined");
        NetworkPlayerInfo networkPlayerInfo = networkPlayersManager.GetNetworkPlayerInfoFromNetworkList(clientId);
        PuzzleGameModePlayerInfo info = new PuzzleGameModePlayerInfo(networkPlayerInfo);
        PuzzleGameModePlayerInfoList.Add(info);

        LevelStatus levelStatus = (LevelStatus)currentNetworkLevelStatus.Value;
        switch (levelStatus)
        {
            case LevelStatus.None:
                {
                    break;
                }
            case LevelStatus.WaitingForPlayers:
                {
                    levelManagerUI.ShowWaitingForPlayersScreen();
                    SpawnCharacterRpc(clientId, new SpawnOptions(LevelManager.Instance.GetRandomSpawnPoint()));
                    break;
                }
            case LevelStatus.CountDown:
                {
                    break;
                }
            case LevelStatus.InProgress:
                {
                    levelManagerUI.ShowGameInProgressScreen();
                    CustomNetworkListHelper<PuzzleGameModePlayerInfo>.UpdateItemToList(info, PuzzleGameModePlayerInfoList);
                    SpawnCharacterRpc(info.clientId, new SpawnOptions(LevelManager.Instance.GetRandomSpawnPoint()));
                    break;
                }
            case LevelStatus.Done:
                {
                    levelManagerUI.ShowGameOverScreen();
                    break;
                }
        }
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
            if (PuzzleGameModePlayerInfoNormalList.Any(x => x.clientId == networkPlayersManager.NetworkPlayerInfoNetworkList[i].clientId)) return;
            PuzzleGameModePlayerInfo info = new PuzzleGameModePlayerInfo(networkPlayersManager.NetworkPlayerInfoNetworkList[i]);
            PuzzleGameModePlayerInfoList.Add(info);
        }
    }

    public override void Deinitialize()
    {
        currentNetworkLevelStatus.OnValueChanged -= OnGamePhaseChanged;
    }

    #region GameLoop
    public override void OnGamePhaseChanged(short previousValue, short newValue)
    {
        LevelStatus levelStatus = (LevelStatus)currentNetworkLevelStatus.Value;
        switch (levelStatus)
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

    public override IEnumerator GameLoop()
    {
        yield return new WaitUntil(() => networkManager.IsServer || networkManager.IsHost);
        // Move this to GameMode code

        GameStarted.Value = true;
        currentNetworkLevelStatus.Value = (short)LevelStatus.None;
        currentNetworkLevelStatus.OnValueChanged += OnGamePhaseChanged;

        Debug.Log("Awaiting Players...");
        // currentNetworkLevelStatus.Value = (short)LevelStatus.WaitingForPlayers;
        // yield return new WaitUntil(() => PuzzleGameModePlayerInfoList.Count >= miniumPlayerToStart);
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
        yield return new WaitUntil(() => PuzzleGameModePlayerInfoList.Count >= (int)miniumPlayerToStart);
        OnPlayerDeathEvent.RemoveListener(CustomOnPlayerDeathLogicWaitingForPlayers);
    }

    protected virtual IEnumerator GameInProgress()
    {
        OnPlayerDeathEvent.AddListener(CustomOnPlayerDeathLogicProgress);
        OnPlayerSpawnEvent.AddListener(CustomOnPlayerSpawnLogicProgress);

        int index = 0;
        for (int i = 0; i < PuzzleGameModePlayerInfoList.Count; i++)
        {
            PuzzleGameModePlayerInfo info = PuzzleGameModePlayerInfoList[i];
            RespawnCharacterRpc(info.clientId, 0, new SpawnOptions(levelManager.SpawnPoints[index]));
            CustomNetworkListHelper<PuzzleGameModePlayerInfo>.UpdateItemToList(info, PuzzleGameModePlayerInfoList);
            index++;
            if (index >= levelManager.SpawnPoints.Count) index = 0;
            yield return 0; //wait for next frame
        }

        currentNetworkLevelStatus.Value = (short)LevelStatus.InProgress;
        levelManagerUI.ShowGameInProgressScreen();
        yield return new WaitUntil(() => CheckGameIsOver());

        OnPlayerDeathEvent.RemoveListener(CustomOnPlayerDeathLogicProgress);
        OnPlayerSpawnEvent.RemoveListener(CustomOnPlayerSpawnLogicProgress);
    }

    protected virtual IEnumerator GameOver()
    {
        for (int i = 0; i < PuzzleGameModePlayerInfoList.Count; i++)
        {
            KillCharacterRpc(PuzzleGameModePlayerInfoList[i].clientId, PuzzleGameModePlayerInfoList[i].clientId);
        }
        currentNetworkLevelStatus.Value = (short)LevelStatus.Done;
        levelManagerUI.ShowGameOverScreen();
        yield return new WaitForSeconds(5f);
    }

    public override bool CheckGameIsOver()
    {
        for (int i = 0; i < PuzzleGameModePlayerInfoList.Count; i++)
        {
            PuzzleGameModePlayerInfo info = PuzzleGameModePlayerInfoList[i];
            if (!info.goalReached && info.playerStatus != (short)PlayerStatus.Spectating)
            {
                return false;
            }
        }
        return true;
    }
    #endregion

    #region GameplayManagement
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
        RespawnCharacterRpc(clientId, respawnTime, new SpawnOptions(levelManager.GetRandomSpawnPoint(), false));
    }

    private void CustomOnPlayerSpawnLogicProgress(ulong clientId)
    {
        PuzzleGameModePlayerInfo info = PuzzleGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
    }

    [Rpc(SendTo.Server)]
    public override void RespawnCharacterRpc(ulong clientId, float timeToSpawn, SpawnOptions options)
    {
        Debug.Log("Respawn");
        PuzzleGameModePlayerInfo info = PuzzleGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
        if (info.character.TryGet(out Playable character))
        {
            info.character = default;
            if (character.NetworkObject.IsSpawned)
                character.NetworkObject.Despawn(true);
            else
                Destroy(character.gameObject);
        }
        CustomNetworkListHelper<PuzzleGameModePlayerInfo>.UpdateItemToList(info, PuzzleGameModePlayerInfoList);
        StartCoroutine(RespawnCharacter(info.clientId, timeToSpawn, options));
    }

    IEnumerator RespawnCharacter(ulong clientId, float timeToSpawn, SpawnOptions options)
    {
        Debug.Log("Respawning");
        PuzzleGameModePlayerInfo info = PuzzleGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
        info.playerStatus = (short)PlayerStatus.Respawning;
        CustomNetworkListHelper<PuzzleGameModePlayerInfo>.UpdateItemToList(info, PuzzleGameModePlayerInfoList);

        yield return new WaitForSeconds(timeToSpawn);
        SpawnCharacterRpc(clientId, options);
    }

    [Rpc(SendTo.Server)]
    public override void SpawnCharacterRpc(ulong clientId, SpawnOptions options)
    {
        Debug.Log("Spawn: " + options.spawnAsSpectator);
        PuzzleGameModePlayerInfo info = PuzzleGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
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
        CustomNetworkListHelper<PuzzleGameModePlayerInfo>.UpdateItemToList(info, PuzzleGameModePlayerInfoList);

        NetworkPlayerInfo networkPlayerInfo = networkPlayersManager.GetNetworkPlayerInfoFromNetworkList(clientId);
        networkPlayerInfo.character = info.character;
        CustomNetworkListHelper<NetworkPlayerInfo>.UpdateItemToList(networkPlayerInfo, networkPlayersManager.NetworkPlayerInfoNetworkList);
        OnPlayerSpawn(clientId);
    }

    [Rpc(SendTo.Server)]
    public override void KillCharacterRpc(ulong clientId, ulong killerId, bool AddToKillFeed = false)
    {
        Debug.Log("Kill");
        PuzzleGameModePlayerInfo info = PuzzleGameModePlayerInfoNormalList.Where(x => x.clientId == clientId).FirstOrDefault();
        if (info.character.TryGet(out Playable character))
        {
            info.character = default;
            StartCoroutine(DestroyAndDespawnAfter(character, respawnTime));
        }
        info.playerStatus = (short)PlayerStatus.Dead;

        CustomNetworkListHelper<PuzzleGameModePlayerInfo>.UpdateItemToList(info, PuzzleGameModePlayerInfoList);
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
