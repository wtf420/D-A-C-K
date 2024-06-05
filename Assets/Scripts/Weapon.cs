using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Weapon : NetworkBehaviour
{
    public ThirdPersonController wielder;
    public float range, attackAngle, minDistance;
    public float coolDown, timing;
    private bool isAttackable = true;
    private Transform holderTransform = null;

    void LateUpdate()
    {
        if (holderTransform == null) return;
        transform.position = holderTransform.transform.position;
        transform.rotation = holderTransform.transform.rotation;
    }

    public void AttemptAttack()
    {
        if (isAttackable && wielder != null)
        {
            Attack();
            StartCoroutine(CoolDown());
        }
    }

    private void Attack()
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
        RaycastHit[] info = Physics.SphereCastAll(wielder.transform.position, range, Vector3.up, 0);
        Debug.Log(info.Length);
        foreach (RaycastHit hit in info)
        {
            //check if theres a wall between
            if (wielder.gameObject == hit.collider.gameObject) continue;
            bool c = false;
            Vector3 hitlocation = (hit.point == Vector3.zero) ? hit.transform.position : hit.point;
            Debug.DrawLine(hitlocation, wielder.transform.position, Color.red, 1f);
            RaycastHit[] info2 = Physics.RaycastAll(this.transform.position, hitlocation - this.transform.position, Vector3.Distance(this.transform.position, hit.transform.position));
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

            Debug.Log(hit.collider.gameObject + " | " + hit.collider.gameObject.GetComponentInParent<ThirdPersonController>() != null);
            if (hit.collider.gameObject.GetComponent<ThirdPersonController>() != null)
            {
                //handle hit
            }
            //Debug.DrawLine(this.transform.position, hitlocation, Color.green, 5f);
        }
    }

    private IEnumerator CoolDown()
    {
        isAttackable = false;
        yield return new WaitForSeconds(coolDown);
        isAttackable = true;
    }
}
