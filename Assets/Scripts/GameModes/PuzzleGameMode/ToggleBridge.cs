using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleBridge : InteractableTarget
{
    [SerializeField] Vector3 OriginalOrientation, DesiredOrientation;

    void Start()
    {
        ResetTrigger();
    }

    public override void Trigger() { 
        this.transform.rotation = Quaternion.Euler(DesiredOrientation);
    }

    public override void ResetTrigger()
    {
        this.transform.rotation = Quaternion.Euler(OriginalOrientation);
    }
}
