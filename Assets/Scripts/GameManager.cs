using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public NetworkVariable<bool> NetworkSpawned = new NetworkVariable<bool>(false);
    //public NetworkList<NetworkBehaviourReference> players = new NetworkList<NetworkBehaviourReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] public Dictionary<NetworkPlayer, bool> playerAliveDict = new Dictionary<NetworkPlayer, bool>();

    void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        NetworkManager.OnClientDisconnectCallback += OnDisconnection;
    }

    private void OnDisconnection(ulong ID)
    {
        if (IsClient)
        {
            Debug.Log("Disconnected!");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        NetworkManager.OnClientDisconnectCallback -= OnDisconnection;
    }

    // Update is called once per frame
    void Update()
    {
        if (NetworkSpawned.Value)
        {
            int currentAlivePlayer = 0;
            NetworkPlayer winner = null;
            foreach (KeyValuePair<NetworkPlayer, bool> entry in playerAliveDict)
            {
                currentAlivePlayer++;
                if (currentAlivePlayer == 1)
                    winner = entry.Key;
                else if (currentAlivePlayer > 1)
                    break;
            }
            //if (currentAlivePlayer == 1) Debug.Log("Winner: " + winner.NetworkObject.GetInstanceID());
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("GameManager OnNetworkSpawn");
        NetworkSpawned.Value = true;
    }

    public void AddPlayer(NetworkBehaviourReference player)
    {
        // players.Add(player);
        // NetworkPlayer p;
        // if (player.TryGet(out p))
        // {
        //     playerAliveDict.Add(p, true);
        // }
    }
}
