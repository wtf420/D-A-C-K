using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollSpawner : Interactable
{
    public GameObject ragdoll;
    public GameObject spawnPoint;

    protected override void OnInteract(CustomCharacterController customCharacterController)
    {
        customCharacterController.ToggleRagdoll();
    }
}
