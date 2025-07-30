using UnityEngine;

/**
 * Represents an area in real life but in the digital world.
 */
public class AugmentedSpace : MonoBehaviour
{
    [Tooltip("Title of the space")]
    public string title;

    // POIs inside this space
    POI[] pois = { };

    [Tooltip("Parent of the POIs inside this space")]
    public GameObject augmentation;

    void Awake()
    {
        pois = augmentation.GetComponentsInChildren<POI>(true);
    }

    // Returns POIs of this space.
    public POI[] GetPOIs()
    {
        return pois;
    }
}
