using UnityEngine;

public class TrainingSequence : MonoBehaviour
{
    // title of this sequence
    public string sequenceTitle;

    // description of this sequence
    public string sequenceDescription;

    // steps of this sequence
    private TrainingSequenceStep[] steps;

    void Awake()
    {
        steps = GetComponentsInChildren<TrainingSequenceStep>();
    }

    public TrainingSequenceStep[] GetTrainingSequenceSteps()
    {
        return steps;
    }
}
