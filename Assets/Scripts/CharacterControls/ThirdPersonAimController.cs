using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonAimController : MonoBehaviour
{
    [SerializeField] Transform cinemachineFollowTarget;
    [SerializeField] CinemachineVirtualCamera virtualCamera;

    [SerializeField] public float lookSpeed = 3f;
    [SerializeField] public float aimSpeed = 1f;
    [SerializeField] public bool invertAim = false;

    [SerializeField] public float topAngleClamp = 30f;
    [SerializeField] public float bottomAngleClamp = 30f;

    public bool isAiming = false;

    Vector3 cameraDirection = Vector3.zero;
    Vector3 inputAimDirection = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        cinemachineFollowTarget.transform.forward = transform.forward;
    }

    void Update()
    {
        virtualCamera.Follow = cinemachineFollowTarget;
    }

    public void AimDirectionInputAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 v = context.ReadValue<Vector2>();
            inputAimDirection = new Vector3(invertAim ? v.y : -v.y, v.x, 0);
        }
        else
        {
            inputAimDirection = Vector3.zero;
        }
    }

    public void AimInputAction(InputAction.CallbackContext context)
    {
        isAiming = context.performed;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        //Input to camera direction
        Vector3 currentvirtualCameraLookTargetRotation = Camera.main.transform.eulerAngles;
        currentvirtualCameraLookTargetRotation += inputAimDirection * (isAiming ? aimSpeed : lookSpeed) * Time.deltaTime;
        //Clamp viewing angle
        if (currentvirtualCameraLookTargetRotation.x > 180 && currentvirtualCameraLookTargetRotation.x < 270 + bottomAngleClamp) currentvirtualCameraLookTargetRotation.x = 270 + bottomAngleClamp;
        else if (currentvirtualCameraLookTargetRotation.x < 180 && currentvirtualCameraLookTargetRotation.x > 90 - topAngleClamp) currentvirtualCameraLookTargetRotation.x = 90 - topAngleClamp;
        //Rotate cinemachineFollowTarget to direction in world coordinates
        cameraDirection = currentvirtualCameraLookTargetRotation;
        cinemachineFollowTarget.transform.eulerAngles = cameraDirection;
    }
}
