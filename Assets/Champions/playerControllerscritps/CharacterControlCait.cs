using UnityEngine;
using System.Collections;

public class CharacterControlCait : MonoBehaviour
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

    public bool canDash = true; // Added boolean for checking if dashing is allowed
    bool isDashing;
    public float dashTime = 0.1f; // Adjust the dash time as needed
    public float dashSpeed = -60f; // Adjust the dash speed as needed
    float dashCooldown = 3f; // Adjust the dash cooldown as needed
    float dashTimer;

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if(isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        
        Vector3 move = transform.right * x + transform.forward * z;

        if(Input.GetButton("Run"))
        {
            controller.Move(move * runSpeed * Time.deltaTime);    
        }
        else
        {
            controller.Move(move * speed * Time.deltaTime);
        }

        if(Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) && canDash && Time.time > dashTimer)
        {
            StartCoroutine(Dash2());
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }

    IEnumerator Dash2()
    {
        isDashing = true;

        // Remember the original speed
        float originalSpeed = speed;

        // Get the camera's forward vector
        Vector3 cameraForward = playerCam.forward;

        // Apply the dash movement for a short duration
        for (float t = 0; t < dashTime; t += Time.deltaTime)
        {
            controller.Move(cameraForward * dashSpeed * Time.deltaTime);

            yield return null;
        }

        // Reset speed and set cooldown
        speed = originalSpeed;
        dashTimer = Time.time + dashCooldown;

        isDashing = false;
    }
}