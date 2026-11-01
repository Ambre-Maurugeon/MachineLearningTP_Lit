using NaughtyAttributes;
using System.Collections.Generic;
using Unity.MLAgents.Policies;
using UnityEngine;

public class TimeScaleModifier : MonoBehaviour {
    [InfoBox("Time Scale modifier will only be active if no agent is in Default (learning) Behaviour Mode")]

    [SerializeField] float timeScale = 20f;
    [SerializeField] bool toggledOn = false;

    List<BehaviorParameters> behaviours = new List<BehaviorParameters>();

    bool HasLearningAgent => behaviours.Exists(x => x.BehaviorType == BehaviorType.Default);

    void Awake() {
        behaviours.AddRange(GameObject.FindObjectsByType<BehaviorParameters>());

        if (toggledOn && !HasLearningAgent) {
            Time.timeScale = timeScale;
        }
    }

    private void OnValidate() {
        if (!Application.isPlaying || HasLearningAgent) { return; }

        Time.timeScale = toggledOn ? timeScale : 1;
    }

}
