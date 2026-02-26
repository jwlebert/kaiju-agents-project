using System;
using System.Linq;
using KaijuSolutions.Agents.Extensions;
using KaijuSolutions.Agents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;

// Defines the possible behaviours for microbes
public enum MicrobeState { Wandering, Foraging, Hunting, Fleeing, Mating }

namespace KaijuSolutions.Agents.Exercises.Microbes
{
    /// <summary>
    /// Basic controller for you to get started with.
    /// </summary>
    [RequireComponent(typeof(Microbe))]
    [HelpURL("https://agents.kaijusolutions.ca/manual/microbes.html#microbe-controller")]
    [AddComponentMenu("Kaiju Solutions/Agents/Exercises/Microbes/Microbe Controller", 18)]
    public class MicrobeController : KaijuController
    {
        /// <summary>
        /// The <see cref="Microbe"/> this is controlling.
        /// </summary>
        [Tooltip("The microbe this is controlling.")]
        [HideInInspector]
        [SerializeField]
        private Microbe microbe;

        [SerializeField] private MicrobeState state;
        private FiniteStateMachine fsm;
        private const float MaxEnergy = 500;

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        protected override void OnEnable()
        {
            if (microbe == null)
            {
                microbe = GetComponent<Microbe>();
                if (microbe == null)
                {
                    Debug.LogError("Microbe Controller - No microbe on this GameObject.", this);
                }
            }
            
            if (microbe != null)
            {
                microbe.OnMate += OnMate;
                microbe.OnEat += OnEat;
                microbe.OnEaten += OnEaten;
            }
            
            base.OnEnable();
            
            // Initialize the state machine
            this.state = MicrobeState.Wandering;
            this.fsm = this.gameObject.AddComponent<FiniteStateMachine>();
        }
        
        protected void Update()
        {
            // Clamp energy at maxEnergy (ensures don't scale crazily).
            this.microbe.Energy = Math.Clamp(this.microbe.Energy, 0, MaxEnergy);

            // If not doing anything, reset it to Wandering.
            if (this.Agent.Movements.Count == 0)
            {
                this.state = MicrobeState.Wandering;
            }
            
            // After fleeing, if we are far enough from a hunter, go back to wandering
            if (
                this.state == MicrobeState.Fleeing &&
                Vector2.Distance(this.Agent.Position, this.fsm.hunter.Position) > 12f)
            {
                this.fsm.hunter = null;
                this.state = MicrobeState.Wandering;
            }
            
            // Scale the microbe's size based on it's energy
            float t = Mathf.InverseLerp(0f, MaxEnergy, this.microbe.Energy);
            float energyScale = Mathf.Lerp(0.75f, 1.25f, t);
            this.transform.localScale = Vector3.one * energyScale;
            
            // Tell the FSM to perform the logic for the current state
            fsm.Step(state);
        }

        /// <summary>
        /// Called after the <see cref="microbe"/> has mated.
        /// </summary>
        /// <param name="mate">The <see cref="Microbe"/> this mated with.</param>
        private void OnMate(Microbe mate)
        {
            // Reduce energy after mating.
            this.microbe.Energy -= 15;
            
            // Go back to wandering.
            this.fsm.mate = null;
            this.state = MicrobeState.Wandering;
        }

        /// <summary>
        /// Called after the <see cref="microbe"/> has eaten.
        /// </summary>
        /// <param name="ate">The <see cref="Microbe"/> this ate.</param>
        private void OnEat(Microbe ate)
        {
            // Go back to wandering.
            this.fsm.prey = null;
            this.state = MicrobeState.Wandering;
        }

