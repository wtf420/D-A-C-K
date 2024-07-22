using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum LevelStatus : short
{
    None = 0,
    WaitingForPlayers = 1,
    CountDown = 2,
    InProgress = 3,
    Done = 4
}

public enum PlayerStatus : short
{
    Spectating,
    Alive,
    Dead,
    Respawning,
}

public struct SpawnOptions : INetworkSerializable, IEquatable<SpawnOptions>
{
    public Vector3 position;
    public Quaternion rotation;
    public bool spawnAsSpectator;

    public SpawnOptions(Transform transform, bool spawnAsSpectator = false)
    {
        position = transform.position;
        rotation = transform.rotation;
        this.spawnAsSpectator = spawnAsSpectator;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out position);
            reader.ReadValueSafe(out rotation);
            reader.ReadValueSafe(out spawnAsSpectator);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(position);
            writer.WriteValueSafe(rotation);
            writer.WriteValueSafe(spawnAsSpectator);
        }
    }

    public bool Equals(SpawnOptions other)
    {
        return false;
    }
}

// this will only update on the server
public class GamePlayManager : NetworkBehaviour
{
    public static GamePlayManager Instance;
    public NetworkManager networkManager => NetworkManager.Singleton;
    public NetworkPlayersManager networkPlayersManager => NetworkPlayersManager.Instance;
    public LevelManager levelManager => LevelManager.Instance;

    public ThirdPersonController characterPlayerPrefab;
    public ThirdPersonSpectatorController spectatorPlayerPrefab;

    [SerializeField] GameMode gameMode;

    void Awake()
    {
        if (Instance) Destroy(Instance.gameObject);
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer || IsHost)
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        if (GameModeDataHelper.Instance.MapsData.gameModeDatas.Any(x => x.GameModeName == LobbyManager.Instance.joinedLobby.Data[LobbyDataField.GameMode.ToString()].Value))
        {
            GameModeData gameModeData = GameModeDataHelper.Instance.MapsData.gameModeDatas.First(x => x.GameModeName == LobbyManager.Instance.joinedLobby.Data[LobbyDataField.GameMode.ToString()].Value);
            gameMode = Instantiate(gameModeData.GameModePrefab, null);
        }
        else
        {
            throw new Exception("Game Mode Not Found");
        }

        gameMode.NetworkObject.Spawn();
        gameMode.Initialize();
    }
}
