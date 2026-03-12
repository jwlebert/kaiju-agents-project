using JetBrains.Annotations;
using KaijuSolutions.Agents.Utility;
using UnityEngine;

[CreateAssetMenu(fileName = "FlankAction", menuName = "Scriptable Objects/FlankAction")]
public class FlankAction : KaijuUtilityAction
{
    [SerializeField] private string targetKey = "TargetEnemy";

    public override void Enter([NotNull] KaijuUtilityBrain brain)
    {
        Component enemy = brain.Get<Component>(targetKey);
        if (enemy)
        {
            // TODO: whatever flank does
        }
    }
}
