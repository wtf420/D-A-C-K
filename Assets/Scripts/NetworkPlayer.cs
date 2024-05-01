using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] CustomCharacterController characterPrefab;
    private CustomCharacterController currentCharacter;

    // Start is called before the first frame update
    void Start()
    {
        if (IsOwner)
        {
            currentCharacter = null;
            StartCoroutine(Spawn());
        }
    }

    IEnumerator Spawn()
    {
        yield return new WaitForSeconds(3f);
        SpawnServerRpc();
    }

    [Rpc(SendTo.Server)]
    public void SpawnServerRpc()
    {
        CustomCharacterController character = Instantiate(characterPrefab);
        character.NetworkObject.Spawn();
        SetPlayerRpc(character);
    }

    [Rpc(SendTo.Everyone)]
    public void SetPlayerRpc(NetworkBehaviourReference characterReference)
    {
        if (characterReference.TryGet(out CustomCharacterController character))
        {
            this.currentCharacter = character;
            character.controlPlayer = this;
        }
    }

    [Rpc(SendTo.Server)]
    public void DespawnRpc()
    {
        currentCharacter.networkObject.Despawn();
        StartCoroutine(Respawn());
    }

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(1f);
        SpawnServerRpc();
    }
}

