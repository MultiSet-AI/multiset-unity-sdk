using UnityEngine;

public class POI : ListItemData
{
    // unique identifier for this POI
    int id;

    [Tooltip("Identification for this POI, e.g. room number")]
    public int identification;

    [Tooltip("Name of this POI")]
    public string poiName;

    [Tooltip("Description for this POI")]
    public string description;

    [Tooltip("Type for this POI")]
    public POIType type;

    [Tooltip("Collider for path calculation and user arrival detection. Place pivot point near NavMesh. Collider should be near NavMesh")]
    public POICollider poiCollider;

    [Tooltip("Visual representation for this POI")]
    public POISign sign;

    void Awake()
    {
        base.listTitle = poiName;
        id = identification; // this can be adapted if you get id from external source
        sign.SetPOI(this);
        poiCollider.SetPOI(this);
    }

    // returns this id
    public int GetId()
    {
        return id;
    }

    // Handles arrival of user at POI
    public void Arrived()
    {
        if (NavigationController.instance.currentDestination != null && NavigationController.instance.currentDestination.GetId() == id)
        {
            // arrived at the selected POI
            NavigationController.instance.ArrivedAtDestination();
        }
    }
}

// various POI type, could be used to render POI specific icon
public enum POIType { Room, VendingMachine, Exit, Staircase, Toilet, FoodArea, Information, BookShelf, Safety, Elevator, Printer, Kitchen }
