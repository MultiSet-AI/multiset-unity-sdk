using UnityEngine;

public class TrainingTask : MonoBehaviour
{
    // title for this task
    public string taskTitle;

    // true when task is completed
    bool isTaskCompleted = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public bool isDone()
    {
        return isTaskCompleted;
    }

    public void setTaskState(bool state)
    {
        isTaskCompleted = state;
        TrainingController.instance.TaskStateToggled();
    }
}
