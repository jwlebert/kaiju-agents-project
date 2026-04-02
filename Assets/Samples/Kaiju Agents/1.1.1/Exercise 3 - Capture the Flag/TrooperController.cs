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
    [RequireComponent(typeof(TrooperOld))]
    [AddComponentMenu("Kaiju Solutions/Agents/Exercises/Capture the Flag/Trooper Controller", 24)]
    [HelpURL("https://agents.kaijusolutions.ca/manual/capture-the-flag.html#trooper-controller")]
    public class TrooperController : KaijuController
    {
        /// <summary>
        /// The <see cref="TrooperOld"/> this is controlling.
        /// </summary>
        [Tooltip("The trooper this is controlling.")]
        [HideInInspector]
        [SerializeField] private TrooperOld trooperOld;
        
        /// <summary>
        /// The <see cref="KaijuUtilityBrain"/> for this trooper.
        /// </summary>
        [Tooltip("The KaijuUtilityBrain for this trooper.")]
        [HideInInspector]
        [SerializeField] private TrooperBrain brain;

        /// <summary>
        /// Callback for this <see cref="trooperOld"/> hitting another <see cref="TrooperOld"/>.
        /// </summary>
        /// <param name="hit">The <see cref="TrooperOld"/> which was hit.</param>
        private void HitTrooperOld(TrooperOld hit)
        {
            brain.nearestEnemy = null;
        }

        /// <summary>
        /// Callback for this <see cref="trooperOld"/> getting hit by another <see cref="TrooperOld"/>.
        /// </summary>
        /// <param name="hitBy">The <see cref="TrooperOld"/> which hit the <see cref="trooperOld"/>.</param>
        private void HitByTrooperOld(TrooperOld hitBy)
        {
            brain.nearestEnemy = null;
        }

        /// <summary>
        /// Callback for this <see cref="trooperOld"/> eliminating another <see cref="TrooperOld"/>.
        /// </summary>
        /// <param name="eliminated">The <see cref="TrooperOld"/> which was eliminated.</param>
        private void EliminatedTrooperOld(TrooperOld eliminated)
        {
            // So it stops shooting after kills/dies.
            brain.nearestEnemy = null;
        }

        /// <summary>
        /// Callback for this <see cref="trooperOld"/> getting eliminated by another <see cref="TrooperOld"/>.
        /// </summary>
        /// <param name="eliminatedBy">The <see cref="TrooperOld"/> which eliminated the <see cref="trooperOld"/>.</param>
        private void EliminatedByTrooperOld(TrooperOld eliminatedBy)
        {
            // So it stops shooting after kills/dies.
            brain.nearestEnemy = null;
            brain.SetHoldingFlag(true);
        }

        /// <summary>
        /// Callback for the <see cref="trooperOld"/> picking up the <see cref="FlagOld"/>.
        /// </summary>
        /// <param name="flagOld">The <see cref="FlagOld"/>.</param>
        private void OnFlagPickedUp(FlagOld flagOld)
        {
            brain.SetHoldingFlag(true);
        }

        /// <summary>
        /// Callback for the <see cref="trooperOld"/> capturing the <see cref="FlagOld"/>.
        /// </summary>
        /// <param name="flagOld">The <see cref="FlagOld"/>.</param>
        private void OnFlagCaptured(FlagOld flagOld)
        {
            brain.SetHoldingFlag(false);
            
        }

        /// <summary>
        /// Callback for the <see cref="trooperOld"/> returning their <see cref="FlagOld"/>.
        /// </summary>
        /// <param name="flagOld">The <see cref="FlagOld"/>.</param>
        private void OnFlagReturned(FlagOld flagOld)
        {
            brain.SetHoldingFlag(false);
            
        }

        /// <summary>
        /// Callback for the <see cref="trooperOld"/> dropping the <see cref="FlagOld"/>.
        /// </summary>
        /// <param name="flagOld">The <see cref="FlagOld"/>.</param>
        private void OnFlagDropped(FlagOld flagOld)
        {
            brain.SetHoldingFlag(true);
        }
        
        /// <summary>
        /// Callback for sensing enemies.
        /// </summary>
        /// <param name="sensor">The <see cref="TrooperEnemyVisionSensor"/>.</param>
        private void OnSenseEnemies(TrooperEnemyVisionSensor sensor)
        {
            // Safety check for null sensor or empty detection list.
            if (!sensor || !sensor.Observed.Any()) return;

            // Combine visible pickups with the currently tracked ammo pickup.
            IEnumerable<TrooperOld> enemies = sensor.Observed
                .Append(brain.nearestEnemy)
                .Where(t => t);

            // Update the brain with the nearest eligible enemy trooper.
            brain.nearestEnemy = Position.Nearest(enemies, out float _);
        }
        
        /// <summary>
        /// Callback for sensing teammates.
        /// </summary>
        /// <param name="sensor">The <see cref="TrooperTeamVisionSensor"/>.</param>
        private void OnSenseTeam(TrooperTeamVisionSensor sensor)
        {
            // no actions require team sensing?
            // could possibly be used to prevent TKs?
        }
        
        /// <summary>
        /// Callback for sensing all <see cref="TrooperOld"/>s.
        /// </summary>
        /// <param name="sensor">The <see cref="TrooperTeamVisionSensor"/>.</param>
        private void OnSenseTroopers(TrooperVisionSensor sensor)
        {
            // none needed
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

            // Combine visible pickups with the currently tracked ammo pickup.
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
        /// Callback for sensing <see cref="FlagOld"/>s.
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
                // Tracks enemy/friendly flags when observed.
                // Currently unused; we decided to treat troopers as omniscient with respect to flags.
                // Just pretend they have walkie talkies.
                if (flag.TeamOne == trooperOld.TeamOne)
                    brain.friendlyFlagOld = flag;
                else
                    brain.enemyFlagOld = flag;
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
            if (trooperOld == null || trooperOld.transform != transform)
            {
                trooperOld = GetComponent<TrooperOld>();
            }
        }
        
        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        protected override void OnEnable()
        {
            if (trooperOld == null)
            {
                trooperOld = GetComponent<TrooperOld>();
                if (trooperOld == null)
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
            
            if (trooperOld != null)
            {
                trooperOld.OnHitTrooper += HitTrooperOld;
                trooperOld.OnHitByTrooper += HitByTrooperOld;
                trooperOld.OnEliminatedTrooper += EliminatedTrooperOld;
                trooperOld.OnEliminatedByTrooper += EliminatedByTrooperOld;
                trooperOld.OnFlagPickedUp += OnFlagPickedUp;
                trooperOld.OnFlagCaptured += OnFlagCaptured;
                trooperOld.OnFlagReturned += OnFlagReturned;
                trooperOld.OnFlagDropped += OnFlagDropped;
            }
            
            base.OnEnable();
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            
            if (trooperOld == null)
            {
                return;
            }
            
            trooperOld.OnHitTrooper -= HitTrooperOld;
            trooperOld.OnHitByTrooper -= HitByTrooperOld;
            trooperOld.OnEliminatedTrooper -= EliminatedTrooperOld;
            trooperOld.OnEliminatedByTrooper -= EliminatedByTrooperOld;
            trooperOld.OnFlagPickedUp -= OnFlagPickedUp;
            trooperOld.OnFlagCaptured -= OnFlagCaptured;
            trooperOld.OnFlagReturned -= OnFlagReturned;
            trooperOld.OnFlagDropped -= OnFlagDropped;
        }
    }
}