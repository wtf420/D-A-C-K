using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

public class InteractableTarget : MonoBehaviour
{
    public virtual void Trigger() { }
    public virtual void ResetTrigger() { }
}
