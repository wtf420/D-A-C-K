using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] ThirdPersonController thirdPersonControllerPrefab;

    public NetworkVariable<NetworkBehaviourReference> currentCharacterNetworkBehaviourReference = new NetworkVariable<NetworkBehaviourReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> playerColor = new NetworkVariable<FixedString32Bytes>("123456", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] public ThirdPersonController currentCharacter;

    void Awake()
    {
        
    }

    //Late join data handle here
    void Start()
    {
        //Late join data handle
    }

    //sync or create network data
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner) NetworkSpawnRpc();
    }

    [Rpc(SendTo.Server)]
    public void NetworkSpawnRpc()
    {
        playerColor.Value = "#" + ColorUtility.ToHtmlStringRGBA(Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f));
        currentCharacter = null;
        StartCoroutine(NetworkSpawn());
    }

    IEnumerator NetworkSpawn()
    {
        yield return new WaitUntil(() => GameManager.Instance.NetworkSpawned.Value);
    }

    [Rpc(SendTo.Server)]
    public void NewPlayerOnServerRpc()
    {
        GameManager.Instance.AddPlayer(this);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
    }

    [Rpc(SendTo.Server)]
    public void SpawnServerRpc()
    {
        ThirdPersonController thirdPersonController = Instantiate(thirdPersonControllerPrefab, null);
        thirdPersonController.NetworkObject.SpawnWithOwnership(this.OwnerClientId);
        currentCharacterNetworkBehaviourReference.Value = thirdPersonController;
        thirdPersonController.controlPlayerNetworkBehaviourReference.Value = this;
        thirdPersonController.NetworkSpawnRpc(this);
        GameManager.Instance.playerAliveDict[this] = true;
    }

    [Rpc(SendTo.Server)]
    public void KillRpc()
    {
        currentCharacter.NetworkObject.Despawn(true);
        StartCoroutine(Respawn());
        GameManager.Instance.playerAliveDict[this] = false;
    }

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(1f);
        SpawnServerRpc();
    }
}

