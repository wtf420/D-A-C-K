using System.Collections.Generic;
using Cinemachine;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonSpectatorController : Playable
{
    [Header("~*// Controls")]
    [SerializeField] CinemachineVirtualCamera virtualCamera;
    [SerializeField] ThirdPersonAimController thirdPersonAimController;
    [HideInInspector] public new Camera camera;

    [Header("~*// Player Input")]
    [SerializeField] PlayerInput playerInput;

    [Header("~*// UI")]
    [SerializeField] Canvas screenCanvas;
    [SerializeField] Canvas worldCanvas;
    [SerializeField] TMP_Text currentlySpectatingText;

    [field: Header("~* NETWORKING")]
    public NetworkPlayer controlPlayer;

    [Header("~*// Spectating")]
    [SerializeField] Transform spectatingTarget;
    [SerializeField] int targetIndex;

    #region Monobehaviour & NetworkBehaviour
    void Start()
    {
        camera = Camera.main;
        targetIndex = 0;
        spectatingTarget = null;

        if (!IsOwner)
        {
            virtualCamera.Priority = 0;

            thirdPersonAimController.enabled = false;
            thirdPersonAimController.SetFollowTarget(null);
            playerInput.enabled = false;
            screenCanvas.gameObject.SetActive(false);
            return;
        }
        else
        {
            //Initialize as owner
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            thirdPersonAimController.enabled = true;
            thirdPersonAimController.SetFollowTarget(transform);
            playerInput.enabled = true;
            screenCanvas.gameObject.SetActive(true);
        }
    }

    //sync or create network data
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += SyncDataAsLateJoiner;
        OnNetworkPlayerDataChanged();
        GoToNextTarget();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
    }
    #endregion

    public void OnNetworkPlayerDataChanged()
    {
        controlPlayerNetworkBehaviourReference.Value.TryGet(out NetworkPlayer networkPlayer);
        controlPlayer = networkPlayer;
    }

    public void SyncDataAsLateJoiner(ulong clientId)
    {
        if (clientId != NetworkManager.LocalClientId) return;
        if (IsClient && !IsHost)
        {
            if (controlPlayerNetworkBehaviourReference.Value.TryGet(out NetworkPlayer player))
            {
                controlPlayer = player;
                OnNetworkPlayerDataChanged();
            }
        }
        NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= SyncDataAsLateJoiner;
    }

    #region Player control
    public void AttackInputAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            GoToNextTarget();
        }
    }

    public void AimInputAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            GoToPreviousTarget();
        }
    }

    public void GoToNextTarget()
    {
        List<PlayerLevelInfo> infos = LevelManager.Instance.PlayerNetworkListToNormalList();
        for (int i = targetIndex + 1; i < infos.Count - 1; i++)
        {
            if (i >= infos.Count) break;
            if (infos[i].character.TryGet(out ThirdPersonController character))
            {
                thirdPersonAimController.SetFollowTarget(character.cinemachineFollowTarget.transform);
                currentlySpectatingText.text = "Currently spectating: " + infos[i].playerName.ToString();
                targetIndex = i;
                return;
            }
        }
        for (int i = 0; i <= targetIndex; i++)
        {
            if (i >= infos.Count) break;
            if (infos[i].character.TryGet(out ThirdPersonController character))
            {
                thirdPersonAimController.SetFollowTarget(character.cinemachineFollowTarget.transform);
                currentlySpectatingText.text = "Currently spectating: " + infos[i].playerName.ToString();
                targetIndex = i;
                return;
            }
        }
        thirdPersonAimController.SetFollowTarget(transform);
        currentlySpectatingText.text = "Currently spectating: None.";
        Debug.Log("No Spectate-able found");
    }

    public void GoToPreviousTarget()
    {
        List<PlayerLevelInfo> infos = LevelManager.Instance.PlayerNetworkListToNormalList();
        for (int i = targetIndex - 1; i >= 0; i--)
        {
            if (i < 0) break;
            if (infos[i].character.TryGet(out ThirdPersonController character))
            {
                thirdPersonAimController.SetFollowTarget(character.cinemachineFollowTarget.transform);
                currentlySpectatingText.text = "Currently spectating: " + infos[i].playerName.ToString();
                targetIndex = i;
                return;
            }
        }
        for (int i = infos.Count - 1; i >= targetIndex; i--)
        {
            if (i < 0) break;
            if (infos[i].character.TryGet(out ThirdPersonController character))
            {
                thirdPersonAimController.SetFollowTarget(character.cinemachineFollowTarget.transform);
                currentlySpectatingText.text = "Currently spectating: " + infos[i].playerName.ToString();
                targetIndex = i;
                return;
            }
        }
        thirdPersonAimController.SetFollowTarget(transform);
        currentlySpectatingText.text = "Currently spectating: None.";
        Debug.Log("No Spectate-able found");
    }
    #endregion
}
