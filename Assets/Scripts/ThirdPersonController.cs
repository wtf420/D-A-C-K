using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class ThirdPersonController : MonoBehaviour
{
    [SerializeField] GameObject virtualCameraLookTarget, groundCheckPosition;
    [SerializeField] CharacterController characterController;
    [SerializeField] float movementSpeed = 10f;
    [SerializeField] float lookSpeed = 360f;
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float playerVerticalVelocity = 0.000f;
    [SerializeField] float maxGroundCheckRadius = 0.05f;
    [SerializeField] bool isGrounded = false;
    [SerializeField] bool isAiming = false;

    [SerializeField] Gun gun;
    [SerializeField] CinemachineVirtualCamera virtualCamera;

    private Vector3 cameraDirection = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
        virtualCameraLookTarget.transform.eulerAngles = cameraDirection;
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = GroundCheck(maxGroundCheckRadius);
        if (isGrounded && DistanceToGround(0.1f) < 0.1f && playerVerticalVelocity < 0f) playerVerticalVelocity = 0f;
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

        isAiming = Input.GetKey(KeyCode.Mouse1);
        virtualCamera.gameObject.SetActive(false);
        if (isAiming)
        {
            Vector3 cameraforward = Camera.main.transform.forward;
            gun.transform.forward = cameraforward;
            cameraforward.y = 0;
            var q = Quaternion.LookRotation(cameraforward);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, q, 1000f * Time.deltaTime);
            virtualCamera.gameObject.SetActive(true);
        }
        else if (movementDirection != Vector3.zero)
        {
            var q = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, q, 1000f * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            RaycastHit[] hits = Physics.RaycastAll(Camera.main.transform.position, Camera.main.transform.forward, Mathf.Infinity, ~LayerMask.GetMask("CharacterController"));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider != null && !hit.collider.isTrigger)
                {
                    Debug.Log(hit.collider.gameObject.ToString());
                    Debug.DrawLine(gun.transform.position, hit.point, Color.red, 0.5f);
                    break;
                }
                else continue;
            }
        }
    }

    void LateUpdate()
    {
        //Camera movement
        Vector3 cameraLookMovementDirection = new Vector3(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"), 0);
        Vector3 currentvirtualCameraLookTargetRotation = Camera.main.transform.eulerAngles;
        currentvirtualCameraLookTargetRotation += cameraLookMovementDirection * lookSpeed * Time.deltaTime;
        currentvirtualCameraLookTargetRotation.z = 0;
        if (currentvirtualCameraLookTargetRotation.x > 180 && currentvirtualCameraLookTargetRotation.x < 270 + 30) currentvirtualCameraLookTargetRotation.x = 270 + 30;
        else if (currentvirtualCameraLookTargetRotation.x < 180 && currentvirtualCameraLookTargetRotation.x > 90 - 30) currentvirtualCameraLookTargetRotation.x = 90 - 30;
        cameraDirection = currentvirtualCameraLookTargetRotation;
        virtualCameraLookTarget.transform.eulerAngles = cameraDirection;
    }

    bool GroundCheck(float maxDistance = 0.05f)
    {
        Collider[] hits = Physics.OverlapSphere(groundCheckPosition.transform.position, maxDistance, ~LayerMask.GetMask("Player"));
        foreach (Collider hit in hits)
        {
            if (hit != null && !hit.isTrigger)
            {
                return true;
            }
        }
        return false;
    }

    float DistanceToGround(float maxDistance = 0.05f)
    {
        RaycastHit[] hits = Physics.RaycastAll(groundCheckPosition.transform.position, -transform.up, maxDistance, ~LayerMask.GetMask("Player"));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && !hit.collider.isTrigger)
            {
                return hit.distance;
            }
            else continue;
        }
        return Mathf.Infinity;
    }
}
