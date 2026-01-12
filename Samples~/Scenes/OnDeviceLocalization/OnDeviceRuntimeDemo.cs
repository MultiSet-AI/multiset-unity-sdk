/// <summary>
/// Demonstrates how to use public methods from OnDeviceLocalizationManager.
///
/// Usage:
/// - Call <see cref="InitiateOnDeviceLocalization"/> to validate and set up a map or mapset code
///   at runtime using <see cref="OnDeviceLocalizationManager.ValidateMapOrMapSetCode"/>.
/// - Call <see cref="Localize"/> to capture a frame and request localization
///   using <see cref="OnDeviceLocalizationManager.LocalizeFrame"/>.
/// </summary>

using MultiSet;
using UnityEngine;

public class OnDeviceRuntimeDemo : MonoBehaviour
{
    [SerializeField] private OnDeviceLocalizationManager onDeviceLocalizationManager;
    [SerializeField] private string mapOrMapsetCode;

    void Start()
    {
        if (onDeviceLocalizationManager == null)
            onDeviceLocalizationManager = FindFirstObjectByType<OnDeviceLocalizationManager>();
    }


    // On-Device-Localization, Capture Frame and Request Localization
    public void Localize()
    {
        onDeviceLocalizationManager.LocalizeFrame();
    }

    public void InitiateOnDeviceLocalization()
    {
        if (string.IsNullOrWhiteSpace(mapOrMapsetCode))
        {
            Debug.LogError("Invalid Map or MapSet Code!");
            return;
        }

        if (onDeviceLocalizationManager == null)
        {
            Debug.LogError("OnDeviceLocalizationManager not found!");
            return;
        }

        onDeviceLocalizationManager.ValidateMapOrMapSetCode(mapOrMapsetCode);
    }
}
