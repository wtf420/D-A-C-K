using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ThirdPersonController : NetworkBehaviour
{
    [Header("~*// Constants")]
    [SerializeField] LayerMask ignoreRaycastMask;

    [Header("~*// Movement")]
    [SerializeField] CharacterController characterController;
    [SerializeField] GameObject groundCheckPosition;
    [SerializeField] float movementSpeed = 10f;
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float drag = 10f;
    [SerializeField] float maxGroundCheckRadius = 0.05f;
    Vector3 lastFramePosition;
    Vector3 externalForcesVelocity = Vector3.zero;
    float distanceMovedSinceLastFrame = 0.0f; // because distanceMovedSinceLastFrame is unreliable as heck
    float verticalVelocity = 0f;

    [Header("~*// Controls")]
    [SerializeField] CinemachineVirtualCamera virtualCamera;
    [SerializeField] PlayerThirdPersonAimController playerThirdPersonAimController;
    [SerializeField] float maxFallingTime = 0.5f;
    protected new Camera camera;
    bool isAiming => playerThirdPersonAimController.isAiming;
    bool isGrounded = false;
    bool movementEnabled = true;
    float currentFallingTimer = 0.0f;


    [Header("~*// Interact system")]
    [SerializeField] float rangeToInteract = 5.0f;

    [Header("~*// Animations & Ragdoll")]
    [SerializeField] Animator animator;
    [SerializeField] Ragdoll ragdoll;
    [SerializeField] Material shirtMaterial;
    [SerializeField] new Renderer renderer;

    [Header("~*// Player Input")]
    [SerializeField] PlayerInput playerInput;
    Vector3 inputMovementDirection = new Vector3(0, 0, 0);

    [Header("~*// UI")]
    [SerializeField] Canvas screenCanvas;
    [SerializeField] ButtonPrompt buttonPromptPrefab;
    ButtonPrompt currentButtonPrompt;
    Interactable closestInteractable = null;

    // [Header("~* Combat")]
    // [SerializeField] Gun gun;
    [field: Header("~* NETWORKING")]
    [field: SerializeField] public new NetworkObject NetworkObject { get; private set;}
    public NetworkPlayer controlPlayer;
    public NetworkVariable<NetworkBehaviourReference> controlPlayerNetworkBehaviourReference = new NetworkVariable<NetworkBehaviourReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> ragdollEnabled = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> networkSpawned = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    #region Monobehaviour & NetworkBehaviour
    // Start is called before the first frame update
    void Start()
    {
        camera = Camera.main;
        Physics.IgnoreLayerCollision(6, 7); //so the ragdoll and character controller dont interact with each other
        ragdoll.DisableRagdoll();
        shirtMaterial = renderer.materials.FirstOrDefault((x) => x.name == "ShirtColor (Instance)");

        if (!IsOwner)
        {
            virtualCamera.Priority = 0;

            ragdoll.outline.enabled = false;
            ragdoll.outline.enabled = true;
            ragdoll.outline.OutlineColor = Color.red;

            playerThirdPersonAimController.enabled = false;
            playerInput.enabled = false;
            return;
        }
        else
        {
            //Initialize as owner
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            //reenable outline so it will render properly
            networkSpawned.Value = true;
            ragdoll.outline.enabled = false;
            ragdoll.outline.enabled = true;
            ragdoll.outline.OutlineColor = Color.green;

            playerThirdPersonAimController.enabled = true;
            playerInput.enabled = true;
        }
        StartCoroutine(InitializeData());
    }

    //sync or create network data
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    // Update is called once per frame
    void Update()
    {
        if (!networkSpawned.Value) return;
        isGrounded = GroundCheck(maxGroundCheckRadius);
        if (!IsOwner) return;
        if (movementEnabled) PlayerMovement();
        CheckForInteractables();
    }

    void FixedUpdate()
    {
        if (!networkSpawned.Value) return;
        distanceMovedSinceLastFrame = (transform.position - lastFramePosition).magnitude;
        lastFramePosition = transform.position;
        if (!IsOwner) return;
        UpdateAnimator();
    }
    #endregion MonoBehaviourMethods

    #region RPCs
    [Rpc(SendTo.Server)] //Server mark complete server spawn process
    public void NetworkSpawnRpc(NetworkBehaviourReference networkBehaviourReference)
    {
        controlPlayerNetworkBehaviourReference.Value = networkBehaviourReference;
        //networkSpawned.Value = true;
    }

    [Rpc(SendTo.Everyone)]
    public void KillRpc()
    {
        EnableRagdoll();
        if (IsServer) controlPlayer.KillRpc();
    }
    #endregion RPCs

    #region Input actions
    public void MovementDirectionInputAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 v = context.ReadValue<Vector2>();
            inputMovementDirection = new Vector3(v.x, 0, v.y);
        } else
        {
            inputMovementDirection = Vector3.zero;
        }
    }

    public void JumpInputAction(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            verticalVelocity += jumpForce;
        }
    }

    public void InteractInputAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            //if (Time.timeScale == 0f) Time.timeScale = 1f; else Time.timeScale = 0f;
            if (Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            } else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    public void AttackInputAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PlayerAttack();
        }
    }
    #endregion

    #region Player control
    public void PlayerMovement()
    {
        float distanceToGround = DistanceToGround(5f);
        //check falling state
        if (isGrounded) currentFallingTimer = 0.0f;
        else
        {
            currentFallingTimer += Time.deltaTime;
            if (currentFallingTimer > maxFallingTime && distanceToGround == Mathf.Infinity && controlPlayer != null)
            {
                controlPlayer.KillRpc();
                return;
            }
        }

        // Applying gravity
        if (isGrounded && distanceToGround < 0.1f && verticalVelocity < 0f) verticalVelocity = 0f;
        verticalVelocity -= 9.81f * Time.deltaTime;

        //Move player relative to camera direction
        Vector3 cameraDirection = camera.transform.forward;
        cameraDirection.y = 0;
        Vector3 relativeMovementDirection = Quaternion.LookRotation(cameraDirection.normalized) * inputMovementDirection;
        characterController.Move(relativeMovementDirection * movementSpeed * Time.deltaTime);

        //Applying physics
        characterController.Move(externalForcesVelocity * Time.deltaTime);
        externalForcesVelocity = Vector3.Lerp(externalForcesVelocity, Vector3.zero, drag * Time.deltaTime);

        //Applying gravity & jump
        characterController.Move(verticalVelocity * transform.up * Time.deltaTime);

        PlayerRotation();
    }

    public void PlayerRotation()
    {
        if (isAiming)
        {
            Vector3 cameraforward = camera.transform.forward;
            //rotate character model to camera direction
            cameraforward.y = 0;
            var q = Quaternion.LookRotation(cameraforward);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, q, 1000f * Time.deltaTime);
        }
        else if (inputMovementDirection != Vector3.zero)
        {
            //rotate character model to movement direction
            Vector3 cameraDirection = camera.transform.forward;
            cameraDirection.y = 0;
            Vector3 relativeMovementDirection = Quaternion.LookRotation(cameraDirection.normalized) * inputMovementDirection;
            var q = Quaternion.LookRotation(relativeMovementDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, q, 1000f * Time.deltaTime);
        }
    }

    public void PlayerAttack()
    {
        RaycastHit[] hits = Physics.RaycastAll(camera.transform.position, camera.transform.forward, Mathf.Infinity, ~ignoreRaycastMask);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && !hit.collider.isTrigger)
            {
                Debug.Log(hit.collider.gameObject.ToString());
                Debug.DrawLine(this.transform.position, hit.point, Color.red, 0.5f);
                return;
            }
            else continue;
        }
        Debug.DrawLine(this.transform.position, this.transform.position + transform.forward * 100f, Color.red, 0.5f);
    }

    void CheckForInteractables()
    {
        float minDistance;
        if (closestInteractable == null) minDistance = Mathf.Infinity;
        else 
        {
            minDistance = Vector3.Distance(this.transform.position, closestInteractable.transform.position);
            if (minDistance > rangeToInteract)
            {
                minDistance = Mathf.Infinity;
                closestInteractable = null;
            }
        }

        //search for nearest interactable
        foreach (Collider c in Physics.OverlapSphere(this.transform.position, rangeToInteract, ~ignoreRaycastMask))
        {
            Interactable interactable = c.gameObject.GetComponent<Interactable>();

            if (interactable && interactable.isInteractable && closestInteractable != interactable)
            {
                float distance = Vector3.Distance(this.transform.position, interactable.transform.position);
                if (distance < minDistance)
                {
                    closestInteractable = interactable;
                    minDistance = Vector3.Distance(this.transform.position, interactable.transform.position);
                }
            }
        }

        //Update interactable prompt
        if (currentButtonPrompt != null && closestInteractable == null) Destroy(currentButtonPrompt.gameObject);
        else if (closestInteractable != null)
        {
            if (currentButtonPrompt == null)
            {
                currentButtonPrompt = Instantiate(buttonPromptPrefab, screenCanvas.transform, false);
                currentButtonPrompt.SetText("Interact");
                //currentButtonPrompt.SetText(playerInput.currentActionMap.FindAction("Interact").GetBindingDisplayString(0));
            }
            currentButtonPrompt.SetPosition(camera.WorldToScreenPoint(closestInteractable.gameObject.transform.position));
        }
    }
    #endregion

    #region Methods
    // Set controlPlayer before use!
    IEnumerator InitializeData()
    {
        yield return new WaitUntil(() => networkSpawned.Value);
        // if (controlPlayerNetworkBehaviourReference.Value.TryGet(out NetworkPlayer player))
        // {
        //     this.controlPlayer = player;
        //     if (shirtMaterial != null && UnityEngine.ColorUtility.TryParseHtmlString(controlPlayer.playerColor.Value.ToString(), out Color color))
        //     {
        //         shirtMaterial.color = color;
        //     }
        // }
    }

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

    // EnableRagdoll() before use!
    public void AddImpulseForceToRagdollPart(Vector3 force, string bodyPartName)
    {
        ragdoll.AddForceToBodyPart(force, bodyPartName);
    }

    public void EnableRagdoll()
    {
        animator.enabled = false;
        movementEnabled = false;
        ragdollEnabled.Value = true;
        characterController.detectCollisions = false;
        ragdoll.EnableRagdoll();
    }

    public void DisableRagdoll()
    {
        animator.enabled = true;
        movementEnabled = true;
        ragdollEnabled.Value = false;
        characterController.detectCollisions = true;
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