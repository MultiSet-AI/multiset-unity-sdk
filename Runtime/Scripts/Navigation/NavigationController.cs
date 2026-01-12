using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/**
 * Handles the agent and other controllers to navigate a user in AR to a selected destination.
 */
public class NavigationController : MonoBehaviour
{
    public static NavigationController instance;

    private Camera ARCamera;

    // collider of the ARCamera to detect POI arrival
    private SphereCollider ARCameraCollider;

    [Tooltip("NavMesh agent child of ARCamera")]
    public NavMeshAgent agent;

    [Tooltip("Current POI for navigation")]
    public POI currentDestination;

    [Tooltip("Space that contains POIs")]
    public AugmentedSpace augmentedSpace;

    // Position tracking to reduce unnecessary updates
    private Vector3 lastAgentPosition;
    [Tooltip("Minimum distance the agent needs to move before updating path")]
    public float positionUpdateThreshold = 0.5f; // in meters
    public UnityEvent DestinationArrived = new UnityEvent();

    void Awake()
    {
        instance = this;
        ARCamera = Camera.main;
    }

    void Start()
    {
        ARCameraCollider = ARCamera.GetComponent<SphereCollider>();
        if (currentDestination)
        {
            StartNavigation();
        }
        lastAgentPosition = agent.transform.position;
    }

    void Update()
    {
        if (agent.isOnNavMesh)
        {
            // stopped the NavMesh agent to walk to destination
            agent.isStopped = true;
        }

        if (IsCurrentlyNavigating() && agent.isOnNavMesh)
        {
            agent.destination = currentDestination.poiCollider.transform.position;

            // Only update the path starting position if we've moved enough
            float distanceMoved = Vector3.Distance(agent.transform.position, lastAgentPosition);
            if (distanceMoved > positionUpdateThreshold)
            {
                lastAgentPosition = agent.transform.position;
                // when we are navigating and we are localized path needs to go from current agent position
                ShowPath.instance.SetPositionFrom(agent.transform);
            }

            // enable collider to detect arrival
            ARCameraCollider.enabled = true;
        }
        else
        {
            ARCameraCollider.enabled = false;
        }
    }

    // Sets a POI for navigation and gets ready for navigation.
    public void SetPOIForNavigation(POI aPOI)
    {
        currentDestination = aPOI;
        StartNavigation();
    }

    // Sets positions for ShowPath to start navigation.
    void StartNavigation()
    {
        lastAgentPosition = agent.transform.position;
        ShowPath.instance.SetPositionFrom(agent.transform);
        ShowPath.instance.SetPositionTo(currentDestination.poiCollider.transform);
    }

    // Stops navigation.
    public void StopNavigation()
    {
        if (currentDestination != null)
        {
            currentDestination = null;
            ShowPath.instance.ResetPath();
            PathEstimationUtils.instance.ResetEstimation();
        }
    }

    // Handles destination arrival. Is called from POI.Arrived()
    public void ArrivedAtDestination()
    {
        DestinationArrived.Invoke();
        StopNavigation();
        NavigationUIController.instance.ShowArrivedState();
    }

    //Returns true when user is currently navigating.
    public bool IsCurrentlyNavigating()
    {
        return currentDestination != null;
    }

    //Toggles the nav mesh agent capsule visibility
    public void ToggleAgentVisibility()
    {
        agent.gameObject.GetComponent<MeshRenderer>().enabled = !agent.gameObject.GetComponent<MeshRenderer>().enabled;
    }
}