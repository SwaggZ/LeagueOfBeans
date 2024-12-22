using UnityEngine;
using System.Collections;

public class CharacterControl : MonoBehaviour
{
    public CharacterController controller;

    public float speed = 12f;
    public float runSpeed = 18f;

    Vector3 velocity;

    public float gravity = -9.81f;

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
    float dashCooldown = 3f;
    float dashTimer;

    private bool isKnockingBack = false;
    private Vector3 knockbackDirection;
    private float knockbackDistance;
    private float knockbackSpeed;
    private float stunEndTime = 0f;
    private bool isStunned = false;

    void Update()
    {
        if (isKnockingBack)
        {
            HandleKnockback();
            return;
        }

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

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

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

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Dash functionality
        if (Input.GetButtonDown("Dash") && canDash && Time.time > dashTimer)
        {
            StartCoroutine(Dash());
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    IEnumerator Dash()
    {
        isDashing = true;

        Vector3 cameraForward = playerCam.forward;

        for (float t = 0; t < dashTime; t += Time.deltaTime)
        {
            controller.Move(cameraForward * dashSpeed * Time.deltaTime);
            yield return null;
        }

        dashTimer = Time.time + dashCooldown;

        isDashing = false;
    }

    public void ApplyKnockback(Vector3 direction, float distance, float speed, float stunDuration)
    {
        isKnockingBack = true;
        knockbackDirection = direction.normalized;
        knockbackDistance = distance;
        knockbackSpeed = speed;

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
        Vector3 move = knockbackDirection * step;

        if (knockbackDistance <= 0 || move.magnitude > knockbackDistance)
        {
            isKnockingBack = false;
            return;
        }

        controller.Move(move);
        knockbackDistance -= move.magnitude;
    }
}
