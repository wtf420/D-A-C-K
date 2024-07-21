using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

public class InteractInfo
{
    public NetworkObjectReference character;
}

public class Interactable : NetworkBehaviour
{
    public UnityEvent<InteractInfo> InteractEvent;
    public bool isInteractable = true;
    [SerializeField] private float interactionCooldown = 1f;

    protected new void OnDestroy()
    {
        InteractEvent.RemoveAllListeners();
    }

    public virtual void Interact(InteractInfo info)
    {
        if (isInteractable)
        {
            InteractEvent?.Invoke(info);
            OnInteract(info);
            StartCoroutine(StartInteractionCoolDown());
        }
    }

    protected virtual IEnumerator StartInteractionCoolDown()
    {
        isInteractable = false;
        yield return new WaitForSeconds(interactionCooldown);
        isInteractable = true;
    }

    protected virtual void OnInteract(InteractInfo info)
    {
        //override this to do stuff
        Debug.Log("Interacted!");
    }
}
