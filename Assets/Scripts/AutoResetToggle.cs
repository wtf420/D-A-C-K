using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AutoResetToggle : Interactable
{
    public InteractableTarget InteractTarget;
    public NetworkVariable<bool> state = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public float interactionCooldown { get; set; } = 1f;
    [SerializeField] private float resetTime = 1f;

    public override void OnNetworkSpawn()
    {
        state.OnValueChanged += OnValueChanged;
    }

    private void OnValueChanged(bool previousValue, bool newValue)
    {
        switch (newValue)
        {
            case true:
                {
                    InteractTarget.Trigger();
                    break;
                }
            case false:
                {
                    InteractTarget.ResetTrigger();
                    break;
                }
        }
    }

    public override void Interact(InteractInfo info)
    {
        if (isInteractable && !state.Value)
        {
            OnInteractRpc();
            StartCoroutine(StartInteractionCoolDown());
        }
    }

    [Rpc(SendTo.Server)]
    protected void OnInteractRpc()
    {
        //override this to do stuff
        Debug.Log(InteractTarget.name + "Interacted!");
        state.Value = true;
        StartCoroutine(AutoReset());
        StartCoroutine(StartInteractionCoolDown());
    }

    [Rpc(SendTo.Server)]
    protected void OnResetRpc()
    {
        //override this to do stuff
        Debug.Log(InteractTarget.name + "Reset!");
        state.Value = false;
    }

    protected IEnumerator AutoReset()
    {
        yield return new WaitForSeconds(resetTime);
        OnResetRpc();
    }
}
