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
        base.Start();
        isAiming = false;
        aimVirtualCamera.gameObject.SetActive(false);
        virtualCamera.gameObject.SetActive(true);
        aimVirtualCamera.Follow = cinemachineFollowTarget;
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
        //Input to camera direction
        Vector3 currentvirtualCameraLookTargetRotation = Camera.main.transform.eulerAngles;
        currentvirtualCameraLookTargetRotation += inputAimDirection * (isAiming ? aimSpeed : lookSpeed) * Time.deltaTime;
        //Clamp viewing angle
        if (currentvirtualCameraLookTargetRotation.x > 180 && currentvirtualCameraLookTargetRotation.x < 270 + bottomAngleClamp) currentvirtualCameraLookTargetRotation.x = 270 + bottomAngleClamp;
        else if (currentvirtualCameraLookTargetRotation.x < 180 && currentvirtualCameraLookTargetRotation.x > 90 - topAngleClamp) currentvirtualCameraLookTargetRotation.x = 90 - topAngleClamp;
        //Rotate cinemachineFollowTarget to direction in world coordinates
        cameraDirection = currentvirtualCameraLookTargetRotation;
        cinemachineFollowTarget.transform.eulerAngles = currentvirtualCameraLookTargetRotation;
    }
}
