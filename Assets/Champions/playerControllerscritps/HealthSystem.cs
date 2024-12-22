using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public float maxHealth = 100f; // Maximum health of the object
    private float currentHealth; // Current health of the object
    private Renderer objectRenderer; // Renderer for changing color based on health

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
        currentHealth -= damage; // Reduce health by the damage amount

        Debug.Log($"{gameObject.name} took {damage} damage. Current health: {currentHealth}");

        UpdateColor(); // Update color based on health

        // Check if the object's health is depleted
        if (currentHealth <= 0)
        {
            Die(); // Trigger the death behavior
        }
    }

    // Method to heal the object
    public void Heal(float amount)
    {
        currentHealth += amount; // Increase health by the healing amount
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Ensure health does not exceed max health

        Debug.Log($"{gameObject.name} healed {amount} health. Current health: {currentHealth}");

        UpdateColor(); // Update color based on health
    }

    // Method to handle object death
    private void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        Destroy(gameObject); // Destroy the object
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
