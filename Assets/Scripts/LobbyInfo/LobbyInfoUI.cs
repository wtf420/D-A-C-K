using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LobbyInfoUI : MonoBehaviour
{
    public static LobbyInfoUI Instance;

    [field: SerializeField] Button startButton;
    [field: SerializeField] Button joinButton;
    [field: SerializeField] Button exitButton;

    [field: SerializeField] GameObject playerLobbyInfoScrollviewContent;
    [field: SerializeField] PlayerLobbyInfoUIItem playerLobbyInfoUIItemPrefab;

    public Lobby joinedLobby {
        get { return UnityLobbyServiceManager.Instance.joinedLobby; }
        set { UnityLobbyServiceManager.Instance.joinedLobby = value; }
    }
    List<PlayerLobbyInfoUIItem> playerLobbyInfoUIItemList = new List<PlayerLobbyInfoUIItem>();

    void Awake()
    {
        if (Instance)
            Destroy(this.gameObject);
        else
            Instance = this;
    }

    void OnEnable()
    {
        _ = UpdateLobbyAsync();
    }

    // Start is called before the first frame update
    void Start()
    {
        startButton.onClick.AddListener(async () =>
        {
            await UnityLobbyServiceManager.Instance.CreateAndHostLobby();
            NetworkManager.Singleton.StartHost();
        });

        joinButton.onClick.AddListener(() => LobbyManager.Instance.JoinGame());
        startButton.onClick.AddListener(() => LobbyManager.Instance.StartGame());
        exitButton.onClick.AddListener(ExitLobby);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _ = UpdateLobbyAsync();
        }
    }

    void OnDestroy()
    {
        _ = UnityLobbyServiceManager.Instance.LeaveLobby();
        startButton.onClick.RemoveAllListeners();
        joinButton.onClick.RemoveAllListeners();
        exitButton.onClick.RemoveAllListeners();
    }

    public async Task UpdateLobbyAsync()
    {
        await UnityLobbyServiceManager.Instance.PollForLobbyUpdates();
        Debug.Log("UpdateLobby: " + joinedLobby.Players.Count);
        foreach (PlayerLobbyInfoUIItem item in playerLobbyInfoUIItemList)
        {
            Destroy(item.gameObject);
        }
        playerLobbyInfoUIItemList.Clear();

        if (joinedLobby.Players.Count == 0) return;
        foreach (Player player in joinedLobby.Players)
        {
            PlayerLobbyInfoUIItem InstantiateItem = Instantiate(playerLobbyInfoUIItemPrefab, playerLobbyInfoScrollviewContent.transform, false);
            InstantiateItem.Initialize(player);
            playerLobbyInfoUIItemList.Add(InstantiateItem);
        }
    }

    public void ExitLobby()
    {
        MainMenuUI.Instance.ExitLobby();
    }
}
