using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleBridge : InteractableTarget
{
    void Start()
    {
        ResetTrigger();
    }

    public override void Trigger() { 
        this.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }

    public override void ResetTrigger()
    {
        this.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
