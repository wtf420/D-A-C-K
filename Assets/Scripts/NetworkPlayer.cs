using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEditor;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] CustomCharacterController characterPrefab;
    private CustomCharacterController currentCharacter;

    // Start is called before the first frame update
    void Start()
    {
        currentCharacter = null;
    }

    internal void Spawn()
    {
        if (currentCharacter == null)
        {
            currentCharacter = Instantiate(characterPrefab);
            currentCharacter.networkObject.Spawn();
        }
    }

    internal void DeSpawn()
    {
        if (currentCharacter == null)
        {
            currentCharacter.networkObject.Despawn();
            Destroy(currentCharacter.gameObject);
        }
    }
}

[CustomEditor(typeof(NetworkPlayer))]
public class LevelScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        NetworkPlayer myTarget = (NetworkPlayer)target;

        if (GUILayout.Button("Spawn"))
        {
            myTarget.Spawn();
        }

        if (GUILayout.Button("Despawn"))
        {
            myTarget.DeSpawn();
        }
    }
}

