using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public enum CharacterType
{
    Pusher,
    Puller
}

public class CustomCharacterController : NetworkBehaviour
{
    [Header("~*// PARAMETERS" )]
    [SerializeField] public CharacterType characterType = CharacterType.Pusher;
    [SerializeField] public Interactable thisInteractable;
    [SerializeField] public NetworkObject networkObject;

    [Header("~* Movements and Controls" )]
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected float jumpForce;
    [SerializeField] protected float gravityForce;
    [SerializeField] protected float airMovementMultiplier;
    [SerializeField] protected float cameraRotationSpeed;
    [Header("~* Others" )]
    [SerializeField] protected float rangeToInteract;
    [SerializeField] protected float rangeToPush;
    [SerializeField] protected float pushForce;
    
    
    [Header("~*// OBJECTS & COMPONENTS" )]
    [SerializeField] protected CinemachineVirtualCamera virtualCamera;
    [SerializeField] protected Canvas ScreenCanvas;
    [SerializeField] protected PlayerInput playerInput;
    [SerializeField] protected GameObject ragdoll;
    protected CharacterController characterController;
    protected new Rigidbody rigidbody;
    protected Animator animator;
    protected Outline outline;

    [Header("~*// OTHERS")]
    [SerializeField] public Rigidbody ragdollCenterRigidbody;
    [SerializeField] public GameObject pickupPosition;


    [Header("~*// VARIABLES" )]
    protected ButtonPrompt btnPrompt;
    protected Interactable closestInteractable;
    protected Vector3 inputDirection;
    protected Vector3 characterVelocity;
    protected Vector3 lastFramePosition; // because distanceMovedSinceLastFrame is unreliable as heck
    protected float cameraInput;
    protected float distanceMovedSinceLastFrame;
    protected bool isGrounded;
    public bool ragdollEnabled { get; protected set; }

    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        rigidbody = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        inputDirection = Vector3.zero;
        characterVelocity = Vector3.zero;
        lastFramePosition = this.transform.position;
        outline = GetComponentInChildren<Outline>();

