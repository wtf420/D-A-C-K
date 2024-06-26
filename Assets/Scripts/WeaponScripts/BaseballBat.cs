using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;

public class BaseballBat : Weapon
{
    public float range, attackAngle, minDistance;
    public float timing;
    public float damage;

    public override void SyncDataAsLateJoiner(ulong clientId)
    {
        base.SyncDataAsLateJoiner(clientId);
        SetConstraint();
    }

    public void SetConstraint()
    {
        if (wielder)
        {
            ConstraintSource constraintSource = new ConstraintSource
            {
                sourceTransform = wielder.weaponHeldTransform,
                weight = 1,
            };
            parentConstraint.SetSource(0, constraintSource);
        } else
        {
            parentConstraint.constraintActive = false;
        }
    }

    public override void OnWielderChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
    {
        base.OnWielderChanged(previous, current);
        SetConstraint();
    }

    [Rpc(SendTo.Server)]
    public override void AttemptAttackRpc()
    {
        if (isAttackable && wielder != null)
        {
            Attack();
            StartCoroutine(CoolDown());
        }
    }

    protected override void Attack()
    {
        StartCoroutine(MeleeAttack());
    }

    private IEnumerator MeleeAttack()
    {
        yield return new WaitForSeconds(timing);
        Hit();
    }

    private void Hit()
    {
        RaycastHit[] info = Physics.SphereCastAll(wielder.transform.position, range, Vector3.up, 0, ~LayerMask.GetMask("PlayerRagdoll"));
        Debug.Log("Hit objects: " + info.Length);
        foreach (RaycastHit hit in info)
        {
            if (wielder.gameObject == hit.collider.gameObject) continue;

            ThirdPersonController hitCharacter = hit.collider.gameObject.GetComponent<ThirdPersonController>();
            if (hitCharacter != null)
            {
                //check if theres a wall between
                bool c = false;
                Vector3 hitlocation = (hit.point == Vector3.zero) ? hit.transform.position : hit.point;
                Debug.DrawLine(hitlocation, wielder.transform.position, Color.red, 1f);
                RaycastHit[] info2 = Physics.RaycastAll(this.transform.position, hitlocation - this.transform.position, Vector3.Distance(this.transform.position, hit.transform.position), ~LayerMask.GetMask("PlayerRagdoll"));
                Debug.Log("Considering: " + hit.collider.gameObject);
                foreach (RaycastHit hit2 in info2)
                {
                    if (hit2.collider.gameObject != hit.collider.gameObject && hit2.collider.gameObject != this.gameObject && hit.transform.tag == hit2.transform.tag)
                    {
                        c = true;
                        Debug.Log("break: " + hit2.collider.gameObject + " is in the way");
                        break;
                    }
                }
                if (c) continue; else Debug.Log("Considered: " + hit.collider.gameObject + " has no gameobject in between!");

                //no wall in between, process hit
                hitCharacter.TakeDamageRpc(wielder.controlPlayer.OwnerClientId, damage, wielder.transform.position - hit.point);
            }
            // Debug.DrawLine(this.transform.position, hitlocation, Color.green, 5f);
            StartCoroutine(CoolDown());
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(wielder.transform.position, range);
    }
}
