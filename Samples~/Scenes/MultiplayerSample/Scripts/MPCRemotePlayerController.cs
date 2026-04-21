using UnityEngine;
using TMPro;

/// <summary>
/// Controls a remote player received via MultipeerConnectivity.
/// Uses the PlayerPose prefab (BoundingBox + NameText) for visualization.
/// </summary>
public class MPCRemotePlayerController : MonoBehaviour
{
    [HideInInspector] public string peerID;
    [HideInInspector] public string playerName;
    [HideInInspector] public Color playerColor = Color.cyan;
    [HideInInspector] public bool isLocalized;

    [HideInInspector] public Vector3 targetPosition;
    [HideInInspector] public Quaternion targetRotation = Quaternion.identity;
    [HideInInspector] public float lastUpdateTime;

    private TextMeshProUGUI nameText;
    private Camera cachedCamera;

    // Adaptive smoothing: fast for small updates, slow for large jumps (re-localization)
    private const float FastSmoothing = 15f;
    private const float SlowSmoothing = 2f;
    private const float JumpThreshold = 0.5f; // meters — positions > this apart are re-localizations
    private float currentSmoothing = FastSmoothing;

    // Stale pose detection: skip identical poses from the Meta glasses
    private Vector3 lastReceivedPosition;
    private bool hasReceivedFirstPose;

    public void Initialize(string name, Color color)
    {
        playerName = name;
        playerColor = color;

        nameText = GetComponentInChildren<TextMeshProUGUI>();
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (nameText != null)
            nameText.text = playerName;
    }

    /// <summary>
    /// Called by MultiplayerManager when a new pose arrives.
    /// Detects re-localization jumps and adjusts smoothing speed.
    /// </summary>
    public void SetTarget(Vector3 position, Quaternion rotation)
    {
        // Skip stale duplicate poses (Meta glasses send same pose at 20Hz between re-localizations)
        if (hasReceivedFirstPose && Vector3.Distance(position, lastReceivedPosition) < 0.001f)
            return;

        // Detect large jump → re-localization happened, use slow smooth transition
        if (hasReceivedFirstPose && Vector3.Distance(position, targetPosition) > JumpThreshold)
        {
            currentSmoothing = SlowSmoothing;
        }

        lastReceivedPosition = position;
        hasReceivedFirstPose = true;
        targetPosition = position;
        targetRotation = rotation;
        lastUpdateTime = Time.time;
    }

    public void SmoothUpdate(float deltaTime)
    {
        float t = Mathf.Min(deltaTime * currentSmoothing, 1f);
        transform.position = Vector3.Lerp(transform.position, targetPosition, t);

        // Ramp smoothing back up to fast once we're close to target
        float remaining = Vector3.Distance(transform.position, targetPosition);
        if (remaining < 0.05f)
            currentSmoothing = FastSmoothing;

        // Billboard toward camera
        if (cachedCamera == null)
            cachedCamera = Camera.main;
        if (cachedCamera == null) return;

        Vector3 dirToCamera = cachedCamera.transform.position - transform.position;
        if (dirToCamera.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(-dirToCamera, Vector3.up);
    }
}
