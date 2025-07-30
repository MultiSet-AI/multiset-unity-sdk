using System.Collections.Generic;
using UnityEngine;

/**
 * Shows & handles UI of a list with different items o select
 */
public class TaskList : MonoBehaviour
{
    // to render stuff
    public RectTransform content;      // parent of spawn point
    public Transform SpawnPoint;       // spawn point of items
    public GameObject spawnItem;       // prefab of item to be spawned
    public int heightOfPrefab;         // height of spawnItem

    public List<TrainingTask> tasks; // all items available for list

    public void RenderTasks()
    {
        // render list
        RenderList(tasks);
    }

    /**
     * Renders given items as a list
     */
    public void RenderList(List<TrainingTask> items)
    {
        // remove previous items first
        foreach (Transform child in SpawnPoint.transform)
        {
            Destroy(child.gameObject);
        }

        int poisCount = items.Count;

        // loop over POIs of this space
        for (int i = 0; i < poisCount; i++)
        {
            TrainingTask item = items[i];

            // y where to spawn destinations
            float spawnY = i * heightOfPrefab; // calculate new spawn point
            Vector3 pos = new Vector3(SpawnPoint.localPosition.x, -spawnY, SpawnPoint.localPosition.z);

            //instantiate Prefab at spawn point
            GameObject SpawnedItem = Instantiate(spawnItem, pos, SpawnPoint.rotation);

            //set parent
            SpawnedItem.transform.SetParent(SpawnPoint, false);

            // set poi item for reference
            TaskItemUI itemUI = SpawnedItem.GetComponent<TaskItemUI>();
            itemUI.SetListItemData(item);
        }

        //set content holder height
        content.sizeDelta = new Vector2(0, poisCount * heightOfPrefab);
    }
}
