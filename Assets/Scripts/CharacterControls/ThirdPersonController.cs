using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ThirdPersonController : MonoBehaviour
{
    [SerializeField] LayerMask ignoreRaycastMask;
    [SerializeField] GameObject virtualCameraLookTarget, groundCheckPosition;
    [SerializeField] CharacterController characterController;
    [SerializeField] Gun gun;
    [SerializeField] CinemachineVirtualCamera virtualCamera;
    [SerializeField] Animator animator;
    [SerializeField] Ragdoll ragdoll;
    [SerializeField] ThirdPersonAimController thirdPersonAimController;

    [SerializeField] float movementSpeed = 10f;
    [SerializeField] float lookSpeed = 360f;
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float drag = 10f;
    [SerializeField] float maxGroundCheckRadius = 0.05f;
    [SerializeField] bool isGrounded = false;
    [SerializeField] bool isAiming = false;

    [SerializeField] PlayerInput playerInput;
    InputAction MovementAction;
    // InputAction AimAction;
    // InputAction Interact;
    // InputAction AttackAction;
    // InputAction JumpAction;

    float distanceMovedSinceLastFrame = 0.0f;
    Vector3 lastFramePosition;
    Vector3 externalForcesVelocity = Vector3.zero;
    float gravity = 0f;
    Vector3 inputDirection = new Vector3(0, 0, 0);

    #region MonobehaviourMethods
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Physics.IgnoreLayerCollision(6, 7);
        ragdoll.DisableRagdoll();

        MovementAction = playerInput.actions.FindAction("Movement");
        // Interact = playerInput.actions.FindAction("Interact");
        // AttackAction = playerInput.actions.FindAction("Attack");
        // JumpAction = playerInput.actions.FindAction("Jump");
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = GroundCheck(maxGroundCheckRadius);
        if (isGrounded && DistanceToGround(0.1f) < 0.1f && gravity < 0f) gravity = 0f;
        gravity -= 9.81f * Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            gravity += jumpForce;
        }

        //Movement
        Vector3 cameraDirection = Camera.main.transform.forward;
        cameraDirection.y = 0;
        Vector2 v = MovementAction.ReadValue<Vector2>();
        Vector3 movementDirection = new Vector3(v.x, 0, v.y);
        movementDirection = Quaternion.LookRotation(cameraDirection.normalized) * movementDirection;
        characterController.Move(movementDirection * movementSpeed * Time.deltaTime);
        characterController.Move(externalForcesVelocity * Time.deltaTime);
        externalForcesVelocity = Vector3.Lerp(externalForcesVelocity, Vector3.zero, drag * Time.deltaTime);
        characterController.Move(gravity * transform.up * Time.deltaTime);

        isAiming = thirdPersonAimController.isAiming;
        virtualCamera.gameObject.SetActive(false);
        if (isAiming)
        {
            //rotate gun model to camera direction
            Vector3 cameraforward = Camera.main.transform.forward;
            gun.transform.forward = cameraforward;
            //rotate character model to camera direction
            cameraforward.y = 0;
            var q = Quaternion.LookRotation(cameraforward);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, q, 1000f * Time.deltaTime);
            //virtualCamera.gameObject.SetActive(true);
        }
        else if (movementDirection != Vector3.zero)
        {
            //rotate character model to movement direction
            var q = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, q, 1000f * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.Mouse0) && isAiming)
        {
            RaycastHit[] hits = Physics.RaycastAll(Camera.main.transform.position, Camera.main.transform.forward, Mathf.Infinity, ~ignoreRaycastMask);
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

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (Time.timeScale == 0f) Time.timeScale = 1f; else Time.timeScale = 0f;
        }
    }

    void FixedUpdate()
    {
        distanceMovedSinceLastFrame = (transform.position - lastFramePosition).magnitude;
        lastFramePosition = transform.position;
        UpdateAnimator();
    }
    #endregion MonoBehaviourMethods

    #region RPCs
    #endregion RPCs

    #region Methods
    bool GroundCheck(float maxDistance = 0.05f)
    {
        Collider[] hits = Physics.OverlapSphere(groundCheckPosition.transform.position, maxDistance, ~ignoreRaycastMask);
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
        RaycastHit[] hits = Physics.RaycastAll(groundCheckPosition.transform.position, -transform.up, maxDistance, ~ignoreRaycastMask);
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

    void UpdateAnimator()
    {
        if (!isGrounded)
        {
            animator.SetBool("IsInAir", true);
            animator.SetLayerWeight(animator.GetLayerIndex("InAirLayer"), 1);
        }
        else
        {
            animator.SetBool("IsInAir", false);
            animator.SetLayerWeight(animator.GetLayerIndex("InAirLayer"), 0);
        }

        if (distanceMovedSinceLastFrame > 0.1)
        {
            animator.SetBool("IsRunning", true);
            animator.SetBool("IsWalking", false);
        }
        else
        if (distanceMovedSinceLastFrame <= 0.1 && distanceMovedSinceLastFrame > 0.02)
        {
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsWalking", true);
        }
        else
        {
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsWalking", false);
        }
    }

    public void AddImpulseForce(Vector3 force)
    {
        externalForcesVelocity += force;
    }

    public void AddImpulseForceToRagdollPart(Vector3 force, string bodyPartName)
    {
        EnableRagdoll();
        ragdoll.AddForceToBodyPart(force, bodyPartName);
    }

    public void EnableRagdoll()
    {
        animator.enabled = false;
        ragdoll.EnableRagdoll();
    }

    public void DisableRagdoll()
    {
        animator.enabled = true;
        ragdoll.DisableRagdoll();
    }
    #endregion Methods
}

#region Custom Editor
#if UNITY_EDITOR
[CustomEditor(typeof(ThirdPersonController))]
public class ThirdPersonControllerEditor : Editor
{
    [SerializeField] Vector3 testForce;

    public override void OnInspectorGUI()
    {
        ThirdPersonController ThirdPersonControllerTarget = (ThirdPersonController)target;
        DrawDefaultInspector();

        testForce = EditorGUILayout.Vector3Field("Test Force", testForce);

        if (GUILayout.Button("Test Force"))
        {
            ThirdPersonControllerTarget.AddImpulseForceToRagdollPart(testForce, "Spine");
        }

        if (GUILayout.Button("Enable Ragdoll"))
        {
            ThirdPersonControllerTarget.EnableRagdoll();
        }

        if (GUILayout.Button("Disable Ragdoll"))
        {
            ThirdPersonControllerTarget.DisableRagdoll();
        }
    }
}
#endif
#endregion