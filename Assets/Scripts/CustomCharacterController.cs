using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class CustomCharacterController : NetworkBehaviour
{
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected float jumpForce;
    [SerializeField] protected float gravityForce;
    [SerializeField] protected float airMovementMultiplier;
    [SerializeField] protected float cameraRotationSpeed;
    [SerializeField] protected float interactionRange;
    
    protected CharacterController characterController;
    protected new Rigidbody rigidbody;
    protected Animator animator;
    [SerializeField] protected CinemachineVirtualCamera virtualCamera;
    [SerializeField] protected Canvas ScreenCanvas;
    [SerializeField] protected PlayerInput playerInput;

    protected Interactable closestInteractable;
    [SerializeField] protected ButtonPrompt btn;
    protected float cameraInput;
    protected Vector3 inputDirection;
    protected Vector3 characterVelocity;
    protected Vector3 lastFramePosition; // because distanceMovedSinceLastFrame is unreliable as heck
    protected float distanceMovedSinceLastFrame;
    protected bool isGrounded;

    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        rigidbody = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        inputDirection = Vector3.zero;
        characterVelocity = Vector3.zero;
        lastFramePosition = this.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) 
        {
            virtualCamera.Priority = 0;
            return;
        }
        virtualCamera.Priority = 10;
        isGrounded = characterController.isGrounded;
        RotateCharacterTowards();
        MoveCharacter();
        UpdateCamera();
        UpdateAnimator();
        CheckForInteractables();
    }

    void CheckForInteractables()
    {
        foreach (Collider c in Physics.OverlapSphere(this.transform.position, interactionRange))
        {
            if (c.gameObject.GetComponent<Interactable>())
            {
                closestInteractable = c.gameObject.GetComponent<Interactable>();
                if (btn != null) Destroy(btn.gameObject);
                if (closestInteractable.isInteractable)
                {
                    btn = ButtonPrompt.Create();
                    btn.transform.SetParent(ScreenCanvas.transform);
                    btn.SetText(playerInput.currentActionMap.FindAction("Interact").GetBindingDisplayString(0));
                    btn.SetPosition(Camera.main.WorldToScreenPoint(closestInteractable.gameObject.transform.position));
                }
                return;
            }
        }
        if (btn != null) Destroy(btn.gameObject);
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
        float _moveSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            _moveSpeed *= 0.2f;
        }

        Vector3 camToPlayer = this.transform.position - virtualCamera.transform.position;
        camToPlayer.y = 0;
        Vector3 movementDirection = Quaternion.LookRotation(camToPlayer.normalized) * inputDirection;

        Vector3 gravity = -transform.up * gravityForce;

        Vector3 movement = movementDirection.normalized * _moveSpeed * Time.deltaTime;
        movement = isGrounded ? movement : movement * airMovementMultiplier;
        characterController.Move(movement);

        if (isGrounded && characterVelocity.y < 0)
        {
            characterVelocity.y = 0;
        }
        characterVelocity += gravity * Time.deltaTime;
        characterController.Move(characterVelocity);
    }

    public void Interact(InputAction.CallbackContext context)
    {
        if (context.performed && closestInteractable != null)
        {
            closestInteractable.Interact(this);
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
		if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
        {
            Vector3 inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            Vector3 camToPlayer = this.transform.position - virtualCamera.transform.position;
            camToPlayer.y = 0;
            Vector3 movementDirection = Quaternion.LookRotation(camToPlayer.normalized) * inputDirection;

            var q = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, q, 1000f * Time.deltaTime);
        }
    }
}
