using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    public UnityEvent<CustomCharacterController> InteractEvent;
    public bool isInteractable = true;
    [SerializeField] private float interactionCooldown = 1f;

    protected void Start()
    {
        InteractEvent.AddListener(OnInteract);
    }

    protected void OnDestroy()
    {
        InteractEvent.RemoveListener(OnInteract);
    }

    public void Interact(CustomCharacterController c)
    {
        if (isInteractable)
        {
            InteractEvent?.Invoke(c);
            StartCoroutine(StartInteractionCoolDown());
        }
    }

    protected IEnumerator StartInteractionCoolDown()
    {
        isInteractable = false;
        yield return new WaitForSeconds(interactionCooldown);
        isInteractable = true;
    }

    protected virtual void OnInteract(CustomCharacterController customCharacterController)
    {
        Debug.Log("Interacted!");
    }
}
