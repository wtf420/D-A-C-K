using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] ThirdPersonController thirdPersonControllerPrefab;
    [SerializeField] FlexibleColorPicker flexibleColorPicker;
    [SerializeField] GameObject playerJoinCanvas;

    public NetworkVariable<NetworkBehaviourReference> currentCharacterNetworkBehaviourReference = new NetworkVariable<NetworkBehaviourReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> playerColor = new NetworkVariable<FixedString32Bytes>("123456", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] public ThirdPersonController currentCharacter;

    public UnityEvent<NetworkPlayer> OnSpawn, OnDeath;

    #region Monobehaviour & NetworkBehaviour
    void Awake()
    {
        
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
    }

    public void SyncDataAsLateJoiner()
    {
        
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
    }
    #endregion

    IEnumerator InitializeOnServer()
    {
        yield return new WaitUntil(() => GameManager.Instance.networkSpawned.Value);
        GameManager.Instance.AddPlayer(this);
    }

    [Rpc(SendTo.Server)]
    public void NewPlayerOnServerRpc()
    {
        GameManager.Instance.AddPlayer(this);
    }

    [Rpc(SendTo.Server)]
    public void SpawnServerRpc()
    {
        if (GameManager.Instance.playerScoreDict[this] > 0)
        {
            OnSpawn?.Invoke(this);
            StartCoroutine(SpawnCharacterOnServer());
        }
    }

    IEnumerator SpawnCharacterOnServer()
    {
        currentCharacter = Instantiate(thirdPersonControllerPrefab, null);
        currentCharacter.NetworkObject.SpawnWithOwnership(this.OwnerClientId, true);
        
        // Initialize data
        currentCharacterNetworkBehaviourReference.Value = currentCharacter;
        currentCharacter.controlPlayerNetworkBehaviourReference.Value = this;
        currentCharacter.InitializeDataRpc(this);

        // Start syncing data to client
        yield return new WaitUntil(() => currentCharacter.networkSpawned);
    }

    [Rpc(SendTo.Server)]
    public void KillCharacterRpc()
    {
        //currentCharacter.NetworkObject.Despawn(true);
        StartCoroutine(RespawnCharacter());
        OnDeath?.Invoke(this);
    }

    IEnumerator RespawnCharacter()
    {
        yield return new WaitForSeconds(1f);
        currentCharacter.NetworkObject.Despawn(true);
        SpawnServerRpc();
    }
}

