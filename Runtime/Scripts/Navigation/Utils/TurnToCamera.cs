using UnityEngine;

/**
 * Turns game object to main camera while enabled.
 */
public class TurnToCamera : MonoBehaviour
{
    // ARCamera of the scene
    Camera ARCamera;

    void Start()
    {
        ARCamera = Camera.main;
    }

    void Update()
    {
        Transform target = ARCamera.transform;
        Vector3 targetPosition = new Vector3(target.position.x,
                                        this.transform.position.y,
                                        target.position.z);
        this.transform.LookAt(targetPosition);
    }
}
