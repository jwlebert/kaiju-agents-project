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

        private void Start()
        {
            trooper = GetComponent<Trooper>();
        }

        protected override void UpdateBlackboard()
        {
            SetStatus();
            SetPickups();
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
            SetScaled("AmmoPickupDistance", this.Agent.transform.Distance(ammoPickup.transform.position), 0f, MaxDistance);
            
            Set("HealthPickup", healthPickup);
            SetScaled("HealthPickupDistance", this.Agent.transform.Distance(healthPickup.transform.position), 0f, MaxDistance);
        }

        private void SetFlags()
        {
            Set("FriendlyFlag", friendlyFlag);
            SetScaled("FriendlyFlagDistance", this.Agent.transform.Distance(friendlyFlag.transform.position), 0f, MaxDistance);

            Set("EnemyFlag", enemyFlag);
            SetScaled("EnemyFlagDistance", this.Agent.transform.Distance(enemyFlag.transform.position), 0f, MaxDistance);
        }

        private void SetTroopers()
        {
            
        }
    }
}
