using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] ThirdPersonController thirdPersonControllerPrefab;
    [SerializeField] FlexibleColorPicker flexibleColorPicker;
    [SerializeField] GameObject playerJoinCanvas;

    [SerializeField] Button spawnButton;

    public NetworkVariable<NetworkBehaviourReference> currentCharacterNetworkBehaviourReference = new NetworkVariable<NetworkBehaviourReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> playerColor = new NetworkVariable<FixedString32Bytes>("123456", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] public ThirdPersonController currentCharacter;

    public UnityEvent<NetworkPlayer> OnSpawn, OnDeath;

    #region Monobehaviour & NetworkBehaviour
    void Awake()
    {
        spawnButton.onClick.AddListener(SpawnCharacterOnServerRpc);
    }

    //Late join data handle here
    void Start()
    {
        //Late join data handle
    }

    //sync or create network data
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner) playerJoinCanvas.SetActive(true);
        else playerJoinCanvas.SetActive(false);
        if (IsServer) StartCoroutine(InitializeOnServer());
        
        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += SyncDataAsLateJoiner;
        currentCharacterNetworkBehaviourReference.OnValueChanged += OnCurrentCharacterChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        spawnButton.onClick.RemoveAllListeners();

        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
        currentCharacterNetworkBehaviourReference.OnValueChanged -= OnCurrentCharacterChanged;

        if (IsServer)
        {
            GameManager.Instance.RemovePlayer(this);
        }
    }

    public void SyncDataAsLateJoiner(ulong clientID)
    {
        if (clientID == NetworkManager.LocalClientId)
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
        }
    }
    #endregion

    IEnumerator InitializeOnServer()
    {
        yield return new WaitUntil(() => GameManager.Instance.IsSpawned);
        GameManager.Instance.AddPlayer(this);
    }

    [Rpc(SendTo.Server)]
    public void NewPlayerOnServerRpc()
    {
        GameManager.Instance.AddPlayer(this);
    }

    [Rpc(SendTo.Server)]
    public void SpawnCharacterOnServerRpc()
    {
        if (GameManager.Instance.playerScoreDict[this] > 0)
        {
            spawnButton.gameObject.SetActive(false);
            OnSpawn?.Invoke(this);
            currentCharacter = Instantiate(thirdPersonControllerPrefab, null);
            currentCharacter.NetworkObject.SpawnWithOwnership(this.OwnerClientId, true);

            // Initialize data
            currentCharacterNetworkBehaviourReference.Value = currentCharacter;
            currentCharacter.controlPlayerNetworkBehaviourReference.Value = this;
        }
    }

    [Rpc(SendTo.Server)]
    public void KillCharacterRpc()
    {
        currentCharacterNetworkBehaviourReference.Value = default;
        //currentCharacter.NetworkObject.Despawn(true);
        StartCoroutine(RespawnCharacter());
        OnDeath?.Invoke(this);
    }

    IEnumerator RespawnCharacter()
    {
        yield return new WaitForSeconds(1f);
        currentCharacter.NetworkObject.Despawn(true);
        SpawnCharacterOnServerRpc();
    }
}

