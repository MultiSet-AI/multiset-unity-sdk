using UnityEngine;

/// <summary>
/// Drives idle/walking animation on a humanoid child of PlayerPose
/// by detecting whether the parent is moving in the AR scene.
/// Also rotates the child to face the movement direction while walking.
/// </summary>
public class PlayerWalking : MonoBehaviour
{
    [Tooltip("Minimum speed (m/s) to trigger walking animation")]
    [SerializeField] private float moveThreshold = 0.05f;

    [Tooltip("How quickly the smoothed speed responds (lower = more filtering)")]
    [SerializeField] private float smoothing = 5f;

    [Tooltip("How quickly the model rotates to face movement direction (degrees/sec)")]
    [SerializeField] private float rotationSpeed = 10f;

    private Animator animator;
    private Vector3 lastPosition;
    private float smoothedSpeed;
    private Quaternion targetWorldRotation;
    private bool hasTargetRotation;
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;

        lastPosition = transform.parent != null ? transform.parent.position : transform.position;
    }

    void Update()
    {
        Vector3 currentPos = transform.parent != null ? transform.parent.position : transform.position;
        Vector3 delta = currentPos - lastPosition;
        float instantSpeed = delta.magnitude / Time.deltaTime;
        lastPosition = currentPos;

        // Smooth the speed to filter out AR tracking jitter
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, instantSpeed, Time.deltaTime * smoothing);

        bool walking = smoothedSpeed > moveThreshold;

        if (animator != null)
            animator.SetBool(IsWalking, walking);

        // Compute facing direction from horizontal movement
        Vector3 horizontalDelta = new Vector3(delta.x, 0f, delta.z);
        if (horizontalDelta.sqrMagnitude > 0.0001f * Time.deltaTime * Time.deltaTime)
        {
            targetWorldRotation = Quaternion.LookRotation(horizontalDelta.normalized, Vector3.up);
            hasTargetRotation = true;
        }

        // Smoothly rotate toward movement direction (in world space, independent of parent billboard)
        if (hasTargetRotation)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetWorldRotation, Time.deltaTime * rotationSpeed);
        }
    }
}
