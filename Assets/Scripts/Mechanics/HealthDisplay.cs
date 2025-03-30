using UnityEngine;
using UnityEngine.UI;
using CharacterController;
using System.Diagnostics;

public class HealthDisplay : MonoBehaviour
{
    [Tooltip("Reference to the ThirdPersonController component to get health value from")]
    public ThirdPersonController playerController;

    [Tooltip("Maximum health value for the slider (defaults to 100)")]
    public int maxHealth = 100;

    [Tooltip("Minimum health value for the slider (defaults to 0)")]
    public int minHealth = 0;

    [Tooltip("Update frequency in seconds (0 for every frame)")]
    public float updateFrequency = 0;

    [Tooltip("Should slider animate smoothly between values?")]
    public bool smoothTransition = true;

    [Tooltip("Speed of smooth transition if enabled")]
    public float transitionSpeed = 5f;

    private Slider healthSlider;
    private float timeUntilNextUpdate = 0;
    private int lastHealthValue = -1;
    private float targetSliderValue = 0f;

    private void Start()
    {
        // Get the Slider component
        healthSlider = GetComponent<Slider>();

        if (healthSlider == null)
        {
            UnityEngine.Debug.LogError("HealthDisplay script requires a Slider component!");
            enabled = false;
            return;
        }

        healthSlider.minValue = minHealth;
        healthSlider.maxValue = maxHealth;

        // Try to find the player controller if not assigned
        if (playerController == null)
        {
            playerController = FindObjectOfType<ThirdPersonController>();

            if (playerController == null)
            {
                UnityEngine.Debug.LogWarning("HealthDisplay could not find ThirdPersonController. Please assign it manually.");
                healthSlider.value = minHealth;
                enabled = false;
                return;
            }
        }
        UpdateHealthDisplay(true);
    }

    private void Update()
    {
        // Check if we need to update based on frequency
        if (updateFrequency > 0)
        {
            timeUntilNextUpdate -= Time.deltaTime;
            if (timeUntilNextUpdate <= 0)
            {
                UpdateHealthDisplay();
                timeUntilNextUpdate = updateFrequency;
            }
        }
        else
        {
            // Update every frame
            UpdateHealthDisplay();
        }

        // Handle smooth transition if enabled
        if (smoothTransition && healthSlider.value != targetSliderValue)
        {
            healthSlider.value = Mathf.Lerp(healthSlider.value, targetSliderValue, Time.deltaTime * transitionSpeed);
        }
    }

    private void UpdateHealthDisplay(bool forceInstant = false)
    {
        if (playerController != null && healthSlider != null)
        {
            // Only update if the health value has changed
            if (lastHealthValue != playerController.health)
            {
                lastHealthValue = playerController.health;

                // Clamp the health value to our min/max range
                float normalizedHealth = Mathf.Clamp(lastHealthValue, minHealth, maxHealth);

                // Set the target value
                targetSliderValue = normalizedHealth;

                if (!smoothTransition || forceInstant)
                {
                    healthSlider.value = normalizedHealth;
                }
            }
        }
    }

    public void ForceUpdate(bool instant = false)
    {
        lastHealthValue = -1; // Reset to force update
        UpdateHealthDisplay(instant);
    }
}