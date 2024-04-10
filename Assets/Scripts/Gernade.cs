using System.Collections;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Gernade : NetworkBehaviour
{
    [SerializeField] protected float explosionTimer, _explosionRadius;
    [HideInInspector] public GameObject source;

    protected bool exploded = false;

    protected virtual void Awake()
    {

    }

    public virtual void Update()
    {
        if (explosionTimer > 0)
        {
            explosionTimer -= Time.deltaTime;
        } else
        {
            if (!exploded)
                Explode();
        }
    }

    protected virtual void Explode()
    {
        exploded = true;
        RaycastHit[] info = Physics.SphereCastAll(this.transform.position, _explosionRadius, Vector3.up, 0);
        Debug.Log(info.Count());
        foreach (RaycastHit hit in info)
        {
            //check if theres a wall between
            if (this.gameObject == hit.collider.gameObject) continue;
            bool c = false;
            Vector3 hitlocation = (hit.point == Vector3.zero) ? hit.transform.position : hit.point;
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
        StopAllCoroutines();
        this.GetComponent<Renderer>().enabled = false;
        Destroy(gameObject);
    }

    [ExecuteInEditMode]
    protected virtual void OnDrawGizmos()
    {
        if (this.tag == "Player")
            Gizmos.color = Color.blue;
        else
            Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(this.transform.position, _explosionRadius);
    }
}

