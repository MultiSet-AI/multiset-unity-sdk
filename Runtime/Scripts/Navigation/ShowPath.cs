using System.Collections;
using MultiSet;
using UnityEngine;
using UnityEngine.AI;

/**
  * Visualizes path between two points on NavMesh.
  *
  * Path is calculated every 0.5 second (twice per second) to reduce fluctuations
  * 
  * Line is drawn with LineRenderer using path.corners, Source: https://gamedev.stackexchange.com/a/86255
  * 
  * Possibility to add indicator when line is out of view: https://assetstore.unity.com/packages/tools/gui/off-screen-target-indicator-71799
  */
public class ShowPath : MonoBehaviour
{
    public static ShowPath instance;

    //Line
    LineRenderer line;

    // path of agent
    NavMeshPath path;

    // timer
    float _elapsed = 0.0f;

    [Tooltip("Path update frequency in seconds")]
    public float pathUpdateFrequency = 0.5f; // Update twice per second

    [Tooltip("Height of the path above NavMesh")]
    public float pathHeightAboveGround = 0.1f; // in meters

    // start and destination transforms
    Transform a = null;
    Transform b = null;

    [Tooltip("Prefab to visualize path corners")]
    public GameObject cornerVisualizationPrefab;

    // holds all current corner GameObjects that are visualized
    GameObject[] visibleCorners = { };

    [Tooltip("Toggles visibility of path corners")]
    public bool isCornersVisible;

    // true when showCornersToggle was used, needed to track change so we don't loop all the time 
    bool cornerVisibilityHasChanged = false;

    // Flag to track if we need to force path recalculation
    bool forcePathRecalculation = false;

    void Awake()
    {
        instance = this;
        line = GetComponent<LineRenderer>();

        line.alignment = LineAlignment.TransformZ;
        line.transform.forward = Vector3.up; // Forces it to stay flat and not roll
    }

    void Start()
    {
        path = new NavMeshPath();
        line.enabled = false;
        isCornersVisible = false;
    }

    void Update()
    {
        if (a != null && b != null)
        {
            line.enabled = true;

            // Calculate path only twice per second (every 0.5 seconds)
            _elapsed += Time.deltaTime;
            if (_elapsed > pathUpdateFrequency || forcePathRecalculation)
            {
                // line.SetPosition(0, a.position); // set first point of line

                StartCoroutine(DrawPath(path));
                PathEstimationUtils.instance.UpdateEstimation(path.corners);


                _elapsed = 0.0f;
                forcePathRecalculation = false;
                NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path);
            }
        }
        else
        {
            line.enabled = false;
            SetCornerVisibility(false);
        }

        if (a != null && b != null && NavigationController.instance.IsCurrentlyNavigating())
        {
            if (path.status == NavMeshPathStatus.PathPartial || path.status == NavMeshPathStatus.PathInvalid)
            {
                // handle unreachable route
                ToastManager.Instance.ShowAlert("Problem calculating route");
                NavigationController.instance.StopNavigation();
            }
        }
    }

    // Draws shortest line from NavMeshAgent to destination
    IEnumerator DrawPath(NavMeshPath path)
    {
        yield return new WaitForEndOfFrame(); // wait for path to be drawn

        if (path.corners.Length < 2) // if the path has 1 or no corners, there is no need
            yield break;

        line.positionCount = path.corners.Length; // set the array of positions to the amount of corners

        if (isCornersVisible)
        {
            cornerVisibilityHasChanged = true;
            if (cornerVisibilityHasChanged)
            {
                SetCornerVisibility(true);
            }
            HandlePathCornerVisualization();
        }
        else
        {
            cornerVisibilityHasChanged = true;
            if (cornerVisibilityHasChanged)
            {
                SetCornerVisibility(false);
            }
        }

        for (var i = 0; i < path.corners.Length; i++)
        {
            // go through each corner and set that to the line renderer's position, a little bit over ground
            Vector3 linePosition = new Vector3(path.corners[i].x, path.corners[i].y + pathHeightAboveGround, path.corners[i].z);
            line.SetPosition(i, linePosition);

            if (isCornersVisible)
            {
                UpdateVisibleCorner(i, linePosition);
            }
        }
    }

    // Reset path.
    public void ResetPath()
    {
        StopAllCoroutines();
        a = null;
        b = null;
        line.positionCount = 1;
    }

    // Set Transform of path start
    public void SetPositionFrom(Transform from)
    {
        a = from;
        forcePathRecalculation = true;
    }

    // set Transform of path end
    public void SetPositionTo(Transform to)
    {
        b = to;

        if (b != null)
        {
            // Force immediate path calculation when destination is set
            forcePathRecalculation = true;
            NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path);
        }
    }

    // Handles the visualization of the path corners.
    void HandlePathCornerVisualization()
    {
        // handle visualized corners
        int pathCornersCount = path.corners.Length;
        if (pathCornersCount > visibleCorners.Length)
        {
            // new corners we haven't visualized yet
            if (visibleCorners.Length == 0)
            {
                // there are no corners yet
                visibleCorners = new GameObject[pathCornersCount];
            }
            else
            {
                // we need to create new array with current size and copy over old objects
                GameObject[] newVisibleCorners = new GameObject[pathCornersCount];
                for (int i = 0; i < visibleCorners.Length; i++)
                {
                    newVisibleCorners[i] = visibleCorners[i];
                }
                visibleCorners = newVisibleCorners;
            }
        }
        else if (pathCornersCount < visibleCorners.Length)
        {
            // there are less corners in the path, delete the once that are not used, source: https://www.c-sharpcorner.com/article/how-to-remove-an-element-from-an-array-in-c-sharp/
            int elementsToRemoveCount = visibleCorners.Length - pathCornersCount;
            GameObject[] newVisibleCorners = new GameObject[visibleCorners.Length - elementsToRemoveCount];

            for (int i = 0; i < visibleCorners.Length; i++)
            {
                if (i < newVisibleCorners.Length)
                {
                    // copy old visible corner to new one
                    newVisibleCorners[i] = visibleCorners[i];
                }
                else
                {
                    // remove deleted corner
                    Destroy(visibleCorners[i]);
                }
            }
            visibleCorners = newVisibleCorners;
        }
        else
        {
            // amount of corners stayed the same, do nothing
        }
    }

    // Updates the position of a corner
    void UpdateVisibleCorner(int i, Vector3 newPosition)
    {
        if (visibleCorners[i] == null)
        {
            // there is no instantiated corner yet
            GameObject newCorner = GameObject.Instantiate(cornerVisualizationPrefab, newPosition, Quaternion.identity);
            visibleCorners[i] = newCorner;
        }
        else
        {
            // update the previously instantiated corner
            visibleCorners[i].gameObject.transform.position = newPosition;
        }
    }

    // Set the visibility of corners.
    void SetCornerVisibility(bool show)
    {
        cornerVisibilityHasChanged = false;
        foreach (var corner in visibleCorners)
        {
            if (corner != null)
            {
                corner.gameObject.SetActive(show);
            }
        }
    }
}