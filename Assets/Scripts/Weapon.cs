using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public CustomCharacterController wielder;
    public float range, attackAngle, minDistance;
    public float coolDown, timing;
    private bool isAttackable = true;

    public void SetWielder(CustomCharacterController customCharacterController = null)
    {
        wielder = customCharacterController;
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

            Debug.Log(hit.collider.gameObject + " | " + hit.collider.gameObject.GetComponentInParent<CustomCharacterController>() != null);
            if (hit.collider.gameObject.GetComponent<CustomCharacterController>() != null)
            {
                Debug.Log("Set Ragdoll State successfully!");
                hit.collider.gameObject.GetComponent<CustomCharacterController>().SetRagdollStateRpc(true);
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
