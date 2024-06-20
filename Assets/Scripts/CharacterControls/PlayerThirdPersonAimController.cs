using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerThirdPersonAimController : ThirdPersonAimController
{
    [SerializeField] public float aimSpeed = 1f;
    [SerializeField] public float aimFOV = 20f;
    [HideInInspector] public bool isAiming = false;
    [SerializeField] protected CinemachineVirtualCamera aimVirtualCamera;

    // Start is called before the first frame update
    protected override void Start()
    {
        if (!IsOwner)
        {
            virtualCamera.Follow = null;
            virtualCamera.LookAt = null;
            virtualCamera.gameObject.SetActive(false);
            aimVirtualCamera.Follow = null;
            aimVirtualCamera.LookAt = null;
            aimVirtualCamera.gameObject.SetActive(false);
        }
        else
        {
            virtualCamera.Follow = cinemachineFollowTarget;
            virtualCamera.gameObject.SetActive(true);
            virtualCamera.Follow = cinemachineFollowTarget;
            aimVirtualCamera.gameObject.SetActive(false);

            inputAimDirection = Vector3.zero;
            virtualCamera.m_Lens.FieldOfView = lookFOV;
        }
        isAiming = false;
    }

    public void AimInputAction(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            isAiming = true;
            aimVirtualCamera.gameObject.SetActive(true);
            virtualCamera.gameObject.SetActive(false);
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            isAiming = false;
            aimVirtualCamera.gameObject.SetActive(false);
            virtualCamera.gameObject.SetActive(true);
        }
    }

    protected override void LateUpdate()
    {
        if (!IsOwner || cinemachineFollowTarget == null) return;
        cameraDirection += inputAimDirection * (isAiming ? aimSpeed : lookSpeed) * Time.unscaledDeltaTime;
        cameraDirection.x = Mathf.Clamp(cameraDirection.x, -90f + bottomAngleClamp, 90f - topAngleClamp);
        cinemachineFollowTarget.transform.rotation = Quaternion.Euler(cameraDirection);
    }
}
