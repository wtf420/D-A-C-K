using System.Collections;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] ThirdPersonController thirdPersonControllerPrefab;

    public NetworkVariable<NetworkBehaviourReference> currentCharacterNetworkBehaviourReference = new NetworkVariable<NetworkBehaviourReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> playerColor = new NetworkVariable<FixedString32Bytes>("123456", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] public ThirdPersonController currentCharacter;

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
        currentCharacterNetworkBehaviourReference.OnValueChanged += OnCurrentCharacterChanged;

        NetworkBehaviourReference reference = this;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
        currentCharacterNetworkBehaviourReference.OnValueChanged -= OnCurrentCharacterChanged;

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
            if (IsClient && !IsHost)
            {
                if (currentCharacterNetworkBehaviourReference.Value.TryGet(out ThirdPersonController character))
                {
                    this.currentCharacter = character;
                }
                NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
            }
        }
    }

    public void OnCurrentCharacterChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
    {
        if (current.TryGet(out ThirdPersonController character))
        {
            this.currentCharacter = character;
            if (IsLocalPlayer) LocalPlayer.Instance.character = character;
        }
    }
    #endregion
}

