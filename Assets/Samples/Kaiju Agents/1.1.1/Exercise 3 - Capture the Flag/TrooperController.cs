using System.Collections.Generic;
using System.Linq;
using KaijuSolutions.Agents.Extensions;
using KaijuSolutions.Agents.Sensors;
using KaijuSolutions.Agents.Utility;
using UnityEngine;

namespace KaijuSolutions.Agents.Exercises.CTF
{
    /// <summary>
    /// Basic controller for you to get started with.
    /// </summary>
    [RequireComponent(typeof(Trooper))]
    [AddComponentMenu("Kaiju Solutions/Agents/Exercises/Capture the Flag/Trooper Controller", 24)]
    [HelpURL("https://agents.kaijusolutions.ca/manual/capture-the-flag.html#trooper-controller")]
    public class TrooperController : KaijuController
    {
        /// <summary>
        /// The <see cref="Trooper"/> this is controlling.
        /// </summary>
        [Tooltip("The trooper this is controlling.")]
        [HideInInspector]
        [SerializeField] private Trooper trooper;
        
        /// <summary>
        /// The <see cref="KaijuUtilityBrain"/> for this trooper.
        /// </summary>
        [Tooltip("The KaijuUtilityBrain for this trooper.")]
        [HideInInspector]
        [SerializeField] private TrooperBrain brain;
        
        /// <summary>
        /// Callback for this <see cref="trooper"/> hitting another <see cref="Trooper"/>.
        /// </summary>
        /// <param name="hit">The <see cref="Trooper"/> which was hit.</param>
        private void OnHitTrooper(Trooper hit) { }
        
        /// <summary>
        /// Callback for this <see cref="trooper"/> getting hit by another <see cref="Trooper"/>.
        /// </summary>
        /// <param name="hitBy">The <see cref="Trooper"/> which hit the <see cref="trooper"/>.</param>
        private void OnHitByTrooper(Trooper hitBy) { }
        
        /// <summary>
        /// Callback for this <see cref="trooper"/> eliminating another <see cref="Trooper"/>.
        /// </summary>
        /// <param name="eliminated">The <see cref="Trooper"/> which was eliminated.</param>
        private void OnEliminatedTrooper(Trooper eliminated) { }
        
        /// <summary>
        /// Callback for this <see cref="trooper"/> getting eliminated by another <see cref="Trooper"/>.
        /// </summary>
        /// <param name="eliminatedBy">The <see cref="Trooper"/> which eliminated the <see cref="trooper"/>.</param>
        private void OnEliminatedByTrooper(Trooper eliminatedBy) { }
        
        /// <summary>
        /// Callback for the <see cref="trooper"/> picking up the <see cref="Flag"/>.
        /// </summary>
        /// <param name="flag">The <see cref="Flag"/>.</param>
        private void OnFlagPickedUp(Flag flag) { }
        
        /// <summary>
        /// Callback for the <see cref="trooper"/> capturing the <see cref="Flag"/>.
        /// </summary>
        /// <param name="flag">The <see cref="Flag"/>.</param>
        private void OnFlagCaptured(Flag flag) { }
        
        /// <summary>
        /// Callback for the <see cref="trooper"/> returning their <see cref="Flag"/>.
        /// </summary>
        /// <param name="flag">The <see cref="Flag"/>.</param>
        private void OnFlagReturned(Flag flag) { }
        
        /// <summary>
        /// Callback for the <see cref="trooper"/> dropping the <see cref="Flag"/>.
        /// </summary>
        /// <param name="flag">The <see cref="Flag"/>.</param>
        private void OnFlagDropped(Flag flag) { }
        
        /// <summary>
        /// Callback for sensing enemies.
        /// </summary>
        /// <param name="sensor">The <see cref="TrooperEnemyVisionSensor"/>.</param>
        private void OnSenseEnemies(TrooperEnemyVisionSensor sensor)
        {
            if (!sensor || !sensor.Observed.Any()) return;

            IEnumerable<Trooper> enemies = sensor.Observed
                .Append(brain.nearestEnemy)
                .Where(t => t);

            brain.nearestEnemy = Position.Nearest(enemies, out float _);
        }
        
        /// <summary>
        /// Callback for sensing teammates.
        /// </summary>
        /// <param name="sensor">The <see cref="TrooperTeamVisionSensor"/>.</param>
        private void OnSenseTeam(TrooperTeamVisionSensor sensor)
        {
            // no actions require team sensing?
        }
        
        /// <summary>
        /// Callback for sensing all <see cref="Trooper"/>s.
        /// </summary>
        /// <param name="sensor">The <see cref="TrooperTeamVisionSensor"/>.</param>
        private void OnSenseTroopers(TrooperVisionSensor sensor)
        {
            // SensorDebug(sensor, "Troopers");
        }

