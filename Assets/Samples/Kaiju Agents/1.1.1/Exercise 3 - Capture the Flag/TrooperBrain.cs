using System;
using KaijuSolutions.Agents.Extensions;
using KaijuSolutions.Agents.Utility;
using UnityEngine;
using Random = System.Random;


namespace KaijuSolutions.Agents.Exercises.CTF
{
    public class TrooperBrain : KaijuUtilityBrain
    {
        [SerializeField] private TrooperOld trooperOld;
        
        // true = attacker, false = defender
        [SerializeField] public bool attacker;
        
        // Fields to keep track of nearest pickups.
        [SerializeField] public HealthPickup healthPickup;
        [SerializeField] public AmmoPickup ammoPickup;

        // Use transform; keep track of last position (not actual flag)
        [SerializeField] public FlagOld friendlyFlagOld;
        [SerializeField] public FlagOld enemyFlagOld;

        [SerializeField] public TrooperOld nearestEnemy;
        
        public override void Awake()
        {
            base.Awake();
            trooperOld = GetComponent<TrooperOld>();

            // Randomly assign troopers as attackers or defenders, 50/50.
            Random random = new Random();
            this.attacker = random.NextDouble() < 0.5f;
        }

        /// <summary>
        /// Basically acts as working memory, updated after every decision made
        /// Maintains current data so Utility AI can accurately score and select best action
        /// </summary>
        protected override void UpdateBlackboard()
        {
            // Add trooper type to blackboard.
            SetBool("TrooperIsAttacker", this.attacker);
            
            // Update health, ammo and ability to shoot
            SetStatus();
            // Locate and calculate distances to nearest health and ammo resupplies
            SetPickups();
            // Track the status and distances of both team flags
            SetFlags();
            // Identify nearest enemy and check if in line of sight
            SetTroopers();
        }

        /// <summary>
        /// Updates the blackboard with info on health, ammo and ability to shoot
        /// </summary>
        private void SetStatus()
        {
            // Set health to [0, 1], scaled along the scale [0, max health]
            // Get max health from CaptureTheFlagManager, started at 100.
            SetScaled("StatusHealth", trooperOld.Health, 0f, CaptureTheFlagManager.Health);
            
            // Set ammo to [0, 1], scaled along the scale [0, max ammo]
            // Get max ammo from CaptureTheFlagManager, started at 30.
            SetScaled("StatusAmmo", trooperOld.Ammo, 0f, CaptureTheFlagManager.Ammo);
            
            // Check if empty, since checking if full doesn't work.
            SetBool("HasAmmo", trooperOld.HasAmmo);
            SetBool("TrooperCanShoot", trooperOld.CanAttack);
        }

        /// <summary>
        /// Arbitrary decided "max" distance (i.e., all distances greater than this are equal to 1 when scaled)
        /// </summary>
        private const float MaxDistance = 20f;

        /// <summary>
        /// Updates the blackboard with status and distance of health and ammo pickups
        /// (Allows Utility AI to weigh the agent's need to resupply against distance and availability)
        /// </summary>
        private void SetPickups()
        {
            // Store reference to ammo pickup
            Set("AmmoPickup", ammoPickup);
            
            // Calculate and normalize distance to ammo pickup
            SetScaled("AmmoPickupDistance", ammoPickup != null
                ? Agent.transform.Distance(ammoPickup.transform.position)
                : 0f, 0f, MaxDistance);
            
            // Tracks if ammo has already been picked up 
            SetBool("AmmoPickupOnCooldown", ammoPickup && ammoPickup.OnCooldown);
            
            // Store reference to health pickup
            Set("HealthPickup", healthPickup);
            
            // Flag is trooper is already at full health 
            SetBool("HealthFull", trooperOld.Health == CaptureTheFlagManager.Health);
            
            // Calculate and normalize the distance to health pickup 
            SetScaled("HealthPickupDistance", healthPickup != null
                ? Agent.transform.Distance(healthPickup.transform.position)
                : 0f, 0f, MaxDistance);
            
            // Track if health pack is currently unavailable
            SetBool("HealthPickupOnCooldown", healthPickup && healthPickup.OnCooldown);
            
        }

        /// <summary>
        /// Updates blackboard with current state and locations of both team flags
        /// (Used for actions like pursuing enemy flag or defending/recovering the friendly flag)
        /// </summary>
        private void SetFlags()
        {
            // Determine which flag belongs to which team
            FlagOld teamFlagOld = trooperOld.TeamOne ? FlagOld.TeamOneFlagOld : FlagOld.TeamTwoFlagOld;
            FlagOld otherFlagOld = trooperOld.TeamOne ? FlagOld.TeamTwoFlagOld : FlagOld.TeamOneFlagOld;
            
            // Store reference to team's flag
            Set("FriendlyFlag", teamFlagOld);
            // Check if friendly flag is at base
            Set("FriendlyFlagMissing", teamFlagOld.transform.position != FlagOld.Base3(trooperOld.TeamOne));
            // Set the base location where trooper needs to bring the enemy flag
            Set("CapturePoint", trooperOld.TeamOne ? FlagOld.TeamOneBase3 :  FlagOld.TeamTwoBase3);
            
            // Store reference to enemy flag
            Set("EnemyFlag", otherFlagOld);
            // Check if the enemy flag is being carried or if dropped
            Set("EnemyFlagCarried", otherFlagOld.Parent != null && otherFlagOld.Parent.name != "Flags");
            // Calculate and normalize the distance to the enemy flag, feeds into the CaptureDesire consideration
            SetScaled("EnemyFlagDistance", Agent.transform.Distance(otherFlagOld.transform.position), 0f, 100f);
        }

        /// <summary>
        /// Checks if there is a clear line of sight between transforms
        /// </summary>
        private bool CheckLineOfSight(Transform t1, Transform t2)
        {
            // Apply offset to cast the ray from about eye level
            Vector3 offset = Vector3.up * 1.75f;
            // Debug.Log((t1.position + offset).HasSight(t2.position + offset, out RaycastHit _, 0.1f));
            
            // Performs raycast from eye level to target's eye level, returns true if ray reaches target with no obstacles
            return (t1.position + offset).HasSight(t2.position + offset, out RaycastHit _, 0.1f);
        }

        /// <summary>
        /// Updates the blackboard with combat related awareness
        /// Tracks the closest enemy threat and makes troopers only fight when realistic conditions are met
        /// </summary>
        private void SetTroopers()
        {
            Set("NearestEnemy", nearestEnemy);
            // Calculate distance to the enemy and normalize it based on max distance
            // If no enemy is found, defaults to max distance, feeds into nearestenemydistance consideration
            SetScaled("NearestEnemyDistance", nearestEnemy != null
                ? Agent.transform.Distance(nearestEnemy.transform.position) : MaxDistance, 0f, MaxDistance);
            // Check to see if there is a clear path to the enemy
            SetBool("NearestEnemyLineOfSight", nearestEnemy != null && CheckLineOfSight(trooperOld.transform, nearestEnemy.transform));
        }

        /// <summary>
        /// Public function, called in TrooperController when a trooper picks up / drops a flag.
        /// </summary>
        /// <param name="v"></param>
        public void SetHoldingFlag(bool v)
        {
            Set("HasFlag", v);
        }
    }
}
