using System;
using System.Linq;
using KaijuSolutions.Agents.Extensions;
using KaijuSolutions.Agents.Sensors;
using UnityEngine;

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

        [SerializeField] private MicrobeState _state;

        /// <summary>
        /// Start wandering, to find food, mates, or prey.
        /// </summary>
        private void StartWandering()
        {
            // Switch state to wandering.
            this._state = MicrobeState.Wandering;
            
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
            // Switch state to foraging.
            this._state = MicrobeState.Foraging;
            
            throw new NotImplementedException();   
        }

        /// <summary>
        /// Seek same species microbe to mate.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void SeekMate(Transform mate)
        {
            // Switch state to mating.
            this._state = MicrobeState.Mating;
            
            throw new NotImplementedException();
        }

        /// <summary>
        /// Hunt microbe of different species using Pursue.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void HuntEnemy(Transform prey)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Flee from microbe of different species using Evade.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void FleeHunter(Transform hunter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called after the <see cref="microbe"/> has mated.
        /// </summary>
        /// <param name="mate">The <see cref="Microbe"/> this mated with.</param>
        private void OnMate(Microbe mate)
        {
            // Mating functionality - TODO? might be done
            throw new NotImplementedException();

            // Go back to wandering.
            this._state = MicrobeState.Wandering;
            StartWandering();
        }

        /// <summary>
        /// Called after the <see cref="microbe"/> has eaten.
        /// </summary>
        /// <param name="ate">The <see cref="Microbe"/> this ate.</param>
        private void OnEat(Microbe ate)
        {
            // Eating functionality - TODO? might be done
            throw new NotImplementedException();

            // Go back to wandering.
            this._state = MicrobeState.Wandering;
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
            Microbe[] enemies = microbeSensor.Observed.Where(m => this.microbe.Eatable(m)).ToArray();
            bool enemyPresent = enemies.Length > 0;

            if (enemyPresent)
            {
                Transform nearest = Position.Nearest(enemies, out float _);
                
                // Flee if energy is lesser. - TODO
                FleeHunter(nearest);
                
                // Hunt if energy is greater - TODO
                HuntEnemy(nearest);
            }
            
            if (!enemyPresent)
            {
                // Only mate if energy surplus (above 80).
                if (this.microbe.Energy <= 80) return;
                
                // Only start mating if current state is wandering.
                // If not, current action is higher priority.
                if (this._state != MicrobeState.Wandering) return;

                // If conditions are satisfied, seek to mate so we can mate.
                Transform nearest = Position.Nearest(microbeSensor.Observed, out float _);
                SeekMate(nearest);
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
            if (this._state != MicrobeState.Wandering) return;

            // If conditions are satisfied, seek to eat the energy.
            Transform nearest = Position.Nearest(energySensor.Observed, out float _);
            SeekFood(nearest);
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
            
            this._state = MicrobeState.Wandering;
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