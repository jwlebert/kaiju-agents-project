using System;
using System.Linq;
using KaijuSolutions.Agents.Extensions;
using KaijuSolutions.Agents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;

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

        /// <summary>
        /// Start wandering, to find food, mates, or prey.
        /// </summary>
        private void StartWandering()
        {
            // Switch state to wandering.
            this.state = MicrobeState.Wandering;
            
            // Look where we move.
            Agent.LookTransform = null;

            Agent.Wander();

            // Avoid obstacles, without clearing the Wander instruction.
            Agent.ObstacleAvoidance(clear: false);
        }

        /// <summary>
        /// Seek energy to consume.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void SeekFood(Transform food)
        {
            // Go to the food with Seek.
            Agent.Seek(food, 0.1f);

            // Look at food as we move to it.
            Agent.LookTransform = food;
        }

        /// <summary>
        /// Seek same species microbe to mate.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void SeekMate(Microbe mate)
        {
            // Get position of mate.
            Transform pos = mate.transform;
            
            // Go to the food with Seek.
            Agent.Seek(pos, 0.1f);

            // Look at food as we move to it.
            Agent.LookTransform = pos;
        }

        /// <summary>
        /// Hunt microbe of different species using Pursue.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void HuntEnemy(Microbe prey)
        {
            Transform pos = prey.transform;

            Agent.Pursue(pos, distance: 0.1f);
            Agent.LookTransform = pos;
        }

        /// <summary>
        /// Flee from microbe of different species using Evade.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void FleeHunter(Microbe hunter)
        {
            // DEBUG - Temporarily skip fleeing to implement other behaviours
            this.state = MicrobeState.Wandering;
            StartWandering();
            return;
            
            Transform pos = hunter.transform;
            
            Agent.Flee(pos, 0.1f);
            Agent.LookTransform = pos;

            // Todo - when does it stop?
            // this.microbe.Agent.GetSensor<MicrobeVisionSensor>()
        }

        /// <summary>
        /// Called after the <see cref="microbe"/> has mated.
        /// </summary>
        /// <param name="mate">The <see cref="Microbe"/> this mated with.</param>
        private void OnMate(Microbe mate)
        {
            // Go back to wandering.
            this.state = MicrobeState.Wandering;
            StartWandering();
        }

        /// <summary>
        /// Called after the <see cref="microbe"/> has eaten.
        /// </summary>
        /// <param name="ate">The <see cref="Microbe"/> this ate.</param>
        private void OnEat(Microbe ate)
        {
            // Go back to wandering.
            this.state = MicrobeState.Wandering;
            StartWandering();
        }

        /// <summary>
        /// Called after the <see cref="microbe"/> has been eaten.
        /// </summary>
        /// <param name="eater">The <see cref="Microbe"/> which ate this.</param>
        private void OnEaten(Microbe eater)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called when a <see cref="MicrobeVisionSensor"/> has been run.
        /// </summary>
        /// <param name="microbeSensor">The <see cref="MicrobeVisionSensor"/> which was run.</param>
        private void OnMicrobeSensor(MicrobeVisionSensor microbeSensor)
        {
            // Get microbes in our vision cone of different species.
            Microbe[] enemies = microbeSensor.Observed.Where(m => this.microbe.Eatable(m)).ToArray();
            if (enemies.Length > 0)
            {
                // Get the strongest enemy in vision cone.
                Microbe strongest = enemies.OrderByDescending(m => m.Energy).First();
                
                // Decide if the strongest is the prey or if we are the prey.
                bool strongestIsPrey = this.microbe.Energy > strongest.GetComponent<Microbe>().Energy;
                // Transform nearest = Position.Nearest(enemies, out float _); // TODO - get highest (flee)

                // If strongest in vision is prey, we hunt.
                if (strongestIsPrey)
                {
                    // If we are not currently wandering or hunting, continue what we are doing. 
                    if (this.state != MicrobeState.Wandering && this.state != MicrobeState.Hunting) return;

                    // Switch state to hunting and invoke the hunting function.
                    this.state = MicrobeState.Hunting;
                    HuntEnemy(strongest); 
                }
                // If there is an enemy stronger than us in vision, we flee.
                else
                {
                    // Switch state to fleeing and invoke the fleeing function.
                    this.state = MicrobeState.Fleeing;
                    FleeHunter(strongest);
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
                // Only mate if energy surplus (above 80).
                if (this.microbe.Energy <= 80) return;
                
                // Only start mating if current state is wandering.
                // If not, current action is higher priority.
                if (this.state != MicrobeState.Wandering) return;

                // Select the strongest mate.
                Microbe mate = potentialMates.OrderByDescending(m => m.Energy).First();
                
                // Switch to mating state and seek to our mate.
                this.state = MicrobeState.Mating;
                SeekMate(mate);
            }
        }

        /// <summary>
        /// Called when a <see cref="EnergyVisionSensor"/> has been run.
        /// </summary>
        /// <param name="energySensor">The <see cref="EnergyVisionSensor"/> which was run.</param>
        private void OnEnergySensor(EnergyVisionSensor energySensor)
        {
            // Only eat if in need of energy (below or at 120).
            if (this.microbe.Energy > 120) return;
            
            // Only start eating if current state is wandering.
            // If not, current action is higher priority.
            if (this.state != MicrobeState.Wandering) return;

            // If we see an energy, seek to eat the energy.
            if (energySensor.Observed.Count > 0)
            {
                Transform nearest = Position.Nearest(energySensor.Observed, out float _);
                SeekFood(nearest);
            }

            // Return to wandering.
            this.state = MicrobeState.Wandering;
            StartWandering();
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
            
            this.state = MicrobeState.Wandering;
            StartWandering();
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