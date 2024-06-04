using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class BodyPart
{
    public string name;
    public GameObject gameObject;
    public Rigidbody rigidbody;
    public ClientTransform clientTransform;
    public Collider collider;
}

public class Ragdoll : MonoBehaviour
{
    [SerializeField] public List<BodyPart> bodyParts = new List<BodyPart>();

    // Start is called before the first frame update
    void Start()
    {
        bodyParts.Clear();
        foreach (Rigidbody subrigidbody in GetComponentsInChildren<Rigidbody>())
        {
            BodyPart bodyPart = new BodyPart
            {
                name = subrigidbody.gameObject.name,
                gameObject = subrigidbody.gameObject,
                rigidbody = subrigidbody,
                collider = subrigidbody.gameObject.GetComponent<Collider>(),
                clientTransform = subrigidbody.gameObject.GetComponent<ClientTransform>()
            };
            bodyParts.Add(bodyPart);
        }
    }

    public void EnableRagdoll()
    {
        foreach (BodyPart bodyPart in bodyParts)
        {
            bodyPart.clientTransform.enabled = true;
            bodyPart.rigidbody.isKinematic = false;
            bodyPart.rigidbody.useGravity = true;
            //bodyPart.collider.enabled = true;
        }
    }

    public void DisableRagdoll()
    {
        foreach (BodyPart bodyPart in bodyParts)
        {
            bodyPart.clientTransform.enabled = false;
            bodyPart.rigidbody.isKinematic = true;
            bodyPart.rigidbody.useGravity = false;
            //bodyPart.collider.enabled = false;
        }
    }

    public void AddForceToBodyPart(Vector3 force, string bodyPartName)
    {
        BodyPart bodyPart = bodyParts.Find(x => x.name.Contains(bodyPartName));
        if (bodyPart != null)
        {
            bodyPart.rigidbody.AddForce(force, ForceMode.Impulse);
        }
        else
        {
            Debug.Log("Body part not found");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Ragdoll))]
public class RagdollEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Ragdoll targetRagdoll = (Ragdoll)target;
        DrawDefaultInspector();

        if (GUILayout.Button("Get All"))
        {
            targetRagdoll.bodyParts.Clear();
            foreach (Rigidbody subrigidbody in targetRagdoll.GetComponentsInChildren<Rigidbody>())
            {
                BodyPart bodyPart = new BodyPart
                {
                    name = subrigidbody.gameObject.name,
                    gameObject = subrigidbody.gameObject,
                    rigidbody = subrigidbody,
                    collider = subrigidbody.gameObject.GetComponent<Collider>(),
                    clientTransform = subrigidbody.gameObject.GetComponent<ClientTransform>()
                };
                targetRagdoll.bodyParts.Add(bodyPart);
            }
        }

        if (GUILayout.Button("Apply configure"))
        {
            foreach (BodyPart bodyPart in targetRagdoll.bodyParts)
            {
                bodyPart.rigidbody.mass = 0.1f;
                if (bodyPart.gameObject.GetComponent<CharacterJoint>())
                {
                    bodyPart.gameObject.GetComponent<CharacterJoint>().enableProjection = true;
                }
            }
        }
    }
}
#endif