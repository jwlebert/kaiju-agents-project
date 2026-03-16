using KaijuSolutions.Agents.Utility;
using UnityEngine;

[CreateAssetMenu(fileName = "PatrolAction", menuName = "Scriptable Objects/PatrolAction")]
public class PatrolAction : KaijuUtilityAction
{
    [Tooltip("Key for center of patrol area")]
    [SerializeField]
    private string patrolCenterKey = "FriendlyBase";

    public override void Enter(KaijuUtilityBrain brain)
    {
        Component patrolCenter = brain.Get<Component>(patrolCenterKey);
        if (patrolCenter)
        {
            // Go to area, then wander to loop around it
            brain.Agent.Seek(patrolCenter, clear: true);
            brain.Agent.Wander(clear: false);
        }
    }
}
