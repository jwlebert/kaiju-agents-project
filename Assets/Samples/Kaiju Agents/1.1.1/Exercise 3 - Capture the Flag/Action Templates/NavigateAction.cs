using JetBrains.Annotations;
using UnityEngine;
using KaijuSolutions.Agents.Utility;

[CreateAssetMenu(fileName = "Navigate Action", menuName = "Scriptable Objects/Assn3/Navigate Action")]
public class NavigateAction : KaijuUtilityAction
{
    [Tooltip("The name of the key for the target")]
    [SerializeField] private string targetKey;
    
    [Tooltip("transform is passed if true, else a component.")]
    [SerializeField] private bool transf;

    public override void Enter([NotNull] KaijuUtilityBrain brain)
    {
        brain.Agent.LookTransform = null;
        // Get the target from brain's blackboard
        if (transf)
        {
            Vector3 target = brain.Get<Vector3>(targetKey);
            if (target != Vector3.zero)
            {
                // This is only used by flag captures.
                brain.Agent.PathFollow(target, clear: true);
            }
        }
        else
        {
            Component target = brain.Get<Component>(targetKey);
            if (target)
            {
                brain.Agent.PathFollow(target, clear: true);
            }
        };
    }
}
