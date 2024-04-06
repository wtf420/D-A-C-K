using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

public class InteractInfo
{
    public CustomCharacterController character;
}

public class Interactable : NetworkBehaviour
{
    public UnityEvent InteractEvent;
    public bool isInteractable = true;
    [SerializeField] private float interactionCooldown = 1f;

    protected new void OnDestroy()
    {
        InteractEvent.RemoveAllListeners();
    }

    public void Interact(InteractInfo info)
    {
        if (isInteractable)
        {
            InteractEvent?.Invoke();
            OnInteract(info);
            StartCoroutine(StartInteractionCoolDown());
        }
    }

    protected IEnumerator StartInteractionCoolDown()
    {
        isInteractable = false;
        yield return new WaitForSeconds(interactionCooldown);
        isInteractable = true;
    }

    protected virtual void OnInteract(InteractInfo info)
    {
        Debug.Log("Interacted!");
    }
}
