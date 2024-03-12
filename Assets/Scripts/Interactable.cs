using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    public UnityEvent<CustomCharacterController> InteractEvent;
    public bool isInteractable = true;
    [SerializeField] private float interactionCooldown = 0f;

    void Start()
    {
        InteractEvent.AddListener(OnInteract);
    }

    void OnDestroy()
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

    IEnumerator StartInteractionCoolDown()
    {
        isInteractable = false;
        yield return new WaitForSeconds(interactionCooldown);
        isInteractable = true;
    }

    void OnInteract(CustomCharacterController customCharacterController)
    {
        Debug.Log("Interacted!");
    }
}
