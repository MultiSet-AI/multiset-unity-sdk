using UnityEngine;
using TMPro;

/**
 * Represents task item in UI.
 */
public class TaskItemUI : MonoBehaviour
{
    [Tooltip("Title of list element in UI")]
    public TextMeshProUGUI title;

    [Tooltip("Empty checkbox to show task state")]
    public GameObject emptyCheckBox;

    [Tooltip("Checked box to show task state")]
    public GameObject checkedBox;

    [Tooltip("Data object for this list item")]
    public TrainingTask dataObject;

    // Set variables for this list item. Should be called during rendering of item list.
    public void SetListItemData(TrainingTask data)
    {
        dataObject = data;
        title.text = data.taskTitle;
        dataObject.setTaskState(false);
        RenderTaskState();
    }

    void RenderTaskState()
    {
        checkedBox.SetActive(dataObject.isDone());
        emptyCheckBox.SetActive(!dataObject.isDone());
    }

    public void ToggleTask()
    {
        dataObject.setTaskState(!dataObject.isDone());
        RenderTaskState();
    }
}
