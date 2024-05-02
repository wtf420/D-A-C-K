using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] CustomCharacterController characterPrefab;

    private NetworkVariable<NetworkBehaviourReference> currentCharacterNetworkBehaviourReference = new NetworkVariable<NetworkBehaviourReference>();
    private CustomCharacterController currentCharacter;

    void Awake()
    {
        currentCharacterNetworkBehaviourReference.OnValueChanged += UpdateCharacterFromReference;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (IsOwner)
        {
            currentCharacter = null;
            SpawnServerRpc();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("Value changed: " + currentCharacterNetworkBehaviourReference.Value.ToString());
        UpdateCharacterFromReference(currentCharacterNetworkBehaviourReference.Value, currentCharacterNetworkBehaviourReference.Value);
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
        //SetPlayerRpc(character);
    }

    [Rpc(SendTo.Server)]
    public void DespawnRpc()
    {
        currentCharacter.networkObject.Despawn(true);
        StartCoroutine(Respawn());
    }

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(1f);
        SpawnServerRpc();
    }
}

