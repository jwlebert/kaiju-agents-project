using JetBrains.Annotations;
using KaijuSolutions.Agents.Utility;
using UnityEngine;

[CreateAssetMenu(fileName = "FlankAction", menuName = "Scriptable Objects/FlankAction")]
public class FlankAction : KaijuUtilityAction
{
    [SerializeField] private string targetKey = "TargetEnemy";
    [Tooltip("How far to the side of the enemy to flank.")]
    [SerializeField] private float sideOffset = 4f;
    
    public override void Enter([NotNull] KaijuUtilityBrain brain)
    {
        Component enemy = brain.Get<Component>(targetKey);
        if (enemy)
        {
            // Calculate flank position based on enemy's current orientation
            Vector3 enemyPos = enemy.transform.position;
            Vector3 enemyRight = enemy.transform.right;
            Vector3 enemyBack = -enemy.transform.forward;
            
            // Target position should be to the side and backwards
            Vector3 flankPos = enemyPos + (enemyRight * sideOffset) + (enemyBack * 2f);
            
            // Move to flank position
            brain.Agent.PathFollow(flankPos, clear: true);
            
            // Keep agent looking at enemy
            brain.Agent.LookTransform = enemy.transform;
        }
    }
}
