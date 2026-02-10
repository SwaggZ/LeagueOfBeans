using UnityEngine;

/// <summary>
/// Simple ally dummy for testing ally-targeting abilities like Lux's shield wand.
/// Has HealthSystem and the "Ally" tag. Includes gravity like enemy dummies.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class AllyDummy : MonoBehaviour
{
    public CharacterController controller;
    
    [Header("Display")]
    public string allyName = "Ally";
    public Color allyColor = new Color(0.3f, 0.8f, 0.3f); // Green

    [Header("Physics")]
    public float gravity = -9.81f;
    public bool gravityEnabled = true;
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private HealthSystem healthSystem;
    private Vector3 velocity = Vector3.zero;
    private bool isGrounded;

    void Awake()
    {
        // Ensure tag is set
        gameObject.tag = "Ally";
    }

    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
            healthSystem = gameObject.AddComponent<HealthSystem>();

        // Add ModifierTracker for shield/modifier display
        if (GetComponent<ModifierTracker>() == null)
            gameObject.AddComponent<ModifierTracker>();

        // Add HealthBarUI so health bar and modifiers are always visible
        var healthBarUI = GetComponent<HealthBarUI>();
        if (healthBarUI == null)
            healthBarUI = gameObject.AddComponent<HealthBarUI>();
        
        // Always show health bar (don't hide when full)
        healthBarUI.hideWhenFull = false;
        healthBarUI.fillColor = allyColor; // Use ally color for health bar

        // Add trigger collider for physics detection (CharacterController doesn't work with OverlapSphere)
        if (GetComponent<CapsuleCollider>() == null)
        {
            var capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            capsule.height = controller != null ? controller.height : 2f;
            capsule.radius = controller != null ? controller.radius : 0.5f;
            capsule.center = controller != null ? controller.center : Vector3.up;
        }

        // Create ground check if not assigned
        if (groundCheck == null)
        {
            var gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform);
            gc.transform.localPosition = Vector3.zero;
            groundCheck = gc.transform;
        }

        Debug.Log($"AllyDummy '{allyName}' ready.");
    }

    void Update()
    {
        ApplyGravity();
    }

    void ApplyGravity()
    {
        if (!gravityEnabled || controller == null) return;

        // Ground check
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to stay grounded
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Get current health (for UI display)
    /// </summary>
    public float GetHealth()
    {
        return healthSystem != null ? healthSystem.GetCurrentHealth() : 0f;
    }

    /// <summary>
    /// Get max health
    /// </summary>
    public float GetMaxHealth()
    {
        return healthSystem != null ? healthSystem.maxHealth : 100f;
    }
}
