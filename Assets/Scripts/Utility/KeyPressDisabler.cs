using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A script that disables a gameobject after a specific key is pressed N times.
/// Useful for boss shields, placeholders, or other state-based disabling logic.
/// </summary>
public class KeyPressDisabler : MonoBehaviour
{
    [Header("Key Configuration")]
    [Tooltip("The key to listen for")]
    public KeyCode activationKey = KeyCode.P;

    [Header("Press Count Settings")]
    [Tooltip("Number of key presses required to disable this object")]
    [Min(1)]
    public int pressesRequired = 5;

    [Tooltip("If true, resets the press count when the object is disabled")]
    public bool resetCountOnDisable = true;

    [Header("Visual Feedback")]
    [Tooltip("Display remaining presses in console")]
    public bool enableDebugLogging = true;

    [Tooltip("Optional: UI text to display press count (e.g., 'Shield: 3/5 hits')")]
    public TMPro.TextMeshProUGUI displayText = null;

    [Tooltip("Optional: Text format for display. Use {current} and {total}. Example: 'Shield: {current}/{total}'")]
    public string displayFormat = "Presses: {current}/{total}";

    [Header("Effects")]
    [Tooltip("Optional: Effect to instantiate when disabled")]
    public GameObject disableEffect = null;

    [Tooltip("Optional: Sound or particle effect to play on each key press")]
    public GameObject hitEffect = null;

    // Current press count
    private int currentPresses = 0;

    // Whether this object has been disabled by this script
    private bool hasBeenDisabled = false;

    /// <summary>
    /// Description:
    /// Standard Unity function called once per frame
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void Update()
    {
        // Don't listen for input if already disabled
        if (hasBeenDisabled)
        {
            return;
        }

        // Check for key press
        if (Input.GetKeyDown(activationKey))
        {
            RegisterKeyPress();
        }
    }

    /// <summary>
    /// Description:
    /// Called when the activation key is pressed
    /// Increments counter and checks if threshold is met
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void RegisterKeyPress()
    {
        currentPresses++;

        // Play hit effect if assigned
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, transform.rotation, null);
        }

        if (enableDebugLogging)
        {
            Debug.Log($"[KeyPressDisabler] Key pressed: {currentPresses}/{pressesRequired}");
        }

        // Update display text if assigned
        UpdateDisplayText();

        // Check if threshold is reached
        if (currentPresses >= pressesRequired)
        {
            DisableObject();
        }
    }

    /// <summary>
    /// Description:
    /// Updates the display text with current press count
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void UpdateDisplayText()
    {
        if (displayText != null)
        {
            string text = displayFormat
                .Replace("{current}", currentPresses.ToString())
                .Replace("{total}", pressesRequired.ToString());
            displayText.text = text;
        }
    }

    /// <summary>
    /// Description:
    /// Disables the gameobject and creates optional effects
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void DisableObject()
    {
        hasBeenDisabled = true;

        // Create disable effect if assigned
        if (disableEffect != null)
        {
            Instantiate(disableEffect, transform.position, transform.rotation, null);
        }

        if (enableDebugLogging)
        {
            Debug.Log($"[KeyPressDisabler] Object disabled after {currentPresses} presses!");
        }

        // Disable this gameobject
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Description:
    /// Resets the press counter and re-enables the object
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    public void ResetAndEnable()
    {
        currentPresses = 0;
        hasBeenDisabled = false;
        gameObject.SetActive(true);
        UpdateDisplayText();

        if (enableDebugLogging)
        {
            Debug.Log("[KeyPressDisabler] Reset and enabled.");
        }
    }

    /// <summary>
    /// Description:
    /// Resets the press counter (keeps object active)
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    public void ResetCounter()
    {
        currentPresses = 0;
        UpdateDisplayText();

        if (enableDebugLogging)
        {
            Debug.Log("[KeyPressDisabler] Counter reset.");
        }
    }

    /// <summary>
    /// Description:
    /// Gets the current number of presses
    /// Inputs:
    /// none
    /// Returns:
    /// int
    /// </summary>
    /// <returns>Current press count</returns>
    public int GetCurrentPresses()
    {
        return currentPresses;
    }

    /// <summary>
    /// Description:
    /// Gets the remaining presses needed to disable
    /// Inputs:
    /// none
    /// Returns:
    /// int
    /// </summary>
    /// <returns>Presses remaining</returns>
    public int GetRemainingPresses()
    {
        return Mathf.Max(0, pressesRequired - currentPresses);
    }

    /// <summary>
    /// Description:
    /// Sets the key to listen for (useful for dynamic configuration)
    /// Inputs:
    /// KeyCode newKey
    /// Returns:
    /// void (no return)
    /// </summary>
    /// <param name="newKey">The new key code to listen for</param>
    public void SetActivationKey(KeyCode newKey)
    {
        activationKey = newKey;
        if (enableDebugLogging)
        {
            Debug.Log($"[KeyPressDisabler] Activation key changed to: {newKey}");
        }
    }

    /// <summary>
    /// Description:
    /// Manually trigger the disable (useful for external systems like Unicron Black)
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    public void ForceDisable()
    {
        DisableObject();
    }
}
