using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

/**
 * Shows & handles UI of a list with different items o select
 */
public class SelectList : MonoBehaviour
{
    // to render stuff
    public RectTransform content;      // parent of spawn point
    public Transform SpawnPoint;       // spawn point of items
    public GameObject spawnItem;       // prefab of item to be spawned
    public int heightOfPrefab;         // height of spawnItem

    // additional UI
    public TMP_InputField searchField;
    public GameObject resetButtonSearchField;
    public GameObject placeholder;

    public List<ListItemData> pois; // all items available for list

    List<ListItemData> currentItemsTotal;

    public void Awake()
    {
        PrepareAllData();
    }

    void PrepareAllData()
    {
        pois = new List<ListItemData>();

        foreach (var poi in NavigationController.instance.augmentedSpace.GetPOIs())
        {
            pois.Add(poi);
        }
    }

    public void RenderPOIs()
    {
        // render list
        RenderList(pois);
        currentItemsTotal = pois;
    }

    /**
     * Renders given items as a list
     */
    public void RenderList(List<ListItemData> items)
    {
        // sort pois alphabetically
        items.Sort(CompareItemTitle);

        // remove previous items first
        foreach (Transform child in SpawnPoint.transform)
        {
            Destroy(child.gameObject);
        }

        int poisCount = items.Count;

        // loop over pois of this space
        for (int i = 0; i < poisCount; i++)
        {
            ListItemData item = items[i];

            // y where to spawn destinations
            float spawnY = (i * heightOfPrefab); // calculate new spawn point
            Vector3 pos = new Vector3(SpawnPoint.localPosition.x, -spawnY, SpawnPoint.localPosition.z);

            //instantiate Prefab at spawn point
            GameObject SpawnedItem = Instantiate(spawnItem, pos, SpawnPoint.rotation);

            //set parent
            SpawnedItem.transform.SetParent(SpawnPoint, false);

            // set poi item for reference
            ListItemUI itemUI = SpawnedItem.GetComponent<ListItemUI>();
            itemUI.SetListItemData(item);
        }

        //set content holder height
        content.sizeDelta = new Vector2(0, poisCount * heightOfPrefab);
    }

    /**
     * Resets list.
     */
    public void ResetPOISearch()
    {
        searchField.text = "";
        resetButtonSearchField.SetActive(false);
        RenderList(currentItemsTotal);
        placeholder.SetActive(true);
    }

    /**
     * Selects input search field.
     */
    public void SelectSearchInputField()
    {
        searchField.Select();
    }

    /**
     * Call when search string changed.
     */
    public void SearchPOIOnSearchChanged(string search)
    {
        if (search == "")
        {
            resetButtonSearchField.SetActive(false);
        }
        else
        {
            resetButtonSearchField.SetActive(true);
        }

        RenderList(FilterByTitle(search));
    }

    /**
     * Filters poi list by title.
     */
    List<ListItemData> FilterByTitle(string searchTerm)
    {
        string search = searchTerm.ToLower();
        List<ListItemData> filteredItems = currentItemsTotal.FindAll(x =>
        {
            if (x.listTitle.ToLower().Contains(search))
            {
                return true;
            }
            else
            {
                return false;
            }
        });
        return filteredItems;
    }

    /**
     * Call to reset search.
     */
    public void ResetSearch()
    {
        searchField.text = "";
        if (placeholder != null)
        {
            placeholder.SetActive(true);
        }
    }

    /**
     * Sorting by item titl.
     */
    int CompareItemTitle(ListItemData a, ListItemData b)
    {
        // Here we sort two times at once, first one the first item, then on the second.
        // ... Compare the first items of each element.
        var part1 = a.listTitle;
        var part2 = b.listTitle;
        var compareResult = part1.CompareTo(part2);
        // If the first items are equal (have a CompareTo result of 0) then compare on the second item.
        if (compareResult == 0)
        {
            return b.listTitle.CompareTo(a.listTitle);
        }
        // Return the result of the first CompareTo.
        return compareResult;
    }
}
