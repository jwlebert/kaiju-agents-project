using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors; 
using KaijuSolutions.Agents.Exercises.CTF.ML;
using KaijuSolutions.Agents.Sensors;
using UnityEngine.InputSystem;
using KaijuSolutions.Agents;
using System;
using System.Reflection;
using UnityEngine.AI;

namespace KaijuSolutions.Agents.Exercises.CTF.ML
{
    /// <summary>
    /// Acts as the bridge between Unity ML-Agents and the Kaiju framework's physical Trooper.
    /// Handles observation collection (using agent-relative coordinates for better learning),
    /// manual heuristic control, and translating ML actions into physics forces.
    /// Includes workarounds (Reflection) to handle assembly clashes if different Trooper
    /// versions exist in the project.
    /// </summary>
    public class TrooperMLAgent : Agent
    {
        private EnvironmentParameters _environment;
        
        // Reference to the base component, used when direct casting fails due to assembly clash.
        private Trooper _trooper;
        private KaijuAgent _kaijuAgent;
        private Rigidbody _rb;
        
        private Flag _enemyFlag;
        private Flag _friendlyFlag;
        private Vector3 _friendlyBasePosition;
        
        private TrooperEnemyVisionSensor _enemySensor;
        private AmmoVisionSensor _ammoSensor;
        private HealthVisionSensor _healthSensor;
        
        private bool _hasFlag;
        
        private int _stepCount;
        public  int MaxStepsAllowed = 500;
        
        // Used for anti-wiggling reward shaping.
        private float _closestDistanceToEnemyFlag;
        private float _closestDistanceToFriendlyBase;
        
        private const float MaxHealth = 100f; 
        private const float MaxAmmo = 30f;
        private readonly float _maxMapDistance = 150f; 
        private const float MaxSensorDist = 20f;
        
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            if (!SetupReferences()) return;

            // RULE 1: Do not allow shooting if the agent has no ammo or is on cooldown
            if (!_trooper.CanAttack)
            {
                // Branch 2 is Combat. Action 1 is "Shoot". Set it to false (disabled).
                actionMask.SetActionEnabled(2, 1, false); 
            }
        }
        
        private NavMeshPath path;
        private float GetPathDistance(Vector3 start, Vector3 target)
        {
            // Calculate the path around the walls
            if (NavMesh.CalculatePath(start, target, NavMesh.AllAreas, path))
            {
                float distance = 0.0f;
        
                // Sum up the length of all the corners along the path
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    distance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
                }
                return distance;
            }
    
