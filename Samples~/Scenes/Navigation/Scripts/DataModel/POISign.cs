using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/**
 * Represents sign of poi.
 * Handles clicking on sign of poi.
 */
public partial class POISign : MonoBehaviour
{
    // POI for this sign
    POI poi;

    [Tooltip("Label to show poi name")]
    public TextMeshProUGUI title;

    [Tooltip("Background image of POI sign")]
    public Image backgroundSignImage;

    // max distance allowed to click sign from
    const float MAX_SIGN_CLICK_DISTANCE = 7.5f;

    // handle click on sign
    private void HandleClick()
    {
        float distanceToSign = Vector3.Distance(Camera.main.transform.position, gameObject.transform.position);
        if (distanceToSign > MAX_SIGN_CLICK_DISTANCE)
        {
            return;
        }
        Debug.Log("Clicked POI: " + poi.poiName);
    }

    // Set POI data object from parent
    public void SetPOI(POI aPoi)
    {
        poi = aPoi;
        title.text = aPoi.poiName;
    }
}


#if UNITY_EDITOR || UNITY_STANDALONE
// handle click inside editor

public partial class POISign : MonoBehaviour, IPointerClickHandler
{
    // Use OnPointerClick for desktop platforms
    public void OnPointerClick(PointerEventData eventData)
    {
        HandleClick();
    }
}

#else
// handle click on mobile

public partial class POISign : MonoBehaviour, IPointerDownHandler
{
    // Use OnPointerDown for mobile platforms
    public void OnPointerDown(PointerEventData eventData)
    {
        HandleClick();
    }
}

#endif