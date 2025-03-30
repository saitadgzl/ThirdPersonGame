using UnityEngine;
using TMPro;
using CharacterController;
using System.Diagnostics;

public class MoneyDisplay : MonoBehaviour
{
    [Tooltip("Reference to the ThirdPersonController component to get money value from")]
    public ThirdPersonController playerController;

    [Tooltip("Format string for the money display (e.g. '{0} $' will show '100 $')")]
    public string formatString = "{0} $";

    [Tooltip("Update frequency in seconds (0 for every frame)")]
    public float updateFrequency = 0;

    private TextMeshProUGUI tmpText;
    private float timeUntilNextUpdate = 0;
    private int lastMoneyValue = -1;

    private void Start()
    {
        // Get the TextMeshPro component
        tmpText = GetComponent<TextMeshProUGUI>();

        if (tmpText == null)
        {
            UnityEngine.Debug.LogError("MoneyDisplay script requires a TextMeshProUGUI component!");
            enabled = false;
            return;
        }

        // Try to find the player controller if not assigned
        if (playerController == null)
        {
            playerController = FindObjectOfType<ThirdPersonController>();

            if (playerController == null)
            {
                UnityEngine.Debug.LogWarning("MoneyDisplay could not find ThirdPersonController. Please assign it manually.");
                tmpText.text = string.Format(formatString, 0);
                enabled = false;
                return;
            }
        }
        UpdateMoneyDisplay();
    }

    private void Update()
    {
        // Check if we need to update based on frequency
        if (updateFrequency > 0)
        {
            timeUntilNextUpdate -= Time.deltaTime;
            if (timeUntilNextUpdate <= 0)
            {
                UpdateMoneyDisplay();
                timeUntilNextUpdate = updateFrequency;
            }
        }
        else
        {
            // Update every frame
            UpdateMoneyDisplay();
        }
    }

    private void UpdateMoneyDisplay()
    {
        if (playerController != null && tmpText != null)
        {
            // Only update the text if the money value has changed
            if (lastMoneyValue != playerController.money)
            {
                lastMoneyValue = playerController.money;
                tmpText.text = string.Format(formatString, lastMoneyValue);
            }
        }
    }

    public void ForceUpdate()
    {
        lastMoneyValue = -1; // Reset to force update
        UpdateMoneyDisplay();
    }
}