            // Fallback just in case the path fails
            return Vector3.Distance(start, target); 
        }

        public override void Initialize()
        {
            path = new NavMeshPath();
            
            SetupReferences();
            _environment = Academy.Instance.EnvironmentParameters;
            _environment.GetWithDefault("map_level", 1f);
        }

        /// <summary>
        /// Lazy-loads necessary components. Prevents NullReferenceExceptions during 
        /// early spawning frames before Unity has finished attaching all scripts.
        /// </summary>
        private bool SetupReferences()
        {
            if (_kaijuAgent == null) _kaijuAgent = GetComponent<KaijuAgent>();
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            
            if (_trooper == null)
            {
                _trooper = GetComponent<KaijuSolutions.Agents.Exercises.CTF.ML.Trooper>();
            }

            if (_kaijuAgent == null || _rb == null || _trooper == null) return false;
            
            if (_enemySensor == null) _enemySensor = _kaijuAgent.GetSensor<TrooperEnemyVisionSensor>();
            if (_healthSensor == null) _healthSensor = _kaijuAgent.GetSensor<HealthVisionSensor>();
            if (_ammoSensor == null) _ammoSensor = _kaijuAgent.GetSensor<AmmoVisionSensor>();

            // Always verify flags are present (they might have been destroyed/recreated during curriculum reset)
            if (_friendlyFlag == null || _enemyFlag == null)
            {
                bool isTeamOne = _trooper.TeamOne;

                Flag[] flags = FindObjectsByType<Flag>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (Flag f in flags)
                {
                    if (f.TeamOne == isTeamOne)
                    {
                        _friendlyFlag = f;
                        _friendlyBasePosition = f.transform.position; 
                    }
                    else
                    {
                        _enemyFlag = f;
                    }
                }

                if (_trooper != null)
                {
                    // Unsubscribe first to avoid double-subscription if we are re-linking
                    _trooper.OnFlagPickedUp -= HandleFlagPickedUp;
                    _trooper.OnFlagDropped -= HandleFlagDropped;
                    _trooper.OnFlagCaptured -= HandleFlagCaptured;
                    _trooper.OnFlagReturned -= HandleFlagReturned;
                    _trooper.OnEliminatedTrooper -= HandleEliminatedEnemy;
                    _trooper.OnEliminatedByTrooper -= HandleEliminatedByEnemy;
                    //_trooper.OnHitTrooper -= HandleHitEnemy;
                    _trooper.OnHealth -= HandleHealthPickup;
                    _trooper.OnAmmo -= HandleAmmoPickup;

                    _trooper.OnFlagPickedUp += HandleFlagPickedUp;
                    _trooper.OnFlagDropped += HandleFlagDropped;
                    _trooper.OnFlagCaptured += HandleFlagCaptured;
                    _trooper.OnFlagReturned += HandleFlagReturned;
                    _trooper.OnEliminatedTrooper += HandleEliminatedEnemy;
                    _trooper.OnEliminatedByTrooper += HandleEliminatedByEnemy;
                    //_trooper.OnHitTrooper += HandleHitEnemy;
                    _trooper.OnHealth += HandleHealthPickup;
                    _trooper.OnAmmo += HandleAmmoPickup;
                }
            }

            return true;
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            SetupReferences();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_trooper == null) return;

            // Unsubscribe to prevent memory leaks
            _trooper.OnFlagPickedUp -= HandleFlagPickedUp;
            _trooper.OnFlagDropped -= HandleFlagDropped;
            _trooper.OnFlagCaptured -= HandleFlagCaptured;
            _trooper.OnFlagReturned -= HandleFlagReturned;
            _trooper.OnEliminatedTrooper -= HandleEliminatedEnemy;
            _trooper.OnEliminatedByTrooper -= HandleEliminatedByEnemy;
            //_trooper.OnHitTrooper -= HandleHitEnemy;
            _trooper.OnHealth -= HandleHealthPickup;
            _trooper.OnAmmo -= HandleAmmoPickup;
        }
        
        public override void OnEpisodeBegin()
        {
            if (this == null || !gameObject.activeInHierarchy) return; // ← fixes the 151 errors
            if (!SetupReferences()) return;
            
            CaptureTheFlagManager.Instance?.NotifyEpisodeBegin(); // ← reads fresh level
            if (this == null) return; // Abort if this agent was destroyed by the level rebuild
            
            _hasFlag = false; // Reset their brain so they don't think they already have a flag
            if (_friendlyFlag != null) _friendlyBasePosition = _friendlyFlag.transform.position; //Tell them where the NEW base is
            
            // 1. Fetch the curriculum parameter (Defaults to 11 if not training)
            // int desiredEnemies = (int)_environment.GetWithDefault("enemy_count", 11f);
            
            // TODO: 2. Add logic here to enable/disable enemy troopers based on 'desiredEnemies'
            
            // Stop any leftover physics momentum from the previous episode.
            _kaijuAgent.Stop();
            _stepCount = 0; // Reset the counter
            
            // Baseline the distances so reward shaping only rewards NEW progress.
            if (_enemyFlag != null)
                _closestDistanceToEnemyFlag = GetPathDistance(transform.position, _enemyFlag.transform.position);

            _closestDistanceToFriendlyBase = GetPathDistance(transform.position, _friendlyBasePosition);
        }
        
        /// <summary>
        /// Total Observation Space: 19 floats.
        /// Agent-relative coordinates (Quaternion.Inverse) are used so the AI always perceives
        /// objects relative to where it is facing (e.g., "enemy is to my left"), speeding up learning.
        /// </summary>
        public override void CollectObservations(VectorSensor sensor)
        {
            if (!SetupReferences())
            {
                // Pad with zeros to prevent Vector size mismatch errors during early frames.
                for (int i = 0; i < 22; i++) sensor.AddObservation(0f);
                return;
            }

            // 1-3. Internal State
            float health = _trooper.Health;
            float ammo = _trooper.Ammo;

            sensor.AddObservation(health / MaxHealth);
            sensor.AddObservation(ammo / MaxAmmo);
            sensor.AddObservation(_hasFlag ? 1.0f : 0.0f);
            
            // 4-6. Enemy Flag Awareness
            if (_enemyFlag != null)
            {
                sensor.AddObservation(Vector3.Distance(transform.position, _enemyFlag.transform.position) / _maxMapDistance);
                // Convert world direction to local direction
                Vector3 dirToFlag = (Quaternion.Inverse(transform.rotation) * (_enemyFlag.transform.position - transform.position)).normalized;
                sensor.AddObservation(dirToFlag.x);
                sensor.AddObservation(dirToFlag.z);
            }
            else { sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f); }

            // 7-9. Friendly Flag Awareness
            if (_friendlyFlag != null)
            {
                sensor.AddObservation(Vector3.Distance(transform.position, _friendlyFlag.transform.position) / _maxMapDistance);
                Vector3 dirToFlag = (Quaternion.Inverse(transform.rotation) * (_friendlyFlag.transform.position - transform.position)).normalized;
                sensor.AddObservation(dirToFlag.x);
                sensor.AddObservation(dirToFlag.z);
            }
            else { sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f); }

            // 10. Friendly Base Distance
            sensor.AddObservation(Vector3.Distance(transform.position, _friendlyBasePosition) / _maxMapDistance);

            // 11-13. Nearest Enemy (Relative to agent's facing)
            float enemyDist = 1.0f;
            Vector3 enemyDir = Vector3.zero;
            if (_enemySensor != null && _enemySensor.Observed != null)
            {
                float minD = float.MaxValue;
                foreach (var e in _enemySensor.Observed)
                {
                    if (e == null) continue;
                    float d = Vector3.Distance(transform.position, e.transform.position);
                    if (d < minD) 
                    { 
                        minD = d; 
                        enemyDir = (Quaternion.Inverse(transform.rotation) * (e.transform.position - transform.position)).normalized; 
                    }
                }
                if (minD != float.MaxValue) enemyDist = minD / MaxSensorDist;
            }
            sensor.AddObservation(enemyDist);
            sensor.AddObservation(enemyDir.x);
            sensor.AddObservation(enemyDir.z);

            // 14-19. Pickups (Health and Ammo)
            AddPickupObs(sensor, _healthSensor);
            AddPickupObs(sensor, _ammoSensor);
            
            // 20-22. Compass to the current primary objective
            Vector3 targetPos = Vector3.zero;
            bool isFriendlyFlagStolen = false;

