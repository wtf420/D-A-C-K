using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] CustomCharacterController characterPrefab;

    public NetworkVariable<NetworkBehaviourReference> currentCharacterNetworkBehaviourReference = new NetworkVariable<NetworkBehaviourReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] public CustomCharacterController currentCharacter;

    void Awake()
    {
        currentCharacterNetworkBehaviourReference.OnValueChanged += UpdateCharacterFromReference;
    }

    //Late join data handle here
    void Start()
    {
        //Late join data handle
        UpdateCharacterFromReference(currentCharacterNetworkBehaviourReference.Value, currentCharacterNetworkBehaviourReference.Value);
    }

    //sync or create network data
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner) NetworkSpawnRpc();
    }
    
    [Rpc(SendTo.Server)]
    public void NetworkSpawnRpc()
    {
        StartCoroutine(NetworkSpawn());
    }

    IEnumerator NetworkSpawn()
    {
        yield return new WaitUntil(() => GameManager.Instance.NetworkSpawned.Value);
        currentCharacter = null;
        SpawnServerRpc();
    }

    [Rpc(SendTo.Server)]
    public void NewPlayerOnServerRpc()
    {
        GameManager.Instance.AddPlayer(this);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        currentCharacterNetworkBehaviourReference.OnValueChanged -= UpdateCharacterFromReference;
    }

    void UpdateCharacterFromReference(NetworkBehaviourReference previous, NetworkBehaviourReference current)
    {
        if (current.TryGet(out CustomCharacterController character))
        {
            this.currentCharacter = character;
        }
    }

    [Rpc(SendTo.Server)]
    public void SpawnServerRpc()
    {
        CustomCharacterController character = Instantiate(characterPrefab);
        character.NetworkObject.SpawnWithOwnership(this.OwnerClientId);
        currentCharacterNetworkBehaviourReference.Value = character;
        character.controlPlayerNetworkBehaviourReference.Value = this;
        GameManager.Instance.playerAliveDict[this] = true;
    }

    [Rpc(SendTo.Server)]
    public void DespawnRpc()
    {
        currentCharacter.networkObject.Despawn(true);
        StartCoroutine(Respawn());
        GameManager.Instance.playerAliveDict[this] = false;
    }

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(1f);
        SpawnServerRpc();
    }
}

