using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
{
    [SerializeField] private Button StartHostButton;
    [SerializeField] private Button StartServerButton;
    [SerializeField] private Button StartClientButton;

    // Start is called before the first frame update
    void Start()
    {
        StartHostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
        });
        StartClientButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
        });
        StartServerButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartServer();
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
