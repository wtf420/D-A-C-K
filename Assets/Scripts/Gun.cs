using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public string Attack()
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, Mathf.Infinity, ~LayerMask.GetMask("Player"));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && !hit.collider.isTrigger)
            {
                ThirdPersonController hitplayer = hit.collider.gameObject.GetComponentInParent<ThirdPersonController>();
                if (hitplayer)
                {
                    Damage(hitplayer, 100f);
                    return hit.collider.gameObject.ToString();
                }
            }
            else continue;
        }
        return "NoHit";
    }

    public void Damage(ThirdPersonController player, float damage)
    {
        //player.TakeDamage(damage);
    }
}
