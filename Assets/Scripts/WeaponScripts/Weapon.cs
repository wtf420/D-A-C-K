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
        if (IsClient && !IsHost) SyncDataAsLateJoiner();
        networkSpawned = true;
    }

    public void SyncDataAsLateJoiner()
    {
        wielderNetworkBehaviourReference.Value.TryGet(out ThirdPersonController player);
        Debug.Log(player);
        SetWielder(player);
    }

    public virtual void SetWielder(ThirdPersonController player)
    {
        wielder = player;
        if (IsServer && player != null) wielderNetworkBehaviourReference.Value = player;
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
