using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonAimController : MonoBehaviour
{
    [SerializeField] protected Transform cinemachineFollowTarget;
    [SerializeField] protected CinemachineVirtualCamera virtualCamera;

    [SerializeField] public float lookSpeed = 3f;
    [SerializeField] public float lookFOV = 60f;
    [SerializeField] public bool invertAim = false;

    [SerializeField] public float topAngleClamp = 30f;
    [SerializeField] public float bottomAngleClamp = 30f;

    protected Vector3 cameraDirection = Vector3.zero;
    protected Vector3 inputAimDirection = Vector3.zero;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        cinemachineFollowTarget.transform.forward = transform.forward;
        inputAimDirection = Vector3.zero;
        virtualCamera.m_Lens.FieldOfView = lookFOV;
        virtualCamera.Follow = cinemachineFollowTarget;
    }

    public virtual void UpdateFollowTarget(Transform target)
    {
        cinemachineFollowTarget = target;
        virtualCamera.Follow = cinemachineFollowTarget;
    }

    public virtual void AimDirectionInputAction(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            Vector2 v = context.ReadValue<Vector2>();
            inputAimDirection = new Vector3(invertAim ? v.y : -v.y, v.x, 0);
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            inputAimDirection = Vector3.zero;
        }
    }

    // Update is called once per frame
    protected virtual void LateUpdate()
    {
        //Input to camera direction
        Vector3 currentvirtualCameraLookTargetRotation = Camera.main.transform.eulerAngles;
        currentvirtualCameraLookTargetRotation += inputAimDirection * lookSpeed * Time.deltaTime;
        //Clamp viewing angle
        if (currentvirtualCameraLookTargetRotation.x > 180 && currentvirtualCameraLookTargetRotation.x < 270 + bottomAngleClamp) currentvirtualCameraLookTargetRotation.x = 270 + bottomAngleClamp;
        else if (currentvirtualCameraLookTargetRotation.x < 180 && currentvirtualCameraLookTargetRotation.x > 90 - topAngleClamp) currentvirtualCameraLookTargetRotation.x = 90 - topAngleClamp;
        //Rotate cinemachineFollowTarget to direction in world coordinates
        cameraDirection = currentvirtualCameraLookTargetRotation;
        cinemachineFollowTarget.transform.eulerAngles = currentvirtualCameraLookTargetRotation;
    }
}
