using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
{
    public string PlayerName;
    public Color PlayerColor;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out PlayerName);
            reader.ReadValueSafe(out PlayerColor);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(PlayerName);
            writer.WriteValueSafe(PlayerColor);
        }
    }

    public bool Equals(PlayerData other)
    {
        return PlayerName == other.PlayerName && PlayerColor == other.PlayerColor;
    }
}
