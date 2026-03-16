using KaijuSolutions.Agents.Exercises.Microbes;
using KaijuSolutions.Agents.Extensions;
using UnityEngine;

namespace KaijuSolutions.Agents.Utility.Exercises.Microbes
{
    /// <summary>
    /// Brain to control a <see cref="microbe"/>.
    /// </summary>
#if UNITY_EDITOR
    [SelectionBase]
    [Icon("Packages/com.kaijusolutions.agents.utility/Editor/Icon.png")]
    [HelpURL("https://utility.kaijusolutions.ca")]
#endif
    [DisallowMultipleComponent]
    [RequireComponent(typeof(KaijuAgent))]
    public class MicrobeBrain : KaijuUtilityBrain
    {
        /// <summary>
        /// The microbe this is for.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("The microbe this is for.")]
        [HideInInspector]
#endif
        [SerializeField]
        private Microbe microbe;
        
        /// <summary>
        /// The microbe vision sensor from the <see cref="microbe"/>.
        /// </summary>
        private MicrobeVisionSensor _microbeSensor;
        
        /// <summary>
        /// The energy vision sensor from the <see cref="microbe"/>.
        /// </summary>
        private EnergyVisionSensor _energySensor;
        
        /// <summary>
        /// Awake is called when an enabled script instance is being loaded.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            GetMicrobe();
        }
        
        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        public override void OnEnable()
        {
            base.OnEnable();
            _microbeSensor = Agent.GetSensor<MicrobeVisionSensor>();
            _energySensor = Agent.GetSensor<EnergyVisionSensor>();
        }
#if UNITY_EDITOR
        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            GetMicrobe();
        }
#endif
        /// <summary>
        /// Get the <see cref="microbe"/>.
        /// </summary>
        private void GetMicrobe()
        {
            if (microbe == null || microbe.transform != transform)
            {
                microbe = GetComponent<Microbe>();
            }
        }
        
        /// <summary>
        /// Set any needed blackboard variables for choosing an action to perform.
        /// </summary>
        protected override void UpdateBlackboard()
        {
            // Store if we are able to mate ourselves.
            Set("CanMate", microbe.CanMate);
            
            // Store our energy level, converting it to a float in [0, 1] based on the maximum energy value.
            SetScaled("Energy", microbe, 0, MicrobeManager.MaximumEnergy);
            
            // Set observations.
            Set("Pickup", _energySensor.Nearest(out float nearest, true));
            Set("PickupDistance", nearest);
            
            // Reset old memory values.
            Vector2 position = Position;
            PotentialMate(position);
            PotentialPrey(position);
            PotentialPredator(position);
        }
        
        /// <summary>
        /// Set the variables for a potential mate.
        /// </summary>
        /// <param name="position">The current position of this <see cref="microbe"/>.</param>
        private void PotentialMate(Vector2 position)
        {
            Microbe mate = null;
            float best = float.MaxValue;
            bool canMate = false;
            foreach (Microbe other in _microbeSensor.Observed)
            {
                if (!microbe.Compatible(other))
                {
                    continue;
                }
                
                bool otherCanMate = other.CanMate;
                if (canMate && !otherCanMate)
                {
                    continue;
                }
                
                float distance = position.Distance(other.Position);
                if (canMate && distance >= best)
                {
                    continue;
                }
                
                mate = other;
                best = distance;
                canMate = otherCanMate;
            }
            
            Set("Mate", mate);
            Set("MateDistance", best);
            Set("HasMate", mate != null);
        }
        
        /// <summary>
        /// Set the variables for a potential prey.
        /// </summary>
        /// <param name="position">The current position of this <see cref="microbe"/>.</param>
        private void PotentialPrey(Vector2 position)
        {
            Microbe prey = null;
            float best = float.MaxValue;
            
            foreach (Microbe other in _microbeSensor.Observed)
            {
                if (!microbe.Eatable(other) || other.Energy >= microbe.Energy)
                {
                    continue;
                }
                
                float distance = position.Distance(other.Position);
                if (distance >= best)
                {
                    continue;
                }
                
                prey = other;
                best = distance;
            }
            
            Set("Prey", prey);
            Set("PreyDistance", best);
            Set("HasPrey", prey != null);
        }
        
        /// <summary>
        /// Set the variables for the worst predator threat.
        /// </summary>
        /// <param name="position">The current position of this <see cref="microbe"/>.</param>
        private void PotentialPredator(Vector2 position)
        {
            Microbe prey = null;
            float worse = float.MaxValue;
            
            foreach (Microbe other in _microbeSensor.Observed)
            {
                if (!microbe.Eatable(other) || other.Energy <= microbe.Energy)
                {
                    continue;
                }
                
                float distance = position.Distance(other.Position);
                if (distance >= worse)
                {
                    continue;
                }
                
                prey = other;
                worse = distance;
            }
            
            Set("Predator", prey);
            Set("PredatorDistance", worse);
            Set("HasPredator", prey != null);
        }
    }
}