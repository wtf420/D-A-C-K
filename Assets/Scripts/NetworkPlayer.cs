using System.Collections;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] ThirdPersonController thirdPersonControllerPrefab;

    public NetworkVariable<FixedString32Bytes> playerColor = new NetworkVariable<FixedString32Bytes>("123456", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    #region Monobehaviour & NetworkBehaviour
    void Awake()
    {
        DontDestroyOnLoad(this);
    }

    //sync or create network data
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // if (IsServer) StartCoroutine(InitializeOnServer());
        
        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += SyncDataAsLateJoiner;

        NetworkBehaviourReference reference = this;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;

        if (IsServer)
        {
            LevelManager.Instance.RemovePlayer(this);
        }
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

