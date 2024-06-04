using Cinemachine;
using System.Collections;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class CustomCharacterController : NetworkBehaviour
{
    [Header("~*// PARAMETERS")]
    [SerializeField] public Interactable thisInteractable;
    [SerializeField] public NetworkObject networkObject;
    public new Collider collider => characterController;

    [Header("~* Movements and Controls")]
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected float jumpForce;
    [SerializeField] protected float gravityForce;
    [SerializeField] protected float airMovementMultiplier;
    [SerializeField] protected float cameraRotationSpeed;
    [SerializeField] protected float maxFallingSpeed;
    [SerializeField] protected float maxFallingDistance = 10f; //how far player fall before its considered infinite falling
    [SerializeField] protected float maxFallingTime = 0.5f; //how long player fall before checking for infinite falling

    [Header("~* Others")]
    [SerializeField] protected float rangeToInteract;

    [Header("~*// OBJECTS & COMPONENTS")]
    [SerializeField] protected CinemachineVirtualCamera virtualCamera;
    [SerializeField] protected Canvas ScreenCanvas;
    [SerializeField] protected PlayerInput playerInput;
    [SerializeField] protected GameObject ragdoll;
    [SerializeField] protected CharacterController characterController;
    protected new Rigidbody rigidbody;
    protected Animator animator;
    protected Outline outline;

    [Header("~* NETWORKING")]
    public NetworkPlayer controlPlayer;
    public NetworkVariable<NetworkBehaviourReference> controlPlayerNetworkBehaviourReference = new NetworkVariable<NetworkBehaviourReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> ragdollEnabled = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString32Bytes> shirtColor = new NetworkVariable<FixedString32Bytes>("123456", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    protected NetworkVariable<bool> networkSpawned = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("~*// OTHERS")]
    [SerializeField] public ButtonPrompt buttonPrompt;
    [SerializeField] public Rigidbody ragdollCenterRigidbody;
    [SerializeField] public GameObject pickupPosition;
    [SerializeField] public GameObject weaponHoldTransform;
    [SerializeField] public new Renderer renderer;

    [Header("~*// WEAPON")]
    [SerializeField] public Weapon weapon;
    [SerializeField] public NetworkObject Gernade;
    [SerializeField] private float throwForce;

    [Header("~*// VARIABLES")]
    private Transform holder;
    protected ButtonPrompt btnPrompt;
    protected Interactable closestInteractable;
    protected Vector3 inputDirection;
    protected Vector3 characterVelocity;
    protected Vector3 lastFramePosition; // because distanceMovedSinceLastFrame is unreliable as heck
    protected float cameraInput;
    protected float distanceMovedSinceLastFrame;
    protected float currentFallingTimer;
    protected float disableRagdollTimer = 0.0f;
    protected bool isGrounded;
    protected bool canMove;

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
        currentFallingTimer = 0.0f;

        thisInteractable.InteractEvent.AddListener(SetPickUp);
        thisInteractable.isInteractable = false;

        controlPlayerNetworkBehaviourReference.OnValueChanged += UpdatePlayerFromReference;
    }

    //Late join data handle here
    void Start()
    {
        Debug.Log("Start");
        virtualCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CinemachineVirtualCamera>();
        if (ragdollEnabled.Value)
            EnableRagdoll();
        else DisableRagdoll();
        StartCoroutine(StartAfterServerLoad());

        if (!IsOwner)
        {
            virtualCamera.Priority = 0;

            outline.enabled = false;
            outline.enabled = true;
            outline.OutlineColor = Color.red;
            return;
        } else
        {
            //Initialize as owner

            //reenable outline so it will render properly
            outline.enabled = false;
            outline.enabled = true;
            outline.OutlineColor = Color.green;

            virtualCamera.Follow = ragdollCenterRigidbody.transform;
            virtualCamera.LookAt = ragdollCenterRigidbody.transform;
            virtualCamera.Priority = 10;
        }
    }

    //sync or create network data
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner) NetworkSpawnRpc();
    }

    [Rpc(SendTo.Server)]
    public void NetworkSpawnRpc()
    {
        StartCoroutine(NetworkSpawn());
    }

    IEnumerator NetworkSpawn()
    {
        yield return new WaitUntil(() => GameManager.Instance.NetworkSpawned.Value);
        shirtColor.Value = "#" + ColorUtility.ToHtmlStringRGBA(Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f));
        Debug.Log("OnNetworkSpawn: " + shirtColor.Value);
        networkSpawned.Value = true;
    }

    IEnumerator StartAfterServerLoad()
    {
        yield return new WaitUntil(() => networkSpawned.Value);
        Debug.Log("StartAfterServerLoad: " + shirtColor.Value);
        UpdatePlayerFromReference(controlPlayerNetworkBehaviourReference.Value, controlPlayerNetworkBehaviourReference.Value);
        Material shirtMaterial = renderer.materials.FirstOrDefault((x) => x.name == "ShirtColor (Instance)");
        if (shirtMaterial != null && UnityEngine.ColorUtility.TryParseHtmlString(shirtColor.Value.ToString(), out Color color))
        {
            shirtMaterial.color = color;
        }
        else
        {
            Debug.Log("Color parse failed");
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        controlPlayerNetworkBehaviourReference.OnValueChanged -= UpdatePlayerFromReference;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        CheckForPlatformBelow(out float a);
        isGrounded = ragdollEnabled.Value ? (holder != null || a < 1.0f) : characterController.isGrounded;
        if (isGrounded) currentFallingTimer = 0.0f;
        else
        {
            currentFallingTimer += Time.deltaTime;
            if (currentFallingTimer > maxFallingTime && !CheckForPlatformBelow(out a) && controlPlayer != null)
            {
                //controlPlayer.DespawnRpc();
            }
        }

        if (!ragdollEnabled.Value)
        {
            if (disableRagdollTimer != 0.0f) disableRagdollTimer = 0.0f;
            RotateCharacterTowards();
            MoveCharacter();
        }
        else
        {
            disableRagdollTimer += Time.deltaTime;
            if (disableRagdollTimer > 3.0f)
            {
                //free yourself & standup
            }
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

    void UpdatePlayerFromReference(NetworkBehaviourReference previous, NetworkBehaviourReference current)
    {
        if (current.TryGet(out NetworkPlayer player))
        {
            this.controlPlayer = player;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void OnWeaponPickUpRpc()
    {
        animator.SetLayerWeight(animator.GetLayerIndex("BaseballBat2"), 1);
    }

    bool CheckForPlatformBelow(out float distance)
    {
        bool result = false;
        RaycastHit[] hits = Physics.RaycastAll(ragdollCenterRigidbody.position, -transform.up, maxFallingDistance, ~LayerMask.GetMask("Player"));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && !hit.collider.isTrigger)
            {
                distance = hit.distance;
                result = true;
                break;
            }
            else continue;
        }
        distance = Mathf.Infinity;
        return result;
    }

    void CheckForInteractables()
    {
        closestInteractable = null;
        float minDistance = Mathf.Infinity;
        foreach (Collider c in Physics.OverlapSphere(this.transform.position, rangeToInteract))
        {
            Interactable interactable = c.gameObject.GetComponent<Interactable>();

            if (interactable && interactable.isInteractable && interactable != thisInteractable)
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
            btnPrompt = Instantiate(buttonPrompt, ScreenCanvas.transform, false);
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
        characterVelocity.y = Mathf.Clamp(characterVelocity.y, maxFallingSpeed, Mathf.Infinity);
        characterController.Move(characterVelocity);
    }

    public void PickUpPlayerRpc(GameObject playerGameObject)
    {
        CustomCharacterController player = playerGameObject.GetComponentInChildren<CustomCharacterController>();
        if (player != null && player != this)
        {
            player.EnableRagdoll();
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
        if (context.performed)
        {
            if (weapon)
            {
                weapon.AttemptAttack();
                animator.SetTrigger("Attack");
            }
            //SpawnGernadeRpc();
        }
    }

    [Rpc(SendTo.Server)]
    public void SpawnGernadeRpc()
    {
        NetworkObject gernade = Instantiate(Gernade, pickupPosition.transform.position, this.transform.rotation);
        gernade.Spawn();
        gernade.GetComponent<Rigidbody>().AddForce(this.transform.forward * throwForce, ForceMode.Impulse);
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
            animator.Play("Jump");
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
            DisableRagdoll();
        else
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
                Vector3 direction = holder.forward;
                holder = null;
                foreach (Collider subcollider in ragdoll.gameObject.GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(subcollider, holderCharacter.collider, false);
                }
                //DisableRagdoll();
                ragdollCenterRigidbody.isKinematic = false;
                ragdollCenterRigidbody.AddForce(direction * throwForce, ForceMode.Impulse);
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
