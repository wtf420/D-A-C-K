using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonAimController : MonoBehaviour
{
    [SerializeField] Transform cinemachineFollowTarget;
    [SerializeField] CinemachineVirtualCamera virtualCamera;
    [SerializeField] float lookSpeed = 360f;
    [SerializeField] bool inverted = false;
    [SerializeField] float topAngleClamp = 30f;
    [SerializeField] float bottomAngleClamp = 30f;

    Vector3 cameraDirection = Vector3.zero;

    [SerializeField] PlayerInput playerInput;
    InputAction LookAction;
    InputAction AimAction;

    public bool isAiming { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        cinemachineFollowTarget.transform.forward = transform.forward;
        LookAction = playerInput.actions.FindAction("Look");
        AimAction = playerInput.actions.FindAction("Aim");
    }

    void Update()
    {
        isAiming = AimAction.IsInProgress();
        virtualCamera.Follow = cinemachineFollowTarget;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (LookAction.IsInProgress())
        {
            //Input to camera direction
            Vector2 v = LookAction.ReadValue<Vector2>();
            Vector3 cameraLookMovementDirection = new Vector3(inverted ? v.y : -v.y, v.x, 0);
            Vector3 currentvirtualCameraLookTargetRotation = Camera.main.transform.eulerAngles;
            currentvirtualCameraLookTargetRotation += cameraLookMovementDirection * lookSpeed * Time.deltaTime;
            //Clamp viewing angle
            if (currentvirtualCameraLookTargetRotation.x > 180 && currentvirtualCameraLookTargetRotation.x < 270 + bottomAngleClamp) currentvirtualCameraLookTargetRotation.x = 270 + bottomAngleClamp;
            else if (currentvirtualCameraLookTargetRotation.x < 180 && currentvirtualCameraLookTargetRotation.x > 90 - topAngleClamp) currentvirtualCameraLookTargetRotation.x = 90 - topAngleClamp;
            //Rotate cinemachineFollowTarget to direction in world coordinates
            cameraDirection = currentvirtualCameraLookTargetRotation;
            cinemachineFollowTarget.transform.eulerAngles = cameraDirection;
        }
    }
}
