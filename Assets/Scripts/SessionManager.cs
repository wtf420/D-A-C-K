using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    [SerializeField] PlayerData playerData;

    void Start()
    {
        DontDestroyOnLoad(this);
        NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnection;
    }

    private void OnDisconnection(ulong ID)
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnection;
        if (NetworkManager.Singleton.IsClient)
        {
            Debug.Log("Disconnected!");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(1);
        }
    }
}
