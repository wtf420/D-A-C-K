using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using TMPro;

public class NetworkUI : MonoBehaviour
{
    [SerializeField] private Button StartHostButton;
    [SerializeField] private Button StartServerButton;
    [SerializeField] private Button StartClientButton;

    [SerializeField] private NetworkPlayer NetworkPlayerPrefab;

    [SerializeField] private TMP_InputField Input;

    // Start is called before the first frame update
    void Start()
    {
        StartHostButton.onClick.AddListener(() =>
        {
            StartHost();
        });
        StartClientButton.onClick.AddListener(() =>
        {
            StartClient();
        });
        StartServerButton.onClick.AddListener(() =>
        {
            StartServer();
        });
    }

    public void StartHost()
    {
        if (Input.text == "") NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = GetIpAddress();
        else NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = Input.text;
        NetworkManager.Singleton.StartHost();
        Debug.Log(NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address);
        Input.text = NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address;
        gameObject.SetActive(false);
    }

    public void StartClient()
    {
        if (Input.text == "") NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = GetIpAddress();
        else NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = Input.text;
        NetworkManager.Singleton.StartClient();
        Debug.Log(NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address);
        gameObject.SetActive(false);
    }

    public void StartServer()
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = GetIpAddress();
        NetworkManager.Singleton.StartServer();
        Debug.Log(NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address);
        gameObject.SetActive(false);
    }

    public string GetIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        string str = "";
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                str = ip.ToString();
                Debug.Log(str);
            }
        }
        return str;
        throw new System.Exception("No network adapters with an IPv4 address in the system!");
    }
}
