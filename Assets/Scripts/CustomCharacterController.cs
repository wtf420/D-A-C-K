using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
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
    public new Collider collider => characterController;

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
    [SerializeField] protected CharacterController characterController;
    protected new Rigidbody rigidbody;
    protected Animator animator;
    protected Outline outline;

    [Header("~*// OTHERS")]
    [SerializeField] public Rigidbody ragdollCenterRigidbody;
    [SerializeField] public GameObject pickupPosition;


    [Header("~*// WEAPON")]
    [SerializeField] public NetworkObject Gernade;
    [SerializeField] private float throwForce;

    [Header("~*// VARIABLES" )]
    private Transform holder;
    protected ButtonPrompt btnPrompt;
    protected Interactable closestInteractable;
    protected Vector3 inputDirection;
    protected Vector3 characterVelocity;
    protected Vector3 lastFramePosition; // because distanceMovedSinceLastFrame is unreliable as heck
    protected float cameraInput;
    protected float distanceMovedSinceLastFrame;
    protected bool isGrounded;
    protected bool canMove;
    public NetworkVariable<bool> ragdollEnabled;

    // Start is called before the first frame update
    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        inputDirection = Vector3.zero;
        characterVelocity = Vector3.zero;
        lastFramePosition = this.transform.position;
        outline = GetComponentInChildren<Outline>();
        canMove = true;
        ragdollEnabled = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        thisInteractable.InteractEvent.AddListener(SetPickUp);
        thisInteractable.isInteractable = false;
    }

    void Start()
    {
        virtualCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CinemachineVirtualCamera>();
        if (!IsOwner) return;
        virtualCamera.Follow = ragdollCenterRigidbody.transform;
        virtualCamera.LookAt = ragdollCenterRigidbody.transform;
        DisableRagdoll();
    }

    public override void OnNetworkSpawn()
    {
        if (ragdollEnabled.Value)
            EnableRagdoll(); else DisableRagdoll();
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
        
        if (!ragdollEnabled.Value)
        {
            RotateCharacterTowards();
            MoveCharacter();
        }
        UpdateCamera();
        UpdateAnimator();
        CheckForInteractables();
    }

    void LateUpdate()
    {
        if (holder == null) return;
        ragdollCenterRigidbody.transform.position = holder.transform.position;
        ragdollCenterRigidbody.transform.rotation = holder.transform.rotation;
    }

    void CheckForInteractables()
    {
        closestInteractable = null;
        float minDistance = Mathf.Infinity;
        foreach (Collider c in Physics.OverlapSphere(this.transform.position, rangeToInteract))
        {
            Interactable interactable = c.gameObject.GetComponent<Interactable>();

            if (interactable && interactable.isInteractable && interactable != thisInteractable )
            {
                float distance = Vector3.Distance(this.transform.position, interactable.transform.position);
                if (distance < minDistance)
                {
                    closestInteractable = interactable;
                    minDistance = Vector3.Distance(this.transform.position, interactable.transform.position);
                }
            }
        }
        if (btnPrompt != null) Destroy(btnPrompt.gameObject);
        if (closestInteractable != null)
        {
            btnPrompt = ButtonPrompt.Create();
            btnPrompt.transform.SetParent(ScreenCanvas.transform);
            btnPrompt.SetText(playerInput.currentActionMap.FindAction("Interact").GetBindingDisplayString(0));
            btnPrompt.SetPosition(Camera.main.WorldToScreenPoint(closestInteractable.gameObject.transform.position));
        }
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
        if (!canMove) return;
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
            player.EnableRagdoll();
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
            SpawnGernadeRpc();
            // switch (characterType)
            // {
            //     case CharacterType.Pusher:
            //     {
            //         Push();
            //         break;
            //     }
            //     case CharacterType.Puller:
            //     {
            //         Pull();
            //         break;
            //     }
            //     default:
            //     {
            //         break;
            //     }
            // }
        }
    }

    [Rpc(SendTo.Server)]
    public void SpawnGernadeRpc()
    {
        NetworkObject gernade = Instantiate(Gernade, pickupPosition.transform.position, this.transform.rotation);
        gernade.Spawn();
        gernade.GetComponent<Rigidbody>().AddForce(this.transform.forward * throwForce, ForceMode.Impulse);
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
            characterVelocity += transform.up.normalized * jumpForce;
        }
    }

    [Rpc(SendTo.Everyone)]
    public void SetRagdollStateRpc(bool state)
    {
        Debug.Log(this.gameObject + " SetRagdollStateRpc to " + state);
        if (!state)
            DisableRagdoll();
        else
            EnableRagdoll();
    }

    [Rpc(SendTo.Everyone)]
    public void ToggleRagdollRpc()
    {
        Debug.Log(this.gameObject + " ToggleRagdollRpc!");
        if (ragdollEnabled.Value)
            DisableRagdoll(); else
            EnableRagdoll();
    }

    protected virtual void SetPickUp(InteractInfo info)
    {
        SetPickUpRpc(info.character);
    }

    [Rpc(SendTo.Everyone)]
    protected virtual void SetPickUpRpc(NetworkObjectReference transform)
    {
        if (transform.TryGet(out NetworkObject networkObject) && networkObject.gameObject.GetComponentInChildren<CustomCharacterController>())
        {
            CustomCharacterController holderCharacter = networkObject.gameObject.GetComponentInChildren<CustomCharacterController>();
            //if is not already being hold by holderCharacter
            if (holderCharacter.pickupPosition.transform != holder)
            {
                //get picked up
                foreach (Collider subcollider in ragdoll.gameObject.GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(subcollider, holderCharacter.collider, true);
                }
                ragdollCenterRigidbody.isKinematic = true;
                holder = networkObject.gameObject.GetComponentInChildren<CustomCharacterController>().pickupPosition.transform;
                Debug.Log("Set Hold:" + holder);
            }
            else
            {
                //get let go and throw yourself forward
                holder = null;
                foreach (Collider subcollider in ragdoll.gameObject.GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(subcollider, holderCharacter.collider, false);
                }
                //DisableRagdoll();
                ragdollCenterRigidbody.isKinematic = false;
                ragdollCenterRigidbody.AddForce(this.transform.forward * throwForce, ForceMode.Impulse);
                Debug.Log("Let go");
            }
        }
        else
        {
            holder = null;
            rigidbody.isKinematic = false;
            Debug.Log("Is null");
        }
    }

    public void EnableRagdoll()
    {
        if (IsOwner) ragdollEnabled.Value = true;
        thisInteractable.isInteractable = true;
        characterController.detectCollisions = false;
        canMove = false;
        animator.enabled = false;
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
            subcollider.enabled = true;
        }
    }

    public void DisableRagdoll()
    {
        if (IsOwner) ragdollEnabled.Value = false;
        Vector3 p = ragdollCenterRigidbody.transform.position;
        thisInteractable.isInteractable = false;
        characterController.detectCollisions = true;
        canMove = true;
        animator.enabled = true;
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
            subcollider.enabled = false;
        }
        //you cant set the character position without disabling the characterController component
        characterController.enabled = false;
        this.gameObject.transform.position = p;
        ragdollCenterRigidbody.transform.localPosition = Vector3.zero;
        characterController.enabled = true;
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