        /// <summary>
        /// Callback for sensing <see cref="AmmoPickup"/>s.
        /// </summary>
        /// <param name="sensor">The <see cref="AmmoVisionSensor"/>.</param>
        private void OnSenseAmmo(AmmoVisionSensor sensor)
        {
            // Safety check for null sensor or empty detection list.
            if (!sensor || !sensor.Observed.Any()) 
            {
                return;
            }

            // Combine current visible pickups with the one we are already tracking.
            // .Where(p => p) uses Unity's implicit null check to filter out destroyed objects.
            IEnumerable<AmmoPickup> pickups = sensor.Observed
                .Append(brain.ammoPickup)
                .Where(p => p);
    
            // Update the brain with the nearest eligible ammo pickup.
            brain.ammoPickup = Position.Nearest(pickups, out float _);
        }

        /// <summary>
        /// Callback for sensing <see cref="HealthPickup"/>s.
        /// </summary>
        /// <param name="sensor">The <see cref="HealthVisionSensor"/>.</param>
        private void OnSenseHealth(HealthVisionSensor sensor)
        {
            // Safety check for null sensor or empty detection list.
            if (!sensor || !sensor.Observed.Any()) 
            {
                return;
            }

            // Combine visible pickups with the currently tracked health pickup.
            IEnumerable<HealthPickup> pickups = sensor.Observed
                .Append(brain.healthPickup)
                .Where(p => p);
    
            // Update the brain with the nearest eligible health pickup.
            brain.healthPickup = Position.Nearest(pickups, out float _);
        }
        
        /// <summary>
        /// Callback for sensing <see cref="Flag"/>s.
        /// </summary>
        /// <param name="sensor">The <see cref="FlagVisionSensor"/>.</param>
        private void OnSenseFlag(FlagVisionSensor sensor)
        {
            // Safety check for null sensor or empty detection list.
            if (!sensor || !sensor.Observed.Any()) 
            {
                return;
            }

            foreach (var flag in sensor.Observed)
            {
                if (flag.TeamOne == trooper.TeamOne)
                    brain.friendlyFlag = flag.transform;
                else
                    brain.enemyFlag = flag.transform;
            }
        }
        
        /// <summary>
        /// Callback for when a <see cref="KaijuSensor"/> has been run.
        /// </summary>
        /// <param name="sensor">The <see cref="KaijuSensor"/>.</param>
        protected override void OnSense(KaijuSensor sensor)
        {
            if (sensor is TrooperVisionSensor troopers)
            {
                if (troopers is TrooperEnemyVisionSensor enemies)
                {
                    OnSenseEnemies(enemies);
                    return;
                }
                
                if (troopers is TrooperTeamVisionSensor team)
                {
                    OnSenseTeam(team);
                    return;
                }
                
                OnSenseTroopers(troopers);
                return;
            }

            if (sensor is FlagVisionSensor flag)
            {
                OnSenseFlag(flag);
                return;
            }
            
            if (sensor is AmmoVisionSensor ammo)
            {
                OnSenseAmmo(ammo);
                return;
            }
            
            if (sensor is HealthVisionSensor health)
            {
                OnSenseHealth(health);
            }
        }

        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            
            // The trooper must on this object.
            if (trooper == null || trooper.transform != transform)
            {
                trooper = GetComponent<Trooper>();
            }
        }
        
        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        protected override void OnEnable()
        {
            if (trooper == null)
            {
                trooper = GetComponent<Trooper>();
                if (trooper == null)
                {
                    Debug.LogError("Trooper Controller - No trooper on this GameObject.", this);
                }
            }
            
            // Get the KaijuUtilityBrain for this trooper.
            if (brain == null)
            {
                brain = GetComponent<TrooperBrain>();
                if (brain == null)
                {
                    Debug.LogError("Trooper Controller - No trooperBrain on this GameObject.", this);
                }
            }
            
            if (trooper != null)
            {
                trooper.OnHitTrooper += OnHitTrooper;
                trooper.OnHitByTrooper += OnHitByTrooper;
                trooper.OnEliminatedTrooper += OnEliminatedTrooper;
                trooper.OnEliminatedByTrooper += OnEliminatedByTrooper;
                trooper.OnFlagPickedUp += OnFlagPickedUp;
                trooper.OnFlagCaptured += OnFlagCaptured;
                trooper.OnFlagReturned += OnFlagReturned;
                trooper.OnFlagDropped += OnFlagDropped;
            }
            
            base.OnEnable();
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            
            if (trooper == null)
            {
                return;
            }
            
            trooper.OnHitTrooper -= OnHitTrooper;
            trooper.OnHitByTrooper -= OnHitByTrooper;
            trooper.OnEliminatedTrooper -= OnEliminatedTrooper;
            trooper.OnEliminatedByTrooper -= OnEliminatedByTrooper;
            trooper.OnFlagPickedUp -= OnFlagPickedUp;
            trooper.OnFlagCaptured -= OnFlagCaptured;
            trooper.OnFlagReturned -= OnFlagReturned;
            trooper.OnFlagDropped -= OnFlagDropped;
        }
    }
}