using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PickUpable : Interactable
{
    private Transform holder = null;
    [SerializeField] private Rigidbody rigidbody;
    [SerializeField] private Collider collider;

    void LateUpdate()
    {
        if (holder == null) return;
        transform.position = holder.transform.position;
        transform.rotation = holder.transform.rotation;
    }

    protected override void OnInteract(InteractInfo info)
    {
        SetPickUpRpc(info.character);
    }

    [Rpc(SendTo.Everyone)]
    protected virtual void SetPickUpRpc(NetworkObjectReference transform)
    {
        NetworkObject networkObject;
        if (transform.TryGet(out networkObject))
        {
            CustomCharacterController customCharacterController = networkObject.gameObject.GetComponentInChildren<CustomCharacterController>();
            if (customCharacterController.pickupPosition.transform != holder)
            {
                holder = networkObject.gameObject.GetComponentInChildren<CustomCharacterController>().pickupPosition.transform;
                rigidbody.isKinematic = true;
                Physics.IgnoreCollision(collider, holder.GetComponentInParent<Collider>(), true);
                Debug.Log("Set Hold:" + holder);
            } else
            {
                Physics.IgnoreCollision(collider, holder.GetComponent<Collider>(), false);
                holder = null;
                rigidbody.isKinematic = false;
                Debug.Log("Let go");
            }
        } else
        {
            holder = null;
            rigidbody.isKinematic = false;
            Debug.Log("Is null");
        }
    }
}
