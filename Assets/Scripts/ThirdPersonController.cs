using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonController : MonoBehaviour
{
    [SerializeField] GameObject virtualCameraLookTarget, groundCheckPosition;
    [SerializeField] CharacterController characterController;
    [SerializeField] float movementSpeed = 10f;
    [SerializeField] float lookSpeed = 360f;
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float playerVerticalVelocity = 0.000f;
    [SerializeField] float maxGroundCheckDistance = 0.05f;
    [SerializeField] bool isGrounded = false;

    private Vector3 cameraDirection = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        virtualCameraLookTarget.transform.eulerAngles = cameraDirection;
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = GroundCheck(maxGroundCheckDistance);
        if (isGrounded && playerVerticalVelocity < 0f) playerVerticalVelocity = 0f;
        playerVerticalVelocity -= 9.81f * Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            playerVerticalVelocity += jumpForce;
        }

        //Movement
        Vector3 cameraDirection = Camera.main.transform.forward;
        cameraDirection.y = 0;
        Vector3 movementDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        movementDirection = Quaternion.LookRotation(cameraDirection.normalized) * movementDirection;
        characterController.Move(movementDirection * movementSpeed * Time.deltaTime);
        characterController.Move(new Vector3(0, playerVerticalVelocity, 0) * Time.deltaTime);

        if (movementDirection != Vector3.zero)
        {
            var q = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, q, 1000f * Time.deltaTime);
        }
    }

    void LateUpdate()
    {
        //Camera movement
        Vector3 cameraLookMovementDirection = new Vector3(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"), 0);
        Vector3 currentvirtualCameraLookTargetRotation = cameraDirection;
        currentvirtualCameraLookTargetRotation += cameraLookMovementDirection * lookSpeed * Time.deltaTime;
        currentvirtualCameraLookTargetRotation.z = 0;
        if (currentvirtualCameraLookTargetRotation.x > 180 && currentvirtualCameraLookTargetRotation.x < 270 + 30) currentvirtualCameraLookTargetRotation.x = 270 + 30;
        else if (currentvirtualCameraLookTargetRotation.x < 180 && currentvirtualCameraLookTargetRotation.x > 90 - 30) currentvirtualCameraLookTargetRotation.x = 90 - 30;
        cameraDirection = currentvirtualCameraLookTargetRotation;
        virtualCameraLookTarget.transform.eulerAngles = cameraDirection;
    }

    bool GroundCheck(float maxDistance = 0.05f)
    {
        bool result = false;
        RaycastHit[] hits = Physics.RaycastAll(groundCheckPosition.transform.position, -transform.up, maxDistance, ~LayerMask.GetMask("Player"));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && !hit.collider.isTrigger)
            {
                result = true;
                break;
            }
            else continue;
        }
        return result;
    }
}
