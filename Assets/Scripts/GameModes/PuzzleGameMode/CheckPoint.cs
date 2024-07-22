using Unity.Netcode;
using UnityEngine;

public class CheckPoint : NetworkBehaviour
{
    public NetworkVariable<bool> Activated = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public PuzzleLevelManager puzzleLevelManager;

    void OnTriggerEnter(Collider other)
    {
        if (Activated.Value) return;
        if (puzzleLevelManager)
        {
            ThirdPersonController character = other.GetComponent<ThirdPersonController>();
            if (character != null)
            {
                ActivateRpc();
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void ActivateRpc()
    {
        puzzleLevelManager.OnCheckPointReached();
        Activated.Value = true;
    }
}
