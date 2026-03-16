
using System.Diagnostics.CodeAnalysis;
#if UNITY_EDITOR
using UnityEngine;
#endif
namespace KaijuSolutions.Agents.Utility.Exercises.Microbes
{
    /// <summary>
    /// Action for a microbe to wander around.
    /// </summary>
#if UNITY_EDITOR
    [Icon("Packages/com.kaijusolutions.agents.utility/Editor/Icon.png")]
    [HelpURL("https://utility.kaijusolutions.ca")]
    [CreateAssetMenu(menuName = "Kaiju Solutions/Agents/Utility/Exercises/Microbes/Actions/Wander", fileName = "Wander Action", order = 200)]
#endif
    public class WanderAction : KaijuUtilityAction
    {
        /// <summary>
        /// Called when this action is run for the first time.
        /// </summary>
        /// <param name="brain">The <see cref="KaijuUtilityBrain"/> this is for.</param>
        public override void Enter([NotNull] KaijuUtilityBrain brain)
        {
            brain.Agent.Wander(clear: true);
            brain.Agent.ObstacleAvoidance(clear: false);
            brain.Agent.Separation(clear: false);
        }
    }
}
