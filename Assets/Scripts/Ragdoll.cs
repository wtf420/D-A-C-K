using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

public class Ragdoll : MonoBehaviour
{
    [SerializeField] public List<Rigidbody> rigidbodies = new List<Rigidbody>();
    [SerializeField] public List<ClientTransform> clientTransforms = new List<ClientTransform>();
    [SerializeField] public List<Collider> colliders = new List<Collider>();

    // Start is called before the first frame update
    void Start()
    {
        foreach (Rigidbody subrigidbody in GetComponentsInChildren<Rigidbody>())
        {
            rigidbodies.Add(subrigidbody);
        }
        foreach (ClientTransform clientTransform in GetComponentsInChildren<ClientTransform>())
        {
            clientTransforms.Add(clientTransform);
        }
        foreach (Collider subcollider in GetComponentsInChildren<Collider>())
        {
            colliders.Add(subcollider);
        }
    }

    public void EnableRagdoll()
    {
        foreach (ClientTransform clientTransform in clientTransforms)
        {
            clientTransform.enabled = true;
        }
        foreach (Rigidbody subrigidbody in rigidbodies)
        {
            subrigidbody.isKinematic = false;
            subrigidbody.useGravity = true;
        }
        // foreach (Collider subcollider in colliders)
        // {
        //     subcollider.enabled = true;
        // }
    }

    public void DisableRagdoll()
    {
        foreach (ClientTransform clientTransform in clientTransforms)
        {
            clientTransform.enabled = false;
        }
        foreach (Rigidbody subrigidbody in rigidbodies)
        {
            subrigidbody.isKinematic = true;
            subrigidbody.useGravity = false;
        }
        // foreach (Collider subcollider in colliders)
        // {
        //     subcollider.enabled = true;
        // }
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
            targetRagdoll.rigidbodies.Clear();
            targetRagdoll.clientTransforms.Clear();
            targetRagdoll.colliders.Clear();
            foreach (Rigidbody subrigidbody in targetRagdoll.GetComponentsInChildren<Rigidbody>())
            {
                targetRagdoll.rigidbodies.Add(subrigidbody);
            }
            foreach (ClientTransform clientTransform in targetRagdoll.GetComponentsInChildren<ClientTransform>())
            {
                targetRagdoll.clientTransforms.Add(clientTransform);
            }
            foreach (Collider subcollider in targetRagdoll.GetComponentsInChildren<Collider>())
            {
                targetRagdoll.colliders.Add(subcollider);
            }
        }
    }
}
#endif