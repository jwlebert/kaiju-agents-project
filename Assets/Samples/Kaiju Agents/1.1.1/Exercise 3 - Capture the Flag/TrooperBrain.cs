using System;
using KaijuSolutions.Agents.Extensions;
using KaijuSolutions.Agents.Utility;
using UnityEngine;

namespace KaijuSolutions.Agents.Exercises.CTF
{
    public class TrooperBrain : KaijuUtilityBrain
    {
        [SerializeField] private Trooper trooper;
        
        [SerializeField] public HealthPickup healthPickup;
        [SerializeField] public AmmoPickup ammoPickup;

        // Use transform; keep track of last position (not actual flag)
        [SerializeField] public Transform friendlyFlag;
        [SerializeField] public Transform enemyFlag;

        [SerializeField] public Trooper nearestEnemy;
        
        public override void Awake()
        {
            base.Awake();
            trooper = GetComponent<Trooper>();
        }

        protected override void UpdateBlackboard()
        {
            SetStatus();
            SetPickups();
            SetFlags();
            SetTroopers();
        }

        private void SetStatus()
        {
            // Set health to [0, 1], scaled along the scale [0, max health]
            // Get max health from CaptureTheFlagManager, started at 100.
            SetScaled("StatusHealth", trooper.Health, 0f, CaptureTheFlagManager.Health);
            
            // Set ammo to [0, 1], scaled along the scale [0, max ammo]
            // Get max ammo from CaptureTheFlagManager, started at 30.
            SetScaled("StatusAmmo", trooper.Ammo, 0f, CaptureTheFlagManager.Ammo);
        }

        /// <summary>
        /// Arbitrary decided "max" distance (i.e., all distances greater than this are equal to 1 when scaled)
        /// </summary>
        private const float MaxDistance = 50f;
        private void SetPickups()
        {
            Set("AmmoPickup", ammoPickup);
            // Check if empty, since checking if full doesn't work.
            SetBool("AmmoEmpty", trooper.HasAmmo);
            SetScaled("AmmoPickupDistance", ammoPickup != null 
                ? Agent.transform.Distance(ammoPickup.transform.position) : 0f, 0f, MaxDistance);

            Set("HealthPickup", healthPickup);
            SetBool("HealthFull", trooper.Health == CaptureTheFlagManager.Health);
            SetScaled("HealthPickupDistance", healthPickup != null 
                ? Agent.transform.Distance(healthPickup.transform.position) : 0f, 0f, MaxDistance);
        }

        private void SetFlags()
        {
            Set("FriendlyFlag", friendlyFlag);
            SetScaled("FriendlyFlagDistance", friendlyFlag != null 
                ? Agent.transform.Distance(friendlyFlag.position) : 0f, 0f, MaxDistance);
            SetBool("FriendlyFlagMissing", friendlyFlag != null && 
                (Vector2)friendlyFlag.position != Flag.Base(trooper.TeamOne));
            
            Set("EnemyFlag", enemyFlag);
            SetScaled("EnemyFlagDistance", enemyFlag != null 
                ? Agent.transform.Distance(enemyFlag.position) : 0f, 0f, MaxDistance);
            SetBool("EnemyFlagMissing", enemyFlag != null && 
                (Vector2)enemyFlag.position != Flag.Base(!trooper.TeamOne));
        }

        private void SetTroopers()
        {
            Set("NearestEnemy", nearestEnemy);
            SetScaled("NearestEnemyDistance", nearestEnemy != null
                ? Agent.transform.Distance(nearestEnemy.transform.position) : 0f, 0f, MaxDistance);
        }
    }
}
