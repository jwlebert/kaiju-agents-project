using JetBrains.Annotations;
using UnityEngine;
using KaijuSolutions.Agents.Utility;
using JetBrains.Annotations;
using KaijuSolutions.Agents.Exercises.CTF;
using KaijuSolutions.Agents.Extensions;

[CreateAssetMenu(fileName = "Shoot Action", menuName = "Scriptable Objects/Assn3/Shoot Action")]
public class ShootAction : KaijuUtilityAction
{
    [Tooltip("Name of key with target enemy")]
    [SerializeField]
    private string targetKey;

    public override void Enter([NotNull] KaijuUtilityBrain brain)
    {
        // Get enemy from brain's blackboard
        Component enemy = brain.Get<Component>(targetKey);
        if (enemy)
        {
            // Shoot Blaster
            brain.Agent.LookTransform = enemy.transform;
            BlasterActuator actuator = brain.Agent.GetActuator<BlasterActuator>();
            actuator.Begin();
        }
    }
}
