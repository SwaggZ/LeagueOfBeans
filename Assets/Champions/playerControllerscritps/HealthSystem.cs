using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using FishNet.Object;
 

public class HealthSystem : MonoBehaviour
{
    public float maxHealth = 100f; // Maximum health of the object
    [SerializeField]
    private float currentHealth; // Current health of the object (serialized so inspector updates at runtime)
    private Renderer objectRenderer; // Renderer for changing color based on health
    [Header("UI / Popups")]
    [Tooltip("Vertical world-space offset for floating damage numbers above this object.")]
    public float damagePopupOffsetY = 1.25f;

    void Start()
    {
        // Initialize the object's health to the maximum value
        currentHealth = maxHealth;

        // Get the Renderer component
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogError($"{gameObject.name} does not have a Renderer component.");
        }
        else
        {
            UpdateColor(); // Set the initial color
        }
    }

    // Method to take damage
    public void TakeDamage(float damage)
    {
        // Apply incoming damage modifiers from attached components
        float multiplier = 1f;
        var behaviours = GetComponents<MonoBehaviour>();
        foreach (var b in behaviours)
        {
            if (b is IIncomingDamageModifier mod)
            {
                float m = Mathf.Clamp(mod.GetIncomingDamageMultiplier(), 0f, 10f);
                multiplier *= m;
            }
        }

        float appliedDamage = damage * multiplier;
        currentHealth -= appliedDamage; // Reduce health by the modified damage amount

        Debug.Log($"{gameObject.name} took {appliedDamage} damage (x{multiplier:0.00}). Current health: {currentHealth}");

        // Spawn floating damage text slightly above the object
        Vector3 popupPos = transform.position + Vector3.up * damagePopupOffsetY;
        DamagePopupSpawner.Spawn(popupPos, appliedDamage);

        UpdateColor(); // Update color based on health

        // Check if the object's health is depleted
        if (currentHealth <= 0)
        {
            Die(); // Trigger the death behavior
        }
    }

    // Overload to accept int damage (useful when callers send integer values via SendMessage)
    public void TakeDamage(int damage)
    {
        TakeDamage((float)damage);
    }

    // Method to heal the object
    public void Heal(float amount)
    {
        currentHealth += amount; // Increase health by the healing amount
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Ensure health does not exceed max health

        Debug.Log($"{gameObject.name} healed {amount} health. Current health: {currentHealth}");

        // Optional: show a green heal popup (comment out if not desired)
        // DamagePopupSpawner.Spawn(transform.position + Vector3.up * damagePopupOffsetY, $"+{Mathf.RoundToInt(amount)}", new Color(0.4f, 1f, 0.4f), false);

        UpdateColor(); // Update color based on health
    }

    // Method to handle object death
    private void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        
        // Check if this is a networked object
        var networkObject = GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            // Networked player death
            if (networkObject.IsOwner)
            {
                Debug.Log("[HealthSystem] Local player died. Showing character selection for respawn.");
                ShowCharacterSelectionForRespawn();
            }
            
            // Despawn on server
            if (networkObject.IsServerInitialized)
            {
                Debug.Log($"[HealthSystem] Despawning {gameObject.name} on server.");
                networkObject.Despawn();
            }
        }
        else
        {
            // Non-networked death (legacy single-player)
            if (CompareTag("Player"))
            {
                TryReturnToSelection();
            }
            Destroy(gameObject);
        }
    }

    private void ShowCharacterSelectionForRespawn()
    {
        // Find CharacterSelection in the scene (it's DontDestroyOnLoad, so it should still exist)
        var characterSelection = FindObjectOfType<CharacterSelection>();
        if (characterSelection != null)
        {
            Debug.Log("[HealthSystem] Found CharacterSelection, showing UI for respawn.");
            characterSelection.SetRespawnMode(true);
            characterSelection.ShowSelectionUI();
            
            // Hide both HUDs during selection
            var cooldownUI = FindObjectOfType<CooldownUIManager>();
            if (cooldownUI != null)
            {
                cooldownUI.HideHUD();
            }
            
            var modifiersUI = FindObjectOfType<ModifiersUIManager>();
            if (modifiersUI != null)
            {
                modifiersUI.HideHUD();
            }
        }
        else
        {
            Debug.LogWarning("[HealthSystem] CharacterSelection not found. Cannot show selection UI for respawn.");
        }
    }

    private void TryReturnToSelection()
    {
        // Load the character selection scene by name. Adjust if your selection scene has a different name.
        const string selectionSceneName = "selection";
        try
        {
            SceneManager.LoadScene(selectionSceneName, LoadSceneMode.Single);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"HealthSystem: Failed to load '{selectionSceneName}' scene on death: {ex.Message}");
        }
    }

    // Getter for current health (optional)
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    // Method to update the object's color based on its current health
    private void UpdateColor()
    {
        if (objectRenderer != null)
        {
            float healthPercentage = currentHealth / maxHealth;
            Color newColor = Color.Lerp(Color.red, Color.white, healthPercentage); // Interpolate between red and white
            objectRenderer.material.color = newColor;
        }
    }
}
