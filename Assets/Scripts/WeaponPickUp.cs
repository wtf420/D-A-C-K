using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class WeaponPickUp : NetworkBehaviour
{
    public Weapon weapon; 

    public override void OnNetworkSpawn()
    {
        Debug.Log("OnNetworkSpawn: ");
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log("OnNetworkDespawn: ");
        base.OnNetworkDespawn();
    }

    void Start()
    {
        Debug.Log("Start: ");
    }

    void Update()
    {
        Debug.Log("Update: ");
    }

    void OnTriggerEnter(Collider other)
    {
        CustomCharacterController character = other.GetComponent<CustomCharacterController>();
        if (character != null && character.weapon == null)
        {
            SpawnWeaponRpc(character);
            DestroyRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void DestroyRpc()
    {
        this.NetworkObject.Despawn(true);
    }

    [Rpc(SendTo.Server)]
    private void SpawnWeaponRpc(NetworkBehaviourReference networkBehaviourReference)
    {
        Weapon spawnedWeapon = Instantiate(weapon);
        spawnedWeapon.NetworkObject.Spawn();

        if (networkBehaviourReference.TryGet(out CustomCharacterController customCharacterController))
        {
            customCharacterController.weapon = spawnedWeapon;
            customCharacterController.OnWeaponPickUpRpc();
            spawnedWeapon.SetWielderRpc(customCharacterController.networkObject);
        }
    }
}
