using System.Collections;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [field: SerializeField] ThirdPersonController thirdPersonControllerPrefab;

    [field: SerializeField] public PlayerData playerData { get; private set; }

    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>("playerName", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> playerColor = new NetworkVariable<FixedString32Bytes>("playerColor", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
        // if (IsServer) StartCoroutine(InitializeOnServer());
        
        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += SyncDataAsLateJoiner;
        NetworkBehaviourReference reference = this;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
    }

    public void SyncDataAsLateJoiner(ulong clientId)
    {
        if (clientId == NetworkManager.LocalClientId)
        {
            Debug.Log("SyncDataAsLateJoiner");
            NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
        }
    }
    #endregion
}

