using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum LobbyStatus
{
    InLobby,
    InGame,
    Done
}

public enum PlayerLobbyStatus
{
    NotReady,
    Ready,
    InGame
}

#region PlayerLevelInfo
[Serializable]
[HideInInspector]
public struct PlayerLobbyInfo : INetworkSerializable, IEquatable<PlayerLobbyInfo>
{
    public ulong clientId;
    public NetworkBehaviourReference networkPlayer;
    public PlayerLobbyStatus playerLobbyStatus;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out clientId);
            reader.ReadValueSafe(out networkPlayer);
            reader.ReadValueSafe(out playerLobbyStatus);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(clientId);
            writer.WriteValueSafe(networkPlayer);
            writer.WriteValueSafe(playerLobbyStatus);
        }
    }

    public bool Equals(PlayerLobbyInfo other)
    {
        return clientId == other.clientId;
    }
}
#endregion

public class CurrentLobbyInfo : NetworkBehaviour
{
    [SerializeField] NetworkList<PlayerLobbyInfo> PlayerLobbyInfoNetworkList;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
