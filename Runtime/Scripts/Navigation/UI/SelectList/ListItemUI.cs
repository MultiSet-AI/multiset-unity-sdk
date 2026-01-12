

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/**
 * Represents data object as list item in UI.
 * Can be used with multiple data objects.
 */
public class ListItemUI : MonoBehaviour
{
    [Tooltip("Title of list element in UI")]
    public TextMeshProUGUI title;

    [Tooltip("Button to start navigation")]
    public GameObject startNavigationButton;

    [Tooltip("Button to select item")]
    public Button itemSelectButton;

    [Tooltip("Data object for this list item")]
    public ListItemData dataObject;

    [Tooltip("Label to show distance to the object")]
    public TextMeshProUGUI distance;

    // Set variables for this list item. Should be called during rendering of item list.
    public void SetListItemData(ListItemData data)
    {
        dataObject = data;
        title.text = data.listTitle;

        // only enable go button if data object is poi
        startNavigationButton.SetActive(data is POI);
    }

    void Update()
    {
        distance.text = GetDistance();
    }

    // click on go button
    public void Go()
    {
        if (dataObject is POI)
        {
            NavigationUIController.instance.ClickedStartNavigation((dataObject as POI));
        }
    }

    // get estimated distance for this object
    string GetDistance()
    {
        float distance = PathEstimationUtils.instance.EstimateDistanceToPosition(dataObject as POI);
        if (distance > 0)
        {
            return (int)distance + " m";
        }
        else if (distance == -2)
        {
            return "Unreachable";
        }
        else
        {
            return "bla";
        }
    }
}
