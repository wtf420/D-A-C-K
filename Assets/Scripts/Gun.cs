using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public string Attack()
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, Mathf.Infinity, ~LayerMask.GetMask("CharacterController"));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && !hit.collider.isTrigger)
            {
                return hit.collider.gameObject.ToString();
            }
            else continue;
        }
        return "NoHit";
    }
}
