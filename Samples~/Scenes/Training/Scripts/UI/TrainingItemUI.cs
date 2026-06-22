using UnityEngine;
using TMPro;

/**
 * Represents training item in UI.
 */
public class TrainingItemUI : MonoBehaviour
{
    [Tooltip("Title of list element in UI")]
    public TextMeshProUGUI title;

    [Tooltip("Data object for this list item")]
    public TrainingSequence dataObject;

    // Set variables for this list item. Should be called during rendering of item list.
    public void SetListItemData(TrainingSequence data)
    {
        dataObject = data;
        title.text = data.sequenceTitle;
    }

    public void SelectSequence()
    {
        TrainingController.instance.StartTraining(dataObject);
    }
}
