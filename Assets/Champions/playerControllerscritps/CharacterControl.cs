using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CharacterControl : MonoBehaviour
{
    public CharacterController controller;

    public float speed = 12f;
    public float runSpeed = 18f;

    Vector3 velocity;

    public float gravity = -9.81f;

    // Allow temporary disabling of gravity (used by abilities like Galio R)
    public bool gravityEnabled = true;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    bool isGrounded;

    public float jumpHeight = 3f;

    public Transform playerCam;

    public bool canDash = true;
    bool isDashing;
    public float dashTime = 0.5f;
    public float dashSpeed = 30f;
    // Dash options
    public bool dashKnockup = false; // if true, dash will apply an upward knockup
    public float dashKnockupForce = 5f; // upward force applied by dash knockup
    public bool dashDamage = false; // if true, dash will deal damage
    public int dashDamageAmount = 0; // amount of damage dealt by dash
    public LayerMask dashHitMask = ~0; // layers that can be hit by dash
    public float dashHitRadius = 1f; // radius used to detect targets during dash
    public string dashTargetTag = "Enemy"; // only targets with this tag are considered
    float dashCooldown = 3f;
    float dashTimer;
    private HashSet<Collider> _dashHitTargets = new HashSet<Collider>();

    private bool isKnockingBack = false;
    private Vector3 knockbackDirection;
    private float knockbackDistance;
    private float knockbackSpeed;
    private float stunEndTime = 0f;
    private bool isStunned = false;

    void Update()
    {
        // Stun handling
        if (isStunned)
        {
            if (Time.time >= stunEndTime)
            {
                isStunned = false;
                Debug.Log($"{gameObject.name} is no longer stunned.");
            }
            else
            {
                return;
            }
        }

        // Ground check
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }

        // Do not clamp vertical velocity here; we'll clamp after movement based on actual grounded state

        // If knocking back, handle horizontal knockback and keep vertical velocity as-is.
        // Downward acceleration (gravity) will start only after knockback ends.
        if (isKnockingBack)
        {
            HandleKnockback(); // moves horizontally and reduces knockbackDistance

            // While in knockback, apply gravity so the vertical motion behaves like a jump arc
            if (isKnockingBack)
            {
                if (gravityEnabled)
                {
                    velocity.y += gravity * Time.deltaTime;
                }
                else
                {
                    velocity.y = 0f;
                }

                controller.Move(velocity * Time.deltaTime);
                // After moving, if now grounded, lightly push down to keep grounded next frame
                if (controller.isGrounded && velocity.y < 0f)
                {
                    velocity.y = -2f;
                }
                return; // skip normal input movement while knocked back
            }
            // If knockback just ended this frame, fall through to normal flow so gravity applies immediately
        }

        // Player movement input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        if (Input.GetButton("Run"))
        {
            controller.Move(move * runSpeed * Time.deltaTime);
        }
        else
        {
            controller.Move(move * speed * Time.deltaTime);
        }

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Dash functionality
        if (Input.GetButtonDown("Dash") && canDash && Time.time > dashTimer)
        {
            // Push cooldown to UI CTRL slot (CTRL is Dash)
            if (dashCooldown > 0f && CooldownUIManager.Instance != null)
            {
                CooldownUIManager.Instance.StartCooldown(AbilityKey.Ctrl, dashCooldown);
            }
            StartCoroutine(Dash());
        }

        // Apply gravity to vertical velocity (unless disabled by abilities)
        if (gravityEnabled)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else
        {
            velocity.y = 0f;
        }

        controller.Move(velocity * Time.deltaTime);
        // After vertical move, if grounded, lightly push down to keep grounded next frame
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
    }

    // Called by other scripts to enable/disable built-in gravity handling
    public void SetGravityEnabled(bool enabled)
    {
        gravityEnabled = enabled;
        if (!gravityEnabled)
        {
            velocity.y = 0f;
        }
    }

    IEnumerator Dash()
    {
        isDashing = true;

        Vector3 cameraForward = playerCam.forward;
        _dashHitTargets.Clear();

        for (float t = 0; t < dashTime; t += Time.deltaTime)
        {
            controller.Move(cameraForward * dashSpeed * Time.deltaTime);

            // detect hits in front of the player during dash
            Vector3 sphereCenter = transform.position + cameraForward * 1f;
            Collider[] hits = Physics.OverlapSphere(sphereCenter, dashHitRadius, dashHitMask);
            foreach (Collider hit in hits)
            {
                if (hit == null) continue;
                // ignore self
                if (hit.transform.IsChildOf(transform)) continue;
                // filter by tag
                if (!string.IsNullOrEmpty(dashTargetTag) && !hit.CompareTag(dashTargetTag)) continue;
                if (_dashHitTargets.Contains(hit)) continue;
                _dashHitTargets.Add(hit);
                HandleDashHit(hit);
            }
            yield return null;
        }

        dashTimer = Time.time + dashCooldown;

        isDashing = false;
    }

    public void ApplyKnockback(Vector3 direction, float distance, float speed, float stunDuration)
    {
        // Compute horizontal knockback direction from input
        Vector3 horiz = new Vector3(direction.x, 0f, direction.z);
        if (horiz.sqrMagnitude > 0.0001f && distance > 0f && speed > 0f)
        {
            isKnockingBack = true;
            knockbackDirection = horiz.normalized;
            knockbackDistance = distance;
            knockbackSpeed = speed;
        }
        else
        {
            // No horizontal component: treat as a pure vertical knockup (like a jump), no knockback state
            isKnockingBack = false;
        }

        // Apply vertical impulse (like a jump). Keep the larger upward velocity if already rising.
        if (direction.y > 0f)
        {
            velocity.y = Mathf.Max(velocity.y, direction.y * speed);
        }

        if (stunDuration > 0)
        {
            isStunned = true;
            stunEndTime = Time.time + stunDuration;
            Debug.Log($"{gameObject.name} is stunned for {stunDuration} seconds.");
        }
    }

    public void Stun(float duration)
    {
        isStunned = true;
        stunEndTime = Time.time + duration;
        Debug.Log($"{gameObject.name} is stunned for {duration} seconds.");
    }

    private void HandleKnockback()
    {
        float step = knockbackSpeed * Time.deltaTime;
        Vector3 move = new Vector3(knockbackDirection.x, 0f, knockbackDirection.z) * step;

        if (knockbackDistance <= 0 || move.magnitude > knockbackDistance)
        {
            isKnockingBack = false;
            // Do not modify vertical velocity; let gravity continue naturally
            return;
        }

        // horizontal movement component handled by knockback logic
        controller.Move(move);
        knockbackDistance -= move.magnitude;
    }

    private void HandleDashHit(Collider hit)
    {
        // Damage: try flexible SendMessage first
        if (dashDamage && dashDamageAmount != 0)
        {
            hit.SendMessage("TakeDamage", dashDamageAmount, SendMessageOptions.DontRequireReceiver);
        }

        // Knockup: prefer applying CharacterControl's ApplyKnockback if present
        if (dashKnockup)
        {
            CharacterControl other = hit.GetComponent<CharacterControl>();
            if (other != null)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;
                dir.y = 1f; // ensure upward component
                other.ApplyKnockback(dir, 2f, dashKnockupForce, 0f);
            }
            else
            {
                // fallback: if the hit has a rigidbody, apply an upward impulse
                Rigidbody rb = hit.attachedRigidbody;
                if (rb != null)
                {
                    rb.AddForce(Vector3.up * dashKnockupForce, ForceMode.VelocityChange);
                }
                else
                {
                    // try SendMessage for a generic knockup handler
                    hit.SendMessage("ApplyKnup", dashKnockupForce, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }
}
