using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LocalPlayer : MonoBehaviour
{
    public static LocalPlayer Instance;
    public NetworkPlayer networkPlayer;
    public ThirdPersonController character;

    [field: SerializeField] PlayerData playerData;
    [field: SerializeField] PlayerInput playerInput;
    [field: SerializeField] Button spawnButton;
    [field: SerializeField] Button exitToMainMenuButton;

    [field: SerializeField] GameObject spawnCanvas;
    [field: SerializeField] GameObject networkCanvas;

    public bool connected = false;

    void Awake()
    {
        DontDestroyOnLoad(this);
        if (Instance) Destroy(Instance.gameObject);
        Instance = this;
    }

    void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnectedCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += OnConnectedCallback;
        StartCoroutine(Initialize());
    }

    private void ExitToMainMenu()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("LobbyScene");
    }

    IEnumerator Initialize()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton.LocalClient.PlayerObject);
        Debug.Log("Initialize");
        networkPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<NetworkPlayer>();
        // use the player object existance as a check for if the networkmanager has connected or not
        // yield return new WaitUntil(() => networkPlayer != null);
        yield return new WaitUntil(() => LevelManager.Instance.IsSpawned);
        spawnCanvas.SetActive(true);
        spawnButton.onClick.AddListener(() => LevelManager.Instance.SpawnCharacterRpc(networkPlayer));
        exitToMainMenuButton.onClick.AddListener(ExitToMainMenu);
    }

    private void OnConnectedCallback(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            connected = true;
            networkPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>();
            Debug.Log("Connected");
        }
    }

    private void OnDisconnectedCallback(ulong ID)
    {
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            exitToMainMenuButton.onClick.RemoveAllListeners();
            spawnButton.onClick.RemoveAllListeners();
            NetworkManager.Singleton.OnClientConnectedCallback -= OnConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnectedCallback;
            Debug.Log("Disconnected!");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    // void Start()
    // {
    //     NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += OnSceneManagerOnLoadComplete;
    // }

    // private void OnSceneManagerOnLoadComplete(ulong clientId)
    // {
    //     Debug.Log("OnSceneManagerOnLoadComplete");
    //     if (clientId == NetworkManager.Singleton.LocalClientId)
    //     {
    //         networkPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>();
    //         networkPlayer.SpawnCharacterOnServerRpc();
    //         Debug.Log("Ready");

    //         NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= OnSceneManagerOnLoadComplete;
    //     }
    // }
}
