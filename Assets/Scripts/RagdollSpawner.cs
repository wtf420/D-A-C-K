using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RagdollSpawner : Interactable
{
    protected override void OnInteract(InteractInfo info)
    {
        NetworkObject networkObject;
        if (info.character.TryGet(out networkObject))
        {
            networkObject.gameObject.GetComponentInChildren<CustomCharacterController>().ToggleRagdollRpc();
        }
    }
}