        //thisInteractable.InteractEvent.AddListener(ToggleRagdollRpc);
        DisableRagdoll();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) 
        {
            virtualCamera.Priority = 0;
            outline.OutlineColor = Color.red;
            return;
        }
        virtualCamera.Priority = 10;

        isGrounded = characterController.isGrounded;
        Debug.Log(characterVelocity);
        
        if (!ragdollEnabled)
        {
            RotateCharacterTowards();
            MoveCharacter();
        }
        UpdateCamera();
        UpdateAnimator();
        CheckForInteractables();
    }

    void CheckForInteractables()
    {
        foreach (Collider c in Physics.OverlapSphere(this.transform.position, rangeToInteract))
        {
            Interactable interactable = c.gameObject.GetComponent<Interactable>();
            if (interactable && interactable != thisInteractable )
            {
                closestInteractable = c.gameObject.GetComponent<Interactable>();
                if (btnPrompt != null) Destroy(btnPrompt.gameObject);
                if (closestInteractable.isInteractable)
                {
                    btnPrompt = ButtonPrompt.Create();
                    btnPrompt.transform.SetParent(ScreenCanvas.transform);
                    btnPrompt.SetText(playerInput.currentActionMap.FindAction("Interact").GetBindingDisplayString(0));
                    btnPrompt.SetPosition(Camera.main.WorldToScreenPoint(closestInteractable.gameObject.transform.position));
                }
                return;
            }
        }
        if (btnPrompt != null) Destroy(btnPrompt.gameObject);
    }

    void FixedUpdate()
    {
        distanceMovedSinceLastFrame = (transform.position - lastFramePosition).magnitude;
        lastFramePosition = transform.position;
    }

    void UpdateCamera()
    {
        Vector3 _rotation = virtualCamera.transform.rotation.eulerAngles;
        if (virtualCamera != null)
        {
            _rotation.y += cameraInput * cameraRotationSpeed * Time.deltaTime;
        }
        virtualCamera.transform.rotation = Quaternion.Euler(_rotation);
    }

    void MoveCharacter()
    {
        if (characterController.enabled == false) return;
        float _moveSpeed = moveSpeed;

        Vector3 camToPlayer = this.transform.position - virtualCamera.transform.position;
        camToPlayer.y = 0;
        Vector3 movementDirection = Quaternion.LookRotation(camToPlayer.normalized) * inputDirection;
        Vector3 movement = movementDirection.normalized * _moveSpeed;
        movement = isGrounded ? movement : movement * airMovementMultiplier;
        characterController.Move(movement * Time.deltaTime);

        if (isGrounded && characterVelocity.y < 0)
        {
            characterVelocity.y = 0;
        }
        Vector3 gravity = -transform.up * gravityForce;
        characterVelocity += gravity * Time.deltaTime;
        characterController.Move(characterVelocity);
    }

    public void PickUpPlayerRpc(GameObject playerGameObject)
    {
        CustomCharacterController player = playerGameObject.GetComponentInChildren<CustomCharacterController>();
        if (player != null && player != this)
        {
            // player.ragdollCenterRigidbody.transform.position = pickupPosition.transform.position;
            player.EnableRagdollForPickupRpc();
            // FixedJoint joint = player.ragdollCenterRigidbody.transform.AddComponent<FixedJoint>();
            // joint.connectedBody = pickupPosition.GetComponent<Rigidbody>();
        }
    }

    public void Interact(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (context.performed && closestInteractable != null)
        {
            InteractInfo info = new InteractInfo();
            info.character = this.networkObject;
            closestInteractable.Interact(info);
        }
    }

    public void Attack(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            switch (characterType)
            {
                case CharacterType.Pusher:
                {
                    Push();
                    break;
                }
                case CharacterType.Puller:
                {
                    Pull();
                    break;
                }
                default:
                {
                    break;
                }
            }
        }
    }

    protected void Push()
    {
        Debug.DrawRay(transform.position, transform.forward * rangeToPush, Color.red, 0.5f);
        RaycastHit[] raycastHits = Physics.RaycastAll(transform.position, transform.forward, rangeToPush);
        foreach (RaycastHit hit in raycastHits)
        {
            Rigidbody hitrg = hit.transform.GetComponent<Rigidbody>();
            if (hit.transform.gameObject != this.gameObject && hitrg != null)
            {
                hitrg.AddForce(this.transform.forward * pushForce, ForceMode.Impulse);
                return;
            }
        }
    }

    protected void Pull()
    {
        Debug.DrawRay(transform.position, transform.forward * rangeToPush, Color.red, 0.5f);
        RaycastHit[] raycastHits = Physics.RaycastAll(transform.position, transform.forward, rangeToPush);
        foreach (RaycastHit hit in raycastHits)
        {
            Rigidbody hitrg = hit.transform.GetComponent<Rigidbody>();
            if (hit.transform.gameObject != this.gameObject && hitrg != null)
            {
                hitrg.AddForce(-this.transform.forward * pushForce, ForceMode.Impulse);
                return;
            }
        }
    }

    public void SetMovement(InputAction.CallbackContext context)
    {
        Vector2 v = context.ReadValue<Vector2>();
        inputDirection = new Vector3(v.x, 0, v.y);
    }

    public void RotateCamera(InputAction.CallbackContext context)
    {
        cameraInput = context.ReadValue<float>();
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            Debug.Log("Jumping: " + characterVelocity);
            characterVelocity += transform.up.normalized * jumpForce;
            Debug.Log("Jumped: " + characterVelocity);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void ToggleRagdollRpc()
    {
        Debug.Log(this.gameObject + " ToggleRagdollRpc!");
        if (ragdollEnabled)
            DisableRagdoll(); else
            EnableRagdoll();
    }

    [Rpc(SendTo.Everyone)]
    public void EnableRagdollForPickupRpc()
    {
        this.transform.position = this.transform.position + this.transform.up * 100f;
        // characterController.enabled = false;
        // animator.enabled = false;
        // ragdollEnabled = true;
        // foreach (ClientTransform clientTransform in ragdoll.gameObject.GetComponentsInChildren<ClientTransform>())
        // {
        //     clientTransform.enabled = true;
        // }
        // foreach (Rigidbody subrigidbody in ragdoll.gameObject.GetComponentsInChildren<Rigidbody>())
        // {
        //     subrigidbody.isKinematic = false;
        // }
        // foreach (Collider subcollider in ragdoll.gameObject.GetComponentsInChildren<Collider>())
        // {
        //     subcollider.isTrigger = false;
        // }
        // ragdollCenterRigidbody.isKinematic = true;
    }

    public void EnableRagdoll()
    {
        characterController.enabled = false;
        animator.enabled = false;
        ragdollEnabled = true;
        foreach (ClientTransform clientTransform in ragdoll.gameObject.GetComponentsInChildren<ClientTransform>())
        {
            clientTransform.enabled = true;
        }
        foreach (Rigidbody subrigidbody in ragdoll.gameObject.GetComponentsInChildren<Rigidbody>())
        {
            subrigidbody.isKinematic = false;
        }
        foreach (Collider subcollider in ragdoll.gameObject.GetComponentsInChildren<Collider>())
        {
            subcollider.isTrigger = false;
        }
    }

    public void DisableRagdoll()
    {
        characterController.enabled = true;
        animator.enabled = true;
        ragdollEnabled = false;
        foreach (ClientTransform clientTransform in ragdoll.gameObject.GetComponentsInChildren<ClientTransform>())
        {
            clientTransform.enabled = false;
        }
        foreach (Rigidbody subrigidbody in ragdoll.gameObject.GetComponentsInChildren<Rigidbody>())
        {
            subrigidbody.isKinematic = true;
        }
        foreach (Collider subcollider in ragdoll.gameObject.GetComponentsInChildren<Collider>())
        {
            subcollider.isTrigger = true;
        }
    }

    void UpdateAnimator()
    {
        if (!isGrounded)
        {
            animator.SetBool("IsInAir", true);
            animator.SetLayerWeight(animator.GetLayerIndex("InAirLayer"), 1);
        } else
        {
            animator.SetBool("IsInAir", false);
            animator.SetLayerWeight(animator.GetLayerIndex("InAirLayer"), 0);
        }

        if (distanceMovedSinceLastFrame > 0.1)
        {
            animator.SetBool("IsRunning", true);
            animator.SetBool("IsWalking", false);
        } else
        if (distanceMovedSinceLastFrame <= 0.1 && distanceMovedSinceLastFrame > 0.02)
        {
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsWalking", true);
        } else
        {
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsWalking", false);
        }
    }

    void RotateCharacterTowards()
    {
        if (inputDirection == Vector3.zero) return;
        
        Vector3 camToPlayer = this.transform.position - virtualCamera.transform.position;
        camToPlayer.y = 0;
        Vector3 movementDirection = Quaternion.LookRotation(camToPlayer.normalized) * inputDirection;

        var q = Quaternion.LookRotation(movementDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, q, 1000f * Time.deltaTime);
    }
}
