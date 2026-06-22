using UnityEngine;

/**
 * Handles arrival of user (ARCamera) in collider of poi.
 * 
 * Note: Is only triggered when collider of ARCamera is enabled during navigation.
 */
public class POICollider : MonoBehaviour
{
    // assigned POI to this collider
    POI poi;

    // Detect if user (respectively ARCamera) hits collider of POI.
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "ARCamera")
        {
            Debug.Log("User visited " + poi.poiName);
            poi.Arrived();
        }
    }

    // Detect if user (respectively ARCamera) left collider of POI.
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "ARCamera")
        {
            Debug.Log("User left " + poi.poiName);
        }
    }

    // Set poi from POI script.
    public void SetPOI(POI aPoi)
    {
        poi = aPoi;
    }
}
