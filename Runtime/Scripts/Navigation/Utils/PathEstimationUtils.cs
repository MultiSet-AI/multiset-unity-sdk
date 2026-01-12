using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

/**
 * Estimate remaining path to selected POI
 **/
public class PathEstimationUtils : MonoBehaviour
{
    public static PathEstimationUtils instance;

    // distance
    float remainingDistance = 0;

    // ARCamera of the scene
    Camera ARCamera;

    // collider radius of the ARCamera used to detect arrival, radius is subtracted from overall path length
    float arCameraColliderRadius;

    // current destination
    POI destination;

    // collider radius of the current destination, radius is subtracted from overall path length
    float destinationColliderRadius;

    [Tooltip("Slider to visualize progress")]
    public Slider progressSlider;

    // distance when starting navigation
    float startingDistance = 0;

    // true when estimation started
    bool estimationStarted = false;

    void Awake()
    {
        instance = this;
        ARCamera = Camera.main;
    }

    void Start()
    {
        arCameraColliderRadius = ARCamera.gameObject.GetComponent<SphereCollider>().radius;
    }

    void FixedUpdate()
    {
        HandleProgress();
    }

    public void HandleProgress()
    {
        if (estimationStarted)
        {
            float currentDistance = startingDistance - remainingDistance;
            progressSlider.value = currentDistance / startingDistance + 0.03f;
        }
    }

    /**
     * Estimates time from first to last position of given Vector3 path, e.g. from NavMeshAgent.
     */
    public void UpdateEstimation(Vector3[] path)
    {
        if (destination == null)
        {
            destination = NavigationController.instance.currentDestination;
            destinationColliderRadius = destination.poiCollider.gameObject.GetComponent<SphereCollider>().radius;
        }

        if (path.Length > 1)
        {
            float remainingPathTotal = 0;
            // loop through path and add up distance between Vectors
            for (int i = 0; i < path.Length; i++)
            {
                if (i < path.Length - 1) // we always comparing with next one
                {
                    remainingPathTotal += Vector3.Distance(path[i], path[i + 1]);
                }
            }
            remainingDistance = remainingPathTotal - arCameraColliderRadius - destinationColliderRadius;

            if (!estimationStarted || remainingDistance > startingDistance)
            {
                // user walked away from destination again, reset starting distance
                startingDistance = remainingDistance;
                estimationStarted = true;
            }
        }
    }

    // reset estimation state
    public void ResetEstimation()
    {
        destination = null;
        estimationStarted = false;
        remainingDistance = 0;
    }

    // returns remaining distance as int
    public int getRemainingDistanceMeters()
    {
        return (int)remainingDistance;
    }

    // Returns distance from agent to given destination
    public float EstimateDistanceToPosition(POI destination)
    {
        NavMeshPath path = new NavMeshPath();

        if (!NavigationController.instance.agent.isOnNavMesh)
        {
            return -1;
        }

        NavMesh.CalculatePath(NavigationController.instance.agent.gameObject.transform.position, destination.poiCollider.transform.position, NavMesh.AllAreas, path);

        if (path.status == NavMeshPathStatus.PathPartial || path.status == NavMeshPathStatus.PathInvalid)
        {
            // handle unreachable route
            return -2;
        }

        if (path.corners.Length > 1)
        {
            float distance = 0;
            for (int i = 0; i < path.corners.Length; i++)
            {
                if (i < path.corners.Length - 1) // we always comparing with next one
                {
                    distance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
                }
            }
            return distance;
        }

        return -1;
    }

}