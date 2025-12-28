using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float smoothRotationSpeed = 500f;
    [SerializeField] private float jumpForce = 7f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.3f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;

    private HealthSystem playerHealth;
    private ManaSystem playerMana;

    [Header("State Flags")]
    private bool isMoving;
    private bool isRunning;
    private float currentMoveSpeed;

    private bool isJumping;
    private bool isGrounded;

    private bool isAttacking;
    private bool isMagicAttacking;

    [Header("Combat Settings")]
    [SerializeField] private float magicManaCost = 30f;

    private Vector3 cameraForward;
    private Vector3 cameraRight;

    private KeyCode[] moveKeyCodes = new KeyCode[]
    {
        KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D
    };

    private string animatorMoveBool = "IsMoving";
    private string animatorRunBool = "IsRunning";
    private string animatorJumpBool = "IsJumping";
    private string animatorPhysicalAttackTrigger = "PhysicalAttack";
    private string animatorMagicalAttackTrigger = "MagicalAttack";
    private string animatorDeathTrigger = "Death";

    void Awake()
    {
        if (cameraTransform == null) cameraTransform = Camera.main?.transform;
        if (animator == null) animator = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<HealthSystem>();
        playerMana = GetComponent<ManaSystem>();

        if (cameraTransform == null)
        {
            Debug.LogError("Main Camera (or CameraTransform) is not assigned for CharacterMovement on " + gameObject.name + "! Script disabled.", this);
            enabled = false;
            return;
        }

        if (animator == null)
        {
            Debug.LogWarning("Animator component not found on " + gameObject.name + ". Animations will not work.", this);
        }

        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found on " + gameObject.name + ". Movement and physics will not work! Script disabled.", this);
            enabled = false;
            return;
        }

        if (playerHealth == null)
        {
            Debug.LogWarning("HealthSystem component not found on " + gameObject.name + ". Player health and death state will not be managed by this script.", this);
        }

        if (playerMana == null)
        {
            Debug.LogWarning("ManaSystem component not found on " + gameObject.name + ". Magic attacks mana cost/reduction will not work.", this);
        }

        isMoving = false;
        isRunning = false;
        isJumping = false;
        isAttacking = false;
        isMagicAttacking = false;

        CheckForGround();
        UpdateCameraAxis();
    }

    void FixedUpdate()
    {
        CheckForGround();
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.IsDead())
        {
            if (rb != null) rb.constraints = RigidbodyConstraints.FreezeAll;
            OnPlayerDeath();
            return;
        }

        UpdateCameraAxis();

        if (rb == null) return;

        bool hasAnimator = (animator != null);

        Vector3 moveDirection = Vector3.zero;

        bool wasPressingMoveKeys = IsPressingMoveKeys(moveKeyCodes);
        isMoving = wasPressingMoveKeys;

        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift);
        isRunning = isShiftPressed && isMoving && CanMove();
        currentMoveSpeed = isRunning ? runSpeed : moveSpeed;

        if (hasAnimator)
        {
            animator.SetBool(animatorMoveBool, isMoving);
            animator.SetBool(animatorRunBool, isRunning);
        }

        if (isMoving && CanMove())
        {
            if (Input.GetKey(KeyCode.W)) moveDirection += cameraForward;
            if (Input.GetKey(KeyCode.S)) moveDirection -= cameraForward;
            if (Input.GetKey(KeyCode.A)) moveDirection -= cameraRight;
            if (Input.GetKey(KeyCode.D)) moveDirection += cameraRight;

            if (moveDirection.magnitude > 1)
            {
                moveDirection.Normalize();
            }

            if (moveDirection.magnitude > 0)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, smoothRotationSpeed * Time.deltaTime);
            }

            Vector3 movementVelocity = moveDirection * currentMoveSpeed;
            rb.velocity = new Vector3(movementVelocity.x, rb.velocity.y, movementVelocity.z);

        }
        else
        {
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);

            if (hasAnimator)
            {
                animator.SetBool(animatorMoveBool, false);
                animator.SetBool(animatorRunBool, false);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && CanJump())
        {
            isJumping = true;
            if (hasAnimator) animator.SetBool(animatorJumpBool, true);
        }

        if (Input.GetMouseButtonDown(0) && CanAttack())
        {
            RotateTowardsCamera(true);
            isAttacking = true;
            if (hasAnimator) animator.SetTrigger(animatorPhysicalAttackTrigger);
        }

        if (Input.GetMouseButtonDown(1) && CanAttack() && playerMana != null && playerMana.GetMana() >= magicManaCost)
        {
            RotateTowardsCamera(true);
            isMagicAttacking = true;
            playerMana.ReduceMana(magicManaCost);
            if (hasAnimator) animator.SetTrigger(animatorMagicalAttackTrigger);
        }
    }

    private void RotateTowardsCamera(bool forceInstantRotation = false)
    {
        if (cameraTransform == null)
        {
            Debug.LogWarning("CameraTransform is null! Cannot rotate towards camera.", this);
            return;
        }

        UpdateCameraAxis();

        Quaternion targetRotation = Quaternion.LookRotation(cameraForward);

        if (forceInstantRotation)
            transform.rotation = targetRotation;
        else
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, smoothRotationSpeed * Time.deltaTime);
    }

    private void UpdateCameraAxis()
    {
        if (cameraTransform == null) return;

        cameraForward = cameraTransform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        cameraRight = cameraTransform.right;
        cameraRight.y = 0;
        cameraRight.Normalize();
    }

    private bool CanMove()
    {
        return !isJumping && !isAttacking && !isMagicAttacking;
    }

    private bool IsPressingMoveKeys(KeyCode[] keyCodes)
    {
        foreach (KeyCode key in keyCodes)
        {
            if (Input.GetKey(key))
            {
                return true;
            }
        }
        return false;
    }

    private void CheckForGround()
    {
        if (rb == null)
        {
            isGrounded = false;
            return;
        }

        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        float rayLength = groundCheckDistance + 0.1f;

        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength);
    }

    private bool CanJump()
    {
        return isGrounded && !isJumping && !isAttacking && !isMagicAttacking;
    }

    public void OnJumpAnimationAddForce()
    {
        if (rb != null)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        else
        {
            Debug.LogWarning("Rigidbody is null! Cannot apply jump force from Animation Event.");
        }
    }

    public void OnJumpAnimationEnd()
    {
        isJumping = false;
        if (animator != null && !string.IsNullOrEmpty(animatorJumpBool))
        {
            animator.SetBool(animatorJumpBool, false);
        }
    }

    public bool GetJumpState()
    {
        return isJumping && (rb != null);
    }

    private bool CanAttack()
    {
        return !isAttacking && !isMagicAttacking && isGrounded;
    }

    public void OnPlayerAttackEnd()
    {
        isAttacking = false;
    }

    public void OnPlayerMagicAttackEnd()
    {
        isMagicAttacking = false;
    }

    private void OnPlayerDeath()
    {
        if (!enabled) return;

        Debug.Log(gameObject.name + " has died.");

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (animator != null && !string.IsNullOrEmpty(animatorDeathTrigger))
        {
            animator.SetTrigger(animatorDeathTrigger);
        }

        enabled = false;
    }

    public void TriggerDeath()
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(playerHealth.GetHealth());
        }
        else
        {
            OnPlayerDeath();
        }
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    public bool IsAttacking()
    {
        return isAttacking || isMagicAttacking;
    }
}