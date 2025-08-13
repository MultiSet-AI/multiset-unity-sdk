/*
Copyright (c) 2025 MultiSet AI. All rights reserved.
Licensed under the MultiSet License. You may not use this file except in compliance with the License. and you can’t re-distribute this file without a prior notice
For license details, visit www.multiset.ai.
Redistribution in source or binary forms must retain this notice.
*/

using System.Linq;
using MultiSet;
using UnityEngine;

public class TrainingController : MonoBehaviour
{
    public static TrainingController instance;
    private TrainingSequence currentTraining;

    private TrainingSequence[] trainings;

    int currentSequenceStepIndex = 0;

    void Awake()
    {
        instance = this;
        trainings = GetComponentsInChildren<TrainingSequence>();
    }

    void Start()
    {
        TrainingUIController.instance.TrainingsListParent.GetComponentInChildren<TrainingList>().RenderList(trainings.ToList<TrainingSequence>());
        NavigationController.instance.DestinationArrived.AddListener(HandleDestinationArrival);
    }

    public void StartTraining(TrainingSequence sequence)
    {
        TrainingUIController.instance.SetTrainingsListVisible(false);
        currentTraining = sequence;
        currentSequenceStepIndex = 0;
        NavigationController.instance.SetPOIForNavigation(currentTraining.GetTrainingSequenceSteps()[currentSequenceStepIndex].location);
        ToastManager.Instance.ShowAlert("Please follow the line.");
    }

    void HandleDestinationArrival()
    {
        bool isLastStep = currentSequenceStepIndex == currentTraining.GetTrainingSequenceSteps().Length - 1;
        TrainingUIController.instance.ShowTaskBoard(currentTraining.GetTrainingSequenceSteps()[currentSequenceStepIndex], isLastStep);
    }

    public void HandleNextStep()
    {
        if (currentSequenceStepIndex < currentTraining.GetTrainingSequenceSteps().Length - 1)
        {
            currentSequenceStepIndex++;
            ToastManager.Instance.ShowAlert("Please follow the line.");
            TrainingUIController.instance.CloseTaskBoard();
            TrainingUIController.instance.SetNextStepButtonVisibility(false);
            NavigationController.instance.SetPOIForNavigation(currentTraining.GetTrainingSequenceSteps()[currentSequenceStepIndex].location);
        }
        else
        {
            ToastManager.Instance.ShowAlert("Training completed!");
            TrainingUIController.instance.CloseTaskBoard();
        }
    }

    public void TaskStateToggled()
    {
        currentTraining.GetTrainingSequenceSteps()[currentSequenceStepIndex].CheckForStepCompletion();
    }
}
