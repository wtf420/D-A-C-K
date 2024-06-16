using System.Collections;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using System;
using UnityEngine.Events;

public class NetworkPlayer : NetworkBehaviour
{
    [field: SerializeField] public PlayerData playerData { get; private set; }

    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>("Ayo", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString32Bytes> playerColor = new NetworkVariable<FixedString32Bytes>("Wtf", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public UnityEvent OnAnyDataChanged;
    
    #region Monobehaviour & NetworkBehaviour
    void Awake()
    {
        DontDestroyOnLoad(this);
    }

    //sync or create network data
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            playerData = PersistentPlayer.Instance.playerData;
            playerName.Value = playerData.PlayerName;
            playerColor.Value = new string("#" + ColorUtility.ToHtmlStringRGBA(playerData.PlayerColor));
        }
        base.OnNetworkSpawn();
        RefreshPlayerData(default, default);
        playerName.OnValueChanged += RefreshPlayerData;
        playerColor.OnValueChanged += RefreshPlayerData;
        // if (IsServer) StartCoroutine(InitializeOnServer());

        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += SyncDataAsLateJoiner;
        NetworkBehaviourReference reference = this;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        NetworkManager.Singleton.OnClientConnectedCallback -= SyncDataAsLateJoiner;
        playerName.OnValueChanged -= RefreshPlayerData;
        playerColor.OnValueChanged -= RefreshPlayerData;
        OnAnyDataChanged.RemoveAllListeners();
    }

    public void SyncDataAsLateJoiner(ulong clientId)
    {
        if (clientId != NetworkManager.LocalClientId) return;
        // this variables doesnt do anything
        RefreshPlayerData(default, default);
    }

    private void RefreshPlayerData(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        playerData.PlayerName = playerName.Value.ToString();
        ColorUtility.TryParseHtmlString(playerColor.Value.ToString(), out Color color);
        playerData.PlayerColor = color;
        NetworkManager.Singleton.OnClientConnectedCallback -= SyncDataAsLateJoiner;
        OnAnyDataChanged.Invoke();
    }
    #endregion
}

