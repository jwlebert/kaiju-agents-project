using JetBrains.Annotations;
using UnityEngine;
using KaijuSolutions.Agents.Utility;

[CreateAssetMenu(fileName = "action", menuName = "Scriptable Objects/action")]
public class NavigateAction : KaijuUtilityAction
{
    [Tooltip("The name of the key for the target")]
    [SerializeField] private string targetKey;

    public override void Enter([NotNull] KaijuUtilityBrain brain)
    {
        // Get the target from brain's blackboard
        Component target = brain.Get<Component>(targetKey);
        if (target)
        {
            // Seek for now, TODO update with pathfinding
            brain.Agent.PathFollow(target, clear: true);
        }
    }
}