        /// <summary>
        /// Called after the <see cref="microbe"/> has been eaten.
        /// </summary>
        /// <param name="eater">The <see cref="Microbe"/> which ate this.</param>
        private void OnEaten(Microbe eater)
        {
            // Handled by the system
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called when a <see cref="MicrobeVisionSensor"/> has been run.
        /// Logic for when the microbe sees other microbes
        /// </summary>
        /// <param name="microbeSensor">The <see cref="MicrobeVisionSensor"/> which was run.</param>
        private void OnMicrobeSensor(MicrobeVisionSensor microbeSensor)
        {
            // Identify potential prey (microbes of a different species)
            Microbe[] enemies = microbeSensor.Observed.Where(m => this.microbe.Eatable(m)).ToArray();
            if (enemies.Length > 0)
            {
                // Get the prey with the highest energy in vision cone. (instead of nearest to be more life like)
                Microbe strongest = enemies.OrderByDescending(m => m.Energy).First();
                
                // Decide if the strongest is the prey or if we are the prey.
                bool strongestIsPrey = this.microbe.Energy > strongest.GetComponent<Microbe>().Energy;

                // If strongest in vision is prey, we hunt.
                if (strongestIsPrey)
                {
                    // If we are not currently wandering, continue what we are doing. 
                    // (Don't interrupt mating/fleeing/foraging)
                    if (this.state != MicrobeState.Wandering) return;
                    
                    // Must have enough base energy to hunt
                    if (this.microbe.Energy <= 125) return;

                    // Switch state to hunting and invoke the hunting function.
                    this.fsm.prey = strongest;
                    this.state = MicrobeState.Hunting;
                }
                // If there is an enemy stronger than us in vision, we flee.
                else
                {
                    // Switch state to fleeing and invoke the fleeing function.(overrides wandering)
                    this.fsm.hunter = strongest;
                    this.state = MicrobeState.Fleeing;
                }

                return;
            }

            // If we are on cooldown, return; we aren't interested in mating.
            if (this.microbe.OnCooldown) return;
            
            // Get microbes in our vision cone of same species which are compatible to mate (not on cooldown, etc).
            Microbe[] potentialMates = microbeSensor.Observed.Where(
                m => this.microbe.Compatible(m) && !m.OnCooldown).ToArray();

            if (potentialMates.Length > 0)
            {
                // Only mate if high enough energy.
                if (this.microbe.Energy <= 125) return;
                
                // Only start mating if current state is wandering.
                // If not, current action is higher priority.
                if (this.state != MicrobeState.Wandering && this.state != MicrobeState.Mating) return;

                // Select the strongest mate to ensure strong offspring
                Microbe mate = potentialMates.OrderByDescending(m => m.Energy).First(); 

                // Switch to mating state and seek to our mate.
                this.fsm.mate = mate;
                this.state = MicrobeState.Mating;
            }
        }

        /// <summary>
        /// Called when a <see cref="EnergyVisionSensor"/> has been run.
        /// Logic for when a microbe sees energy/food sources (static)
        /// </summary>
        /// <param name="energySensor">The <see cref="EnergyVisionSensor"/> which was run.</param>
        private void OnEnergySensor(EnergyVisionSensor energySensor)
        {
            // Only eat if in need of energy.
            if (this.microbe.Energy > 350) return;
            
            // Only start eating if current state is wandering.
            // If not, current action is higher priority.
            if (this.state != MicrobeState.Wandering) return;
            
            // If we see an energy, seek to eat the energy.
            if (energySensor.Observed.Count > 0)
            {
                // Seek to closest energy source
                Transform nearest = Position.Nearest(energySensor.Observed, out float _);
                this.fsm.energyPos = nearest;
                this.state = MicrobeState.Foraging;
                
                // NOTE: No dedicated callback for eating static energy (unlike OnEat for microbes)
                // The transition back to wandering is handled in Update() by switching to wandering
                // when the Agent reaches the energy and it's movement is stopped.
                // TODO: Maybe move logic to an OnStop callback
            }
        }

        /// <summary>
        /// Callback for when a <see cref="KaijuSensor"/> has been run.
        /// </summary>
        /// <param name="sensor">The <see cref="KaijuSensor"/>.</param>
        protected override void OnSense(KaijuSensor sensor)
        {
            // Run methods for either of the sensors.
            if (sensor is MicrobeVisionSensor microbeSensor)
            {
                OnMicrobeSensor(microbeSensor);
            }
            else if (sensor is EnergyVisionSensor energySensor)
            {
                OnEnergySensor(energySensor);
            }
        }

        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            
            // The microbe must on this object.
            if (microbe == null || microbe.transform != transform)
            {
                microbe = GetComponent<Microbe>();
            }
        }
        
        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            
            if (microbe == null)
            {
                return;
            }
            
            microbe.OnMate -= OnMate;
            microbe.OnEat -= OnEat;
            microbe.OnEaten -= OnEaten;
        }
    }
}