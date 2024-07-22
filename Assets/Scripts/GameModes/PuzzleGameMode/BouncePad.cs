using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class BouncePad : NetworkBehaviour
{
    [SerializeField] float BounceForce = 10f;

    void OnTriggerEnter(Collider other)
    {
        if (IsServer)
        {
            ThirdPersonController character = other.GetComponent<ThirdPersonController>();
            if (character != null)
            {
                character.AddImpulseForceRpc(transform.up * BounceForce);
            }
        }
    }
}