// ONLY check distance if the flag actually exists right now
            if (_friendlyFlag != null)
            {
                isFriendlyFlagStolen = Vector3.Distance(_friendlyFlag.transform.position, _friendlyBasePosition) > 1.0f;
            }

            if (isFriendlyFlagStolen && !_hasFlag && _friendlyFlag != null)
            {
                // PRIORITY: If our flag is stolen, go get it back!
                targetPos = _friendlyFlag.transform.position;
            }
            else if (!_hasFlag && _enemyFlag != null)
            {
                // GOAL: Go get the enemy flag
                targetPos = _enemyFlag.transform.position;
            }
            else if (_hasFlag)
            {
                // GOAL: Bring the enemy flag home
                targetPos = _friendlyBasePosition;
            }

            if (targetPos != Vector3.zero)
            {
                Vector3 localTarget = transform.InverseTransformPoint(targetPos);
                sensor.AddObservation(localTarget.normalized);
            }
            else
            {
                sensor.AddObservation(Vector3.zero);
            }
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            // Essential components check
            if (_kaijuAgent == null || _rb == null) 
            {
                _kaijuAgent = GetComponent<KaijuAgent>();
                _rb = GetComponent<Rigidbody>();
                if (_kaijuAgent == null || _rb == null) return;
            }

            // 3 Branches: [Movement(3), Rotation(3), Combat(2)]
            var discreteActions = actions.DiscreteActions;
            
            // Branch 0: Movement (0=Idle, 1=Forward, 2=Backward)
            float moveVal = 0;
            if (discreteActions[0] == 1) moveVal = 1f;
            else if (discreteActions[0] == 2) moveVal = -1f;
            _rb.linearVelocity = transform.forward * moveVal * _kaijuAgent.MoveSpeed;

            // Branch 1: Rotation (0=Idle, 1=Right, 2=Left)
            float rotateVal = 0;
            if (discreteActions[1] == 1) rotateVal = 1f;
            else if (discreteActions[1] == 2) rotateVal = -1f;
            if (rotateVal != 0)
            {
                transform.Rotate(Vector3.up, rotateVal * _kaijuAgent.LookSpeed * Time.fixedDeltaTime);
            }

            // Combat and Reward Shaping require full references (Trooper + Flags)
            if (!SetupReferences()) return;

            // Branch 2: Combat
            if (discreteActions[2] == 1)
            {
                _trooper.Attack();
                
                // 1. Get current ammo as a percentage (0.0 to 1.0) 
                float ammoPct = Mathf.Clamp01(_trooper.Ammo / MaxAmmo);
                
                // 2. Gently scale the penalty based on the ammo percentage. 
                // If Ammo is Full (1.0), the penalty is very light (-0.001f) 
                // If Ammo is Empty (0.0), the penalty is harsher (-0.005f) 
                float shootPenalty = Mathf.Lerp(-0.005f, -0.001f, ammoPct);
                
                AddReward(shootPenalty);
            }
            else
            {
                _trooper.StopAttacking();
            }
            
            // --- REWARD SHAPING ---
            // (Rest of the reward shaping remains the same)

            // Only calculate distance rewards if we haven't timed out and aren't dead
            if (!_hasFlag && _enemyFlag != null)
            {
                float currentDist = GetPathDistance(transform.position, _enemyFlag.transform.position);
        
                // ONLY reward if they broke their previous record!
                if (currentDist < _closestDistanceToEnemyFlag) 
                {
                    float distanceDelta = _closestDistanceToEnemyFlag - currentDist;
                    AddReward(distanceDelta * 0.002f); // Increased from 0.1f
                    _closestDistanceToEnemyFlag = currentDist; 
                }
            }
            else if (_hasFlag && _friendlyFlag != null)
            {
                float currentDist = GetPathDistance(transform.position, _friendlyBasePosition);
        
                // ONLY reward if they broke their previous record!
                if (currentDist < _closestDistanceToFriendlyBase)
                {
                    float distanceDelta = _closestDistanceToFriendlyBase - currentDist;
                    AddReward(distanceDelta * 0.002f); // Increased from 0.1f
                    _closestDistanceToFriendlyBase = currentDist;
                }
            }

            // Keep the existential penalty so they don't dawdle
            AddReward(-1f / 5000f);
            
            if (++_stepCount >= (CaptureTheFlagManager.Instance?.MaxStepsForLevel ?? 3000))
            {
                // AddReward(-1.0f); // Penalize for failing the objective
                CaptureTheFlagManager.Instance?.NotifyEpisodeEnd();
                EndEpisode();
            }
        }
        
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var discreteActionsOut = actionsOut.DiscreteActions;
            discreteActionsOut[0] = 0; discreteActionsOut[1] = 0; discreteActionsOut[2] = 0;

            if (Keyboard.current == null) return;

            if (Keyboard.current.wKey.isPressed) discreteActionsOut[0] = 1;
            else if (Keyboard.current.sKey.isPressed) discreteActionsOut[0] = 2;

            if (Keyboard.current.dKey.isPressed) discreteActionsOut[1] = 1;
            else if (Keyboard.current.aKey.isPressed) discreteActionsOut[1] = 2;

            if (Keyboard.current.spaceKey.isPressed) discreteActionsOut[2] = 1;
        }

        // --- Helper methods to handle Assembly Conflicts (Reflection Fallback) ---
        // TODO: Remove these when all Trooper scripts exist in the same assembly namespace.
        

        private void AddPickupObs<T>(VectorSensor sensor, KaijuVisionSensor<T> s) where T : Pickup
        {
            T nearest = null;
            float minDist = float.MaxValue;
            if (s != null && s.Observed != null)
            {
                foreach (var p in s.Observed)
                {
                    if (p == null) continue;
                    bool onCooldown = false;
                    if (p is NumberPickup np) onCooldown = np.OnCooldown;
                    if (onCooldown) continue;
                    float d = Vector3.Distance(transform.position, p.transform.position);
                    if (d < minDist) { minDist = d; nearest = p; }
                }
            }
            if (nearest != null)
            {
                sensor.AddObservation(minDist / MaxSensorDist);
                Vector3 dir = (Quaternion.Inverse(transform.rotation) * (nearest.transform.position - transform.position)).normalized;
                sensor.AddObservation(dir.x); sensor.AddObservation(dir.z);
            }
            else { sensor.AddObservation(1.0f); sensor.AddObservation(0f); sensor.AddObservation(0f); }
        }
        
        // Game Objective Rewards
        // TODO - I think it was bad to have it outside of range [-1f, 1f]?
        private void HandleFlagPickedUp(Flag flag)
        {
            _hasFlag = true;
            // Reset base distance so reward shaping starts from the current position
            _closestDistanceToFriendlyBase = GetPathDistance(transform.position, _friendlyBasePosition);
            AddReward(0.1f); // BIG reward for grabbing the enemy flag!
        }

        private void HandleFlagDropped(Flag flag)
        {
            _hasFlag = false; 
            // Reset enemy flag distance so they can earn rewards for going back to it
            if (_enemyFlag != null)
                _closestDistanceToEnemyFlag = GetPathDistance(transform.position, _enemyFlag.transform.position);
            AddReward(-0.1f);
        }

        private void HandleFlagCaptured(Flag flag)
        {
            _hasFlag = false; 
            AddReward(1.0f); 
            CaptureTheFlagManager.Instance?.NotifyEpisodeEnd();
            EndEpisode();
        }

        private void HandleFlagReturned(Flag flag)
        {
            _hasFlag = false; 
            AddReward(0.1f); // Reward for saving your own flag
        }

        private void HandleEliminatedEnemy(Trooper e)
        {
            AddReward(0.1f);
        }

        private void HandleHitEnemy(Trooper e)
        {
            AddReward(0.02f); // Slightly higher reward for combat success
        }

        private void HandleEliminatedByEnemy(Trooper e)
        {
            AddReward(-0.2f); 
            CaptureTheFlagManager.Instance?.NotifyEpisodeEnd();
            EndEpisode();
        }
        private void HandleHealthPickup(HealthPickup pickup) 
        { 
            // 1. Calculate how much health we are missing (0.0 = full health, 1.0 = near death)
            // Note: Use Mathf.Clamp01 just in case the pickup overheals past MaxHealth
            float missingHealthPct = Mathf.Clamp01(1f - _trooper.Health / MaxHealth);
            
            // 2. Linear Scaling: Multiply the max possible reward (e.g., 0.2f) by the missing percentage.
            // Near dead = +0.20 reward. Full health = +0.00 reward.
            float dynamicReward = 0.2f * missingHealthPct; 
            
            AddReward(dynamicReward);
        }
        private void HandleAmmoPickup(AmmoPickup pickup) 
        { 
             float missingAmmoPct = Mathf.Clamp01(1f - _trooper.Ammo / MaxAmmo);
             
             // 2. Quadratic (Exponential) Scaling: Squaring the percentage creates a curve.
             // This strongly discourages hoarding. If you are missing 50% ammo, you only get 25% of the reward. 
             // You only get the big reward when you are truly running on empty.
             float dynamicReward = 0.2f * (missingAmmoPct * missingAmmoPct);
             
             AddReward(dynamicReward);
        }
    }
}
