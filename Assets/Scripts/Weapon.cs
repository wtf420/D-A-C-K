using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

public class Weapon : NetworkBehaviour
{
    public ThirdPersonController wielder;
    public ParentConstraint parentConstraint;
    public float range, attackAngle, minDistance;
    public float coolDown, timing;
    public float damage;
    private bool isAttackable = true;

    public NetworkVariable<bool> networkSpawned = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<NetworkBehaviourReference> wielderNetworkBehaviourReference = new NetworkVariable<NetworkBehaviourReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Vector3 constPositionOffset = new Vector3(0.25f, -0.25f, 0.01f);
    // Vector3 constRotationOffset = new Vector3(0f, 0f, 90f);

    //sync or create network data
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            networkSpawned.Value = true;
        } else if (networkSpawned.Value)
        {
            //sync late joiner data
            if (wielderNetworkBehaviourReference.Value.TryGet(out ThirdPersonController player))
            {
                wielder = player;
                DoStuff(player);
            }
        }
    }

    public void DoStuff(ThirdPersonController player)
    {
        ConstraintSource constraintSource = new ConstraintSource
        {
            sourceTransform = player.weaponHeldTransform,
            weight = 1,
        };
        parentConstraint.SetSource(0, constraintSource);
    }

    [Rpc(SendTo.Server)]
    public void SetWielderRpc(NetworkBehaviourReference networkBehaviourReference)
    {
        if (networkBehaviourReference.TryGet(out ThirdPersonController player))
        {
            wielder = player;
            wielderNetworkBehaviourReference.Value = player;
            player.SetWieldWeaponRpc(this);
            DoStuff(player);
        }
    }

    [Rpc(SendTo.Server)]
    public void AttemptAttackRpc()
    {
        if (isAttackable && wielder != null)
        {
            Attack();
            StartCoroutine(CoolDown());
        }
    }

    private void Attack()
    {
        StartCoroutine(MeleeAttack());
    }

    private IEnumerator MeleeAttack()
    {
        yield return new WaitForSeconds(timing);
        Hit();
    }

    private void Hit()
    {
        RaycastHit[] info = Physics.SphereCastAll(wielder.transform.position, range, Vector3.up, 0, ~LayerMask.GetMask("PlayerRagdoll"));
        Debug.Log("Hit objects: " + info.Length);
        foreach (RaycastHit hit in info)
        {
            //check if theres a wall between
            if (wielder.gameObject == hit.collider.gameObject) continue;
            bool c = false;
            Vector3 hitlocation = (hit.point == Vector3.zero) ? hit.transform.position : hit.point;
            Debug.DrawLine(hitlocation, wielder.transform.position, Color.red, 1f);
            RaycastHit[] info2 = Physics.RaycastAll(this.transform.position, hitlocation - this.transform.position, Vector3.Distance(this.transform.position, hit.transform.position), ~LayerMask.GetMask("PlayerRagdoll"));
            Debug.Log("Considering: " + hit.collider.gameObject);
            foreach (RaycastHit hit2 in info2)
            {
                if (hit2.collider.gameObject != hit.collider.gameObject && hit2.collider.gameObject != this.gameObject && hit.transform.tag == hit2.transform.tag)
                {
                    c = true;
                    Debug.Log("break: " + hit2.collider.gameObject + " is in the way");
                    break;
                }
            }
            if (c) continue; else Debug.Log("Considered: " + hit.collider.gameObject + " has no gameobject in between!");

            ThirdPersonController hitCharacter = hit.collider.gameObject.GetComponent<ThirdPersonController>();
            if (hitCharacter != null)
            {
                //handle hit
                hitCharacter.AddImpulseForceRpc(wielder.transform.forward * 10f);
                hitCharacter.TakeDamageRpc(damage);
            }
            //Debug.DrawLine(this.transform.position, hitlocation, Color.green, 5f);
        }
    }

    private IEnumerator CoolDown()
    {
        isAttackable = false;
        yield return new WaitForSeconds(coolDown);
        isAttackable = true;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Weapon))]
public class WeaponEditor : Editor
{
    [SerializeField] ThirdPersonController targetPlayer;

    public override void OnInspectorGUI()
    {
        Weapon weapon = (Weapon)target;
        DrawDefaultInspector();

        targetPlayer = (ThirdPersonController)EditorGUILayout.ObjectField(targetPlayer, typeof(ThirdPersonController), true);

        if (GUILayout.Button("Test"))
        {
            weapon.SetWielderRpc(targetPlayer);
            targetPlayer.SetWieldWeaponRpc(weapon);
        }
    }
}
#endif
