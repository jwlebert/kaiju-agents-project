using JetBrains.Annotations;
using UnityEngine;
using KaijuSolutions.Agents.Utility;

[CreateAssetMenu(fileName = "Wander Action", menuName = "Scriptable Objects/Assn3/Wander Action")]
public class WanderAction : KaijuUtilityAction
{
    public override void Enter([NotNull] KaijuUtilityBrain brain)
    {
        // Idle
        brain.Agent.Wander(15f, 1f);
        brain.Agent.ObstacleAvoidance(avoidance: 1f, clear: false);
    }
}