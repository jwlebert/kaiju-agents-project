using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace KaijuSolutions.Agents.Utility.Exercises.Microbes
{
    /// <summary>
    /// Action for a microbe to seek to a component.
    /// </summary>
#if UNITY_EDITOR
    [Icon("Packages/com.kaijusolutions.agents.utility/Editor/Icon.png")]
    [HelpURL("https://utility.kaijusolutions.ca")]
    [CreateAssetMenu(menuName = "Kaiju Solutions/Agents/Utility/Exercises/Microbes/Actions/Seek", fileName = "Seek Action", order = 202)]
#endif
    public class SeekAction : KaijuUtilityAction
    {
        /// <summary>
        /// The name of the key to seek to.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("The name of the key to seek to.")]
#endif
        [SerializeField]
        private string target;
        
        /// <summary>
        /// Called when this action is run for the first time.
        /// </summary>
        /// <param name="brain">The <see cref="KaijuUtilityBrain"/> this is for.</param>
        public override void Enter([NotNull] KaijuUtilityBrain brain)
        {
            Component c = brain.Get<Component>(target);
            if (c)
            {
                brain.Agent.Seek(c, clear: true);
            }
        }
    }
}