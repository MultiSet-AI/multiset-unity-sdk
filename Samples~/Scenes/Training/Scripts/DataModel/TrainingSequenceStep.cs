using UnityEngine;

public class TrainingSequenceStep : MonoBehaviour
{
    // title for this step
    public string stepTitle;

    // location represented by point of interest
    public POI location;

    // training steps at this location
    private TrainingTask[] tasks;

    void Awake()
    {
        tasks = GetComponents<TrainingTask>();
    }

    public TrainingTask[] GetTasks()
    {
        return tasks;
    }

    public void CheckForStepCompletion()
    {
        bool isAllDone = true;
        foreach (var task in tasks)
        {
            if (!task.isDone())
            {
                isAllDone = false;
                break;
            }
        }

        TrainingUIController.instance.SetNextStepButtonVisibility(isAllDone);
    }
}
