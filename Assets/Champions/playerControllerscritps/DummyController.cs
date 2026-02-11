using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simplified character controller for dummy/training targets.
/// Handles knockback, stun, slowness, and gravity.
/// No player input or movement.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class DummyController : MonoBehaviour
{
    public CharacterController controller;
    public Vector3 velocity = Vector3.zero;

    [Header("Physics")]
    public float gravity = -9.81f;
    public bool gravityEnabled = true;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private bool isGrounded;

    [Header("Status Effects")]
    private bool isStunned = false;
    private float stunEndTime = 0f;
    private float slowMultiplier = 1f; // 1.0 = normal speed, 0.5 = half speed
    private float slowEndTime = 0f; // When current slow expires
    private ModifierTracker modifierTracker;

    // Knockback state
    private bool isKnockingBack = false;
    private Vector3 knockbackDirection;
    private float knockbackDistance;
    private float knockbackSpeed;

    // Attract/Pull state
    private bool isBeingPulled = false;
    private Vector3 pullTargetPosition;
    private float pullSpeed = 10f;
    private float pullDuration = 2f;
    private float pullEndTime = 0f;

    void Start()
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        modifierTracker = GetComponent<ModifierTracker>();

        if (controller == null)
        {
            Debug.LogError("DummyController requires a CharacterController component!");
        }
    }

    void Update()
    {
        // Stun handling - cannot move while stunned
        if (isStunned)
        {
            if (Time.time >= stunEndTime)
            {
                isStunned = false;
                Debug.Log($"{gameObject.name} is no longer stunned.");
            }
            else
            {
                // Apply gravity even while stunned
                if (gravityEnabled)
                {
                    velocity.y += gravity * Time.deltaTime;
                }
                controller.Move(velocity * Time.deltaTime);
                if (controller.isGrounded && velocity.y < 0f)
                {
                    velocity.y = -2f;
                }
                return;
            }
        }

        // Check if slow has expired
        if (slowMultiplier < 1f && Time.time >= slowEndTime)
        {
            slowMultiplier = 1f;
            ModifierUtils.RemoveModifier(gameObject, "StatusSlow", removePlayerHud: false, removeEnemy: true);
        }

        // Ground check
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }

        // Handle attract/pull
        if (isBeingPulled && Time.time < pullEndTime)
        {
            HandleAttract();
        }
        else if (isBeingPulled)
        {
            isBeingPulled = false;
        }

        // Handle knockback
        if (isKnockingBack)
        {
            HandleKnockback();

            // Apply gravity during knockback
            if (gravityEnabled)
            {
                velocity.y += gravity * Time.deltaTime;
            }

            controller.Move(velocity * Time.deltaTime);
            if (controller.isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;
            }
            return;
        }

        // Apply gravity normally
        if (gravityEnabled)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else
        {
            velocity.y = 0f;
        }

        // Dummies don't move on their own, only from external forces (knockback)
        controller.Move(velocity * Time.deltaTime);
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
    }

    /// <summary>
    /// Apply burn status (ICON ONLY)
    /// </summary>
    public void ApplyBurn(float duration)
    {
        ModifierUtils.ApplyModifier(gameObject, "StatusBurn", null, "Burning", duration, 0, includePlayerHud: false, includeEnemy: true);
    }

    /// <summary>
    /// Apply knockback + optional stun to the dummy
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float distance, float speed, float stunDuration = 0f)
    {
        // Horizontal knockback
        Vector3 horiz = new Vector3(direction.x, 0f, direction.z);
        if (horiz.sqrMagnitude > 0.0001f && distance > 0f && speed > 0f)
        {
            isKnockingBack = true;
            knockbackDirection = horiz.normalized;
            knockbackDistance = distance;
            knockbackSpeed = speed;
        }

        // Vertical impulse
        if (direction.y > 0f)
        {
            velocity.y = Mathf.Max(velocity.y, direction.y * speed);
        }

        // Apply stun if specified
        if (stunDuration > 0)
        {
            Stun(stunDuration);
        }

        // Show knockup modifier
        if (direction.y > 0.1f && modifierTracker != null && FindObjectOfType<ModifiersIconLibrary>(true) != null)
        {
            float dur = stunDuration > 0 ? stunDuration : Mathf.Max(0.4f, distance > 0 && speed > 0 ? (distance / speed) : 0.6f);
            ModifierUtils.ApplyModifier(gameObject, "StatusKnockup", null, "Knocked Up", dur, 0, includePlayerHud: false, includeEnemy: true);
        }
    }

    /// <summary>
    /// Apply stun to the dummy
    /// </summary>
    public void Stun(float duration)
    {
        isStunned = true;
        stunEndTime = Time.time + duration;
        Debug.Log($"{gameObject.name} is stunned for {duration} seconds.");

        ModifierUtils.ApplyModifier(gameObject, "StatusStun", null, "Stunned", duration, 0, includePlayerHud: false, includeEnemy: true);
    }

    /// <summary>
    /// Apply slow effect
    /// </summary>
    public void ApplySlow(float multiplier, float duration)
    {
        slowMultiplier = multiplier;
        slowEndTime = Time.time + duration;
        Debug.Log($"{gameObject.name} slowed to {multiplier * 100}% for {duration} seconds.");

        ModifierUtils.ApplyModifier(gameObject, "StatusSlow", null, "Slowed", duration, 0, includePlayerHud: false, includeEnemy: true);
    }

    /// <summary>
    /// Clear any active slow effect immediately
    /// </summary>
    public void ClearSlow()
    {
        slowMultiplier = 1f;
        slowEndTime = 0f;
        ModifierUtils.RemoveModifier(gameObject, "StatusSlow", removePlayerHud: false, removeEnemy: true);
    }

    public void SetGravityEnabled(bool enabled)
    {
        gravityEnabled = enabled;
        if (!gravityEnabled)
        {
            velocity.y = 0f;
        }
    }

    private void HandleKnockback()
    {
        float step = knockbackSpeed * slowMultiplier * Time.deltaTime; // Apply slow multiplier
        Vector3 move = knockbackDirection * step;

        if (knockbackDistance <= 0 || move.magnitude > knockbackDistance)
        {
            isKnockingBack = false;
            return;
        }

        controller.Move(move);
        knockbackDistance -= move.magnitude;
    }

    private void HandleAttract()
    {
        Vector3 directionToTarget = (pullTargetPosition - transform.position).normalized;
        float step = pullSpeed * slowMultiplier * Time.deltaTime; // Apply slow multiplier to pull speed
        Vector3 move = directionToTarget * step;

        controller.Move(move);
    }

    /// <summary>
    /// Apply attract/pull effect toward a target position
    /// </summary>
    public void ApplyAttract(Vector3 targetPosition, float speed, float duration)
    {
        isBeingPulled = true;
        pullTargetPosition = targetPosition;
        pullSpeed = speed;
        pullDuration = duration;
        pullEndTime = Time.time + duration;

        Debug.Log($"{gameObject.name} is being pulled for {duration} seconds.");

        ModifierUtils.ApplyModifier(gameObject, "StatusAttract", null, "Attracted", duration, 0, includePlayerHud: false, includeEnemy: true);
    }

    /// <summary>
    /// Get current movement speed multiplier (for slow effects)
    /// </summary>
    public float GetSpeedMultiplier()
    {
        return isStunned ? 0f : slowMultiplier;
    }

    /// <summary>
    /// Check if dummy is currently stunned
    /// </summary>
    public bool IsStunned()
    {
        return isStunned;
    }
}
