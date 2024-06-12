using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;

public class Weapon : NetworkBehaviour
{
    [field: SerializeField] public ParentConstraint parentConstraint;
    [field: SerializeField] public ThirdPersonController wielder { get; protected set; }
    [field: SerializeField] public AnimatorOverrideController animatorOverrideController { get; protected set; }

    public float delayBetweenAttacks;
    public bool isAttackable { get; protected set; } = true;
    public bool networkSpawned { get; protected set; } = false;

    public NetworkVariable<NetworkBehaviourReference> wielderNetworkBehaviourReference = new NetworkVariable<NetworkBehaviourReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    //sync or create network data
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        networkSpawned = true;

        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += SyncDataAsLateJoiner;
        wielderNetworkBehaviourReference.OnValueChanged += OnWielderChanged;

        Debug.Log(NetworkManager.LocalClientId + " has spawned ");
    }

    public virtual void SyncDataAsLateJoiner(ulong clientID)
    {
        Debug.Log(this + "SyncDataAsLateJoiner: " + NetworkObjectId);
        if (clientID == NetworkManager.LocalClientId)
        {
            if (IsClient && !IsHost)
            {
                wielderNetworkBehaviourReference.Value.TryGet(out ThirdPersonController player);
                wielder = player;
            }
            NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
        wielderNetworkBehaviourReference.OnValueChanged -= OnWielderChanged;
    }

    public virtual void OnWielderChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
    {
        if (current.TryGet(out ThirdPersonController character))
        {
            wielder = character;
        }
    }

    [Rpc(SendTo.Server)]
    public virtual void AttemptAttackRpc()
    {
        if (isAttackable && wielder != null)
        {
            Attack();
        }
    }

    protected virtual void Attack()
    {
        // attack code goes here
        StartCoroutine(CoolDown());
    }

    protected virtual IEnumerator CoolDown()
    {
        isAttackable = false;
        yield return new WaitForSeconds(delayBetweenAttacks);
        isAttackable = true;
    }
}
