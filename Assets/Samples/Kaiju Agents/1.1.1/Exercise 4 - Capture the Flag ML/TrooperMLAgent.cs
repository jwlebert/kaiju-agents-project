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
    /// Handles observation collection, manual heuristic control, and translating ML actions into physics forces.
    /// </summary>
    public class TrooperMLAgent : Agent
    {
        /// <summary>
        /// Environment parameters provided by the ML-Agents Academy.
        /// </summary>
        private EnvironmentParameters _environment;
        
        /// <summary>
        /// Reference to the Trooper component.
        /// </summary>
        private Trooper _trooper;
        
        /// <summary>
        /// Reference to the base KaijuAgent component.
        /// </summary>
        private KaijuAgent _kaijuAgent;
        
        /// <summary>
        /// Reference to the Rigidbody component.
        /// </summary>
        private Rigidbody _rb;
        
        /// <summary>
        /// Reference to the enemy team's flag.
        /// </summary>
        private Flag _enemyFlag;
        
        /// <summary>
        /// Reference to the friendly team's flag.
        /// </summary>
        private Flag _friendlyFlag;
        
        /// <summary>
        /// The starting position of the friendly team's base.
        /// </summary>
        private Vector3 _friendlyBasePosition;
        
        /// <summary>
        /// Vision sensor for detecting enemy troopers.
        /// </summary>
        private TrooperEnemyVisionSensor _enemySensor;
        
        /// <summary>
        /// Vision sensor for detecting ammo pickups.
        /// </summary>
        private AmmoVisionSensor _ammoSensor;
        
        /// <summary>
        /// Vision sensor for detecting health pickups.
        /// </summary>
        private HealthVisionSensor _healthSensor;
        
        /// <summary>
        /// Tracks whether this agent is currently carrying a flag.
        /// </summary>
        private bool _hasFlag;
        
        /// <summary>
        /// The current step count for the episode.
        /// </summary>
        private int _stepCount;
        
        /// <summary>
        /// The maximum number of steps allowed before the episode is terminated.
        /// </summary>
        public int MaxStepsAllowed = 500;
        
        /// <summary>
        /// Tracks the closest distance the agent has reached to the enemy flag.
        /// </summary>
        private float _closestDistanceToEnemyFlag;
        
        /// <summary>
        /// Tracks the closest distance the agent has reached to the friendly base while carrying the flag.
        /// </summary>
        private float _closestDistanceToFriendlyBase;
        
        /// <summary>
        /// The maximum possible health for a trooper.
        /// </summary>
        private const float MaxHealth = 100f; 
        
        /// <summary>
        /// The maximum possible ammo for a trooper.
        /// </summary>
        private const float MaxAmmo = 30f;
        
        /// <summary>
        /// The maximum expected distance across the map, used for observation normalization.
        /// </summary>
        private readonly float _maxMapDistance = 150f; 
        
        /// <summary>
        /// The maximum distance at which sensors detect objects.
        /// </summary>
        private const float MaxSensorDist = 20f;
        
        /// <summary>
        /// The path calculation object for navmesh distance logic.
        /// </summary>
        private NavMeshPath path;

        /// <summary>
        /// Sets up the discrete action mask to prevent invalid actions, such as shooting without ammo.
        /// </summary>
        /// <param name="actionMask">The action mask provided by ML-Agents.</param>
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            if (!SetupReferences())
            {
                return;
            }

            if (!_trooper.CanAttack)
            {
                actionMask.SetActionEnabled(2, 1, false); 
            }
        }
        
        /// <summary>
        /// Calculates the distance along the navmesh path between two points.
        /// </summary>
        /// <param name="start">The starting point.</param>
        /// <param name="target">The destination point.</param>
        /// <returns>The calculated path distance, or straight line distance if pathing fails.</returns>
        private float GetPathDistance(Vector3 start, Vector3 target)
        {
            if (NavMesh.CalculatePath(start, target, NavMesh.AllAreas, path))
            {
                float distance = 0.0f;
        
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    distance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
                }
                
                return distance;
            }
    
            return Vector3.Distance(start, target); 
        }

        /// <summary>
        /// Initializes the agent by setting up references and environment parameters.
        /// </summary>
        public override void Initialize()
        {
            path = new NavMeshPath();
            
            SetupReferences();
            
            _environment = Academy.Instance.EnvironmentParameters;
            _environment.GetWithDefault("map_level", 1f);
        }

        /// <summary>
        /// Lazy-loads necessary components to prevent NullReferenceExceptions.
        /// </summary>
        /// <returns>True if all references were successfully established, false otherwise.</returns>
        private bool SetupReferences()
        {
            if (_kaijuAgent == null)
            {
                _kaijuAgent = GetComponent<KaijuAgent>();
            }
            
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody>();
            }
            
            if (_trooper == null)
            {
                _trooper = GetComponent<KaijuSolutions.Agents.Exercises.CTF.ML.Trooper>();
            }

            if (_kaijuAgent == null || _rb == null || _trooper == null)
            {
                return false;
            }
            
            if (_enemySensor == null)
            {
                _enemySensor = _kaijuAgent.GetSensor<TrooperEnemyVisionSensor>();
            }
            
            if (_healthSensor == null)
            {
                _healthSensor = _kaijuAgent.GetSensor<HealthVisionSensor>();
            }
            
            if (_ammoSensor == null)
            {
                _ammoSensor = _kaijuAgent.GetSensor<AmmoVisionSensor>();
            }

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
                    _trooper.OnFlagPickedUp -= HandleFlagPickedUp;
                    _trooper.OnFlagDropped -= HandleFlagDropped;
                    _trooper.OnFlagCaptured -= HandleFlagCaptured;
                    _trooper.OnFlagReturned -= HandleFlagReturned;
                    _trooper.OnEliminatedTrooper -= HandleEliminatedEnemy;
                    _trooper.OnEliminatedByTrooper -= HandleEliminatedByEnemy;
                    _trooper.OnHealth -= HandleHealthPickup;
                    _trooper.OnAmmo -= HandleAmmoPickup;

                    _trooper.OnFlagPickedUp += HandleFlagPickedUp;
                    _trooper.OnFlagDropped += HandleFlagDropped;
                    _trooper.OnFlagCaptured += HandleFlagCaptured;
                    _trooper.OnFlagReturned += HandleFlagReturned;
                    _trooper.OnEliminatedTrooper += HandleEliminatedEnemy;
                    _trooper.OnEliminatedByTrooper += HandleEliminatedByEnemy;
                    _trooper.OnHealth += HandleHealthPickup;
                    _trooper.OnAmmo += HandleAmmoPickup;
                }
            }

            return true;
        }
        
        /// <summary>
        /// Handles the component enable event.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            SetupReferences();
        }

        /// <summary>
        /// Handles the component disable event to clear subscriptions.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
            if (_trooper == null)
            {
                return;
            }

            _trooper.OnFlagPickedUp -= HandleFlagPickedUp;
            _trooper.OnFlagDropped -= HandleFlagDropped;
            _trooper.OnFlagCaptured -= HandleFlagCaptured;
            _trooper.OnFlagReturned -= HandleFlagReturned;
            _trooper.OnEliminatedTrooper -= HandleEliminatedEnemy;
            _trooper.OnEliminatedByTrooper -= HandleEliminatedByEnemy;
            _trooper.OnHealth -= HandleHealthPickup;
            _trooper.OnAmmo -= HandleAmmoPickup;
        }
        
        /// <summary>
        /// Prepares the agent for a new episode.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            if (this == null || !gameObject.activeInHierarchy)
            {
                return;
            }
            
            CaptureTheFlagManager.Instance?.NotifyEpisodeBegin(); // ← reads fresh level
            if (this == null) return; // Abort if this agent was destroyed by the level rebuild
            
            CaptureTheFlagManager.Instance?.NotifyEpisodeBegin();
            
            if (this == null)
            {
                return;
            }
            
            _hasFlag = false;
            if (_friendlyFlag != null) _friendlyBasePosition = _friendlyFlag.transform.position;
            
            // Stop any leftover physics momentum from the previous episode.
            _kaijuAgent.Stop();
            _stepCount = 0;
            
            // Baseline the distances so reward shaping only rewards NEW progress.
            if (_enemyFlag != null)
            {
                _closestDistanceToEnemyFlag = GetPathDistance(transform.position, _enemyFlag.transform.position);
            }

            _closestDistanceToFriendlyBase = GetPathDistance(transform.position, _friendlyBasePosition);
        }
        
        /// <summary>
        /// Collects vector observations from the environment.
        /// </summary>
        /// <param name="sensor">The vector sensor provided by ML-Agents.</param>
        public override void CollectObservations(VectorSensor sensor)
        {
            if (!SetupReferences())
            {
                for (int i = 0; i < 22; i++)
                {
                    sensor.AddObservation(0f);
                }
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
            else 
            { 
                sensor.AddObservation(0f); 
                sensor.AddObservation(0f); 
                sensor.AddObservation(0f); 
            }

            // 7-9. Friendly Flag Awareness
            if (_friendlyFlag != null)
            {
                sensor.AddObservation(Vector3.Distance(transform.position, _friendlyFlag.transform.position) / _maxMapDistance);
                Vector3 dirToFlag = (Quaternion.Inverse(transform.rotation) * (_friendlyFlag.transform.position - transform.position)).normalized;
                sensor.AddObservation(dirToFlag.x);
                sensor.AddObservation(dirToFlag.z);
            }
            else 
            { 
                sensor.AddObservation(0f); 
                sensor.AddObservation(0f); 
                sensor.AddObservation(0f); 
            }

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
                    if (e == null)
                    {
                        continue;
                    }
                    
                    float d = Vector3.Distance(transform.position, e.transform.position);
                    if (d < minD) 
                    { 
                        minD = d; 
                        enemyDir = (Quaternion.Inverse(transform.rotation) * (e.transform.position - transform.position)).normalized; 
                    }
                }
                if (minD != float.MaxValue)
                {
                    enemyDist = minD / MaxSensorDist;
                }
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

        /// <summary>
        /// Executes the discrete actions chosen by the agent's policy.
        /// </summary>
        /// <param name="actions">The actions provided by ML-Agents.</param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            if (_kaijuAgent == null || _rb == null) 
            {
                _kaijuAgent = GetComponent<KaijuAgent>();
                _rb = GetComponent<Rigidbody>();
                if (_kaijuAgent == null || _rb == null)
                {
                    return;
                }
            }

            var discreteActions = actions.DiscreteActions;
            
            float moveVal = 0;
            if (discreteActions[0] == 1)
            {
                moveVal = 1f;
            }
            else if (discreteActions[0] == 2)
            {
                moveVal = -1f;
            }
            
            _rb.linearVelocity = transform.forward * moveVal * _kaijuAgent.MoveSpeed;

            float rotateVal = 0;
            if (discreteActions[1] == 1)
            {
                rotateVal = 1f;
            }
            else if (discreteActions[1] == 2)
            {
                rotateVal = -1f;
            }
            
            if (rotateVal != 0)
            {
                transform.Rotate(Vector3.up, rotateVal * _kaijuAgent.LookSpeed * Time.fixedDeltaTime);
            }

            if (!SetupReferences())
            {
                return;
            }

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
                CaptureTheFlagManager.Instance?.NotifyEpisodeEnd();
                EndEpisode();
            }
        }
        
        /// <summary>
        /// Provides manual heuristic controls for testing the agent behavior.
        /// </summary>
        /// <param name="actionsOut">The actions array to populate with heuristic input.</param>
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var discreteActionsOut = actionsOut.DiscreteActions;
            
            discreteActionsOut[0] = 0; 
            discreteActionsOut[1] = 0; 
            discreteActionsOut[2] = 0;

            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.wKey.isPressed)
            {
                discreteActionsOut[0] = 1;
            }
            else if (Keyboard.current.sKey.isPressed)
            {
                discreteActionsOut[0] = 2;
            }

            if (Keyboard.current.dKey.isPressed)
            {
                discreteActionsOut[1] = 1;
            }
            else if (Keyboard.current.aKey.isPressed)
            {
                discreteActionsOut[1] = 2;
            }

            if (Keyboard.current.spaceKey.isPressed)
            {
                discreteActionsOut[2] = 1;
            }
        }

        /// <summary>
        /// Adds observations to the sensor for the nearest pickup of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of pickup to observe.</typeparam>
        /// <param name="sensor">The vector sensor to append observations to.</param>
        /// <param name="s">The vision sensor for tracking pickups.</param>
        private void AddPickupObs<T>(VectorSensor sensor, KaijuVisionSensor<T> s) where T : Pickup
        {
            T nearest = null;
            float minDist = float.MaxValue;
            
            if (s != null && s.Observed != null)
            {
                foreach (var p in s.Observed)
                {
                    if (p == null)
                    {
                        continue;
                    }
                    
                    bool onCooldown = false;
                    
                    if (p is NumberPickup np)
                    {
                        onCooldown = np.OnCooldown;
                    }
                    
                    if (onCooldown)
                    {
                        continue;
                    }
                    
                    float d = Vector3.Distance(transform.position, p.transform.position);
                    
                    if (d < minDist) 
                    { 
                        minDist = d; 
                        nearest = p; 
                    }
                }
            }
            
            if (nearest != null)
            {
                sensor.AddObservation(minDist / MaxSensorDist);
                Vector3 dir = (Quaternion.Inverse(transform.rotation) * (nearest.transform.position - transform.position)).normalized;
                sensor.AddObservation(dir.x); 
                sensor.AddObservation(dir.z);
            }
            else 
            { 
                sensor.AddObservation(1.0f); 
                sensor.AddObservation(0f); 
                sensor.AddObservation(0f); 
            }
        }
        
        /// <summary>
        /// Callback for when the agent picks up an enemy flag.
        /// </summary>
        /// <param name="flag">The flag instance picked up.</param>
        private void HandleFlagPickedUp(Flag flag)
        {
            _hasFlag = true;
            // Reset base distance so reward shaping starts from the current position
            _closestDistanceToFriendlyBase = GetPathDistance(transform.position, _friendlyBasePosition);
            AddReward(0.1f);
        }

        /// <summary>
        /// Callback for when the agent drops an enemy flag.
        /// </summary>
        /// <param name="flag">The flag instance dropped.</param>
        private void HandleFlagDropped(Flag flag)
        {
            _hasFlag = false; 
            // Reset enemy flag distance so they can earn rewards for going back to it
            if (_enemyFlag != null)
            {
                _closestDistanceToEnemyFlag = GetPathDistance(transform.position, _enemyFlag.transform.position);
            }
                
            AddReward(-0.1f);
        }

        /// <summary>
        /// Callback for when the agent successfully captures an enemy flag.
        /// </summary>
        /// <param name="flag">The flag instance captured.</param>
        private void HandleFlagCaptured(Flag flag)
        {
            _hasFlag = false; 
            AddReward(1.0f); 
            CaptureTheFlagManager.Instance?.NotifyEpisodeEnd();
            EndEpisode();
        }

        /// <summary>
        /// Callback for when the agent successfully returns their own flag.
        /// </summary>
        /// <param name="flag">The flag instance returned.</param>
        private void HandleFlagReturned(Flag flag)
        {
            _hasFlag = false; 
            AddReward(0.1f);
        }

        /// <summary>
        /// Callback for when the agent successfully eliminates an enemy trooper.
        /// </summary>
        /// <param name="e">The trooper that was eliminated.</param>
        private void HandleEliminatedEnemy(Trooper e)
        {
            AddReward(0.1f);
        }

        /// <summary>
        /// Callback for when the agent successfully hits an enemy trooper.
        /// </summary>
        /// <param name="e">The trooper that was hit.</param>
        private void HandleHitEnemy(Trooper e)
        {
            AddReward(0.02f);
        }

        /// <summary>
        /// Callback for when the agent is eliminated by an enemy trooper.
        /// </summary>
        /// <param name="e">The trooper that eliminated this agent.</param>
        private void HandleEliminatedByEnemy(Trooper e)
        {
            AddReward(-0.2f); 
            CaptureTheFlagManager.Instance?.NotifyEpisodeEnd();
            EndEpisode();
        }
        
        /// <summary>
        /// Callback for when the agent collects a health pickup.
        /// </summary>
        /// <param name="pickup">The health pickup instance collected.</param>
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
        
        /// <summary>
        /// Callback for when the agent collects an ammo pickup.
        /// </summary>
        /// <param name="pickup">The ammo pickup instance collected.</param>
        private void HandleAmmoPickup(AmmoPickup pickup) 
        { 
            // 1. Calculate how much ammo we are missing (0.0 = full ammo, 1.0 - empty ammo)
             float missingAmmoPct = Mathf.Clamp01(1f - _trooper.Ammo / MaxAmmo);
             
             // 2. Quadratic (Exponential) Scaling: Squaring the percentage creates a curve.
             // This strongly discourages hoarding. If you are missing 50% ammo, you only get 25% of the reward. 
             // You only get the big reward when you are truly running on empty.
             float dynamicReward = 0.2f * (missingAmmoPct * missingAmmoPct);
             AddReward(dynamicReward);
        }
    }
}