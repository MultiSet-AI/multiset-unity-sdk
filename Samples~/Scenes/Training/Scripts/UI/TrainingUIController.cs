using System.Linq;
using TMPro;
using UnityEngine;

public class TrainingUIController : MonoBehaviour
{
    public static TrainingUIController instance;

    public GameObject TrainingsListParent;
    public GameObject tasksBoard;
    public GameObject NextStepButton;
    public TextMeshProUGUI nextStepButtonLabel;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        tasksBoard.SetActive(false);
        SetNextStepButtonVisibility(false);
    }

    public void SetTrainingsListVisible(bool isVisible)
    {
        TrainingsListParent.SetActive(isVisible);
    }

    public void ShowTaskBoard(TrainingSequenceStep step, bool isLastStep)
    {
        if (!isLastStep)
        {
            nextStepButtonLabel.text = "Next Step";
        }
        else
        {
            nextStepButtonLabel.text = "End Training";
        }
        tasksBoard.SetActive(true);
        tasksBoard.GetComponentInChildren<TaskList>().RenderList(step.GetTasks().ToList());
    }

    public void CloseTaskBoard()
    {
        tasksBoard.SetActive(false);
        SetNextStepButtonVisibility(false);
    }

    public void SetNextStepButtonVisibility(bool isVisible)
    {
        NextStepButton.SetActive(isVisible);
    }

    public void NextStepButtonClicked()
    {
        TrainingController.instance.HandleNextStep();
    }
}
