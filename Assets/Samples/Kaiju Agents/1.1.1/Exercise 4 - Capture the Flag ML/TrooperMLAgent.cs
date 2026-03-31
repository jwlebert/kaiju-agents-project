using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors; 
using KaijuSolutions.Agents.Exercises.CTF;
using KaijuSolutions.Agents.Sensors;
using UnityEngine.InputSystem;
using KaijuSolutions.Agents;
using System;
using System.Reflection;

namespace Samples.Kaiju_Agents._1._1._1.Exercise_4___Capture_the_Flag_ML
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
        // Reference to the base component, used when direct casting fails due to assembly clash.
        private Component _trooperComp; 
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
        
        // Used for anti-wiggling reward shaping.
        private float _previousDistanceToEnemyFlag;
        private float _previousDistanceToFriendlyBase;
        
        private const float MaxHealth = 100f; 
        private const float MaxAmmo = 30f;
        private readonly float _maxMapDistance = 150f; 
        private const float MaxSensorDist = 20f;

        public override void Initialize()
        {
            SetupReferences();
        }

        /// <summary>
        /// Lazy-loads necessary components. Prevents NullReferenceExceptions during 
        /// early spawning frames before Unity has finished attaching all scripts.
        /// </summary>
        private bool SetupReferences()
        {
            if (_kaijuAgent != null && _rb != null && _trooperComp != null) return true;

            _kaijuAgent = GetComponent<KaijuAgent>();
            _rb = GetComponent<Rigidbody>();
            
            // Assembly-Safe lookup for the Trooper script
            // TODO: Remove this search and just use GetComponent<Trooper>() once 
            // all Trooper scripts are consolidated into a single assembly/namespace.
            var allScripts = GetComponents<MonoBehaviour>();
            foreach (var s in allScripts)
            {
                if (s == null) continue;
                if (s.GetType().Name == "Trooper")
                {
                    _trooperComp = s;
                    _trooper = s as Trooper; 
                    break;
                }
            }

            if (_kaijuAgent == null || _rb == null || _trooperComp == null) return false;

            // // Force old logic off
            // var oldBrain = GetComponent("TrooperBrain") as MonoBehaviour;
            // if (oldBrain != null) oldBrain.enabled = false;
            // var oldController = GetComponent("TrooperController") as MonoBehaviour;
            // if (oldController != null) oldController.enabled = false;

            // if (_rb != null)
            // {
            //     _rb.isKinematic = false; 
            //     _rb.useGravity = false;
            //     _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
            //     _rb.linearDamping = 0f; 
            //     _rb.angularDamping = 5f;
            // }
            //
            // _kaijuAgent.MoveSpeed = 10f;
            // _kaijuAgent.MoveAcceleration = 100f;
            // _kaijuAgent.AutoRotate = false;

            _enemySensor = _kaijuAgent.GetSensor<TrooperEnemyVisionSensor>();
            _ammoSensor = _kaijuAgent.GetSensor<AmmoVisionSensor>();
            _healthSensor = _kaijuAgent.GetSensor<HealthVisionSensor>();
            
            bool isTeamOne = GetTrooperBool("TeamOne", true);

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
                _trooper.OnFlagPickedUp += HandleFlagPickedUp;
                _trooper.OnFlagDropped += HandleFlagDropped;
                _trooper.OnFlagCaptured += HandleFlagCaptured;
                _trooper.OnFlagReturned += HandleFlagReturned;
                _trooper.OnEliminatedTrooper += HandleEliminatedEnemy;
                _trooper.OnEliminatedByTrooper += HandleEliminatedByEnemy;
                _trooper.OnHitTrooper += HandleHitEnemy;
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
            _trooper.OnHitTrooper -= HandleHitEnemy;
        }
        
        public override void OnEpisodeBegin()
        {
            if (!SetupReferences()) return;
            
            // Stop any leftover physics momentum from the previous episode.
            _kaijuAgent.Stop();
            
            // Baseline the distances so reward shaping only rewards NEW progress.
            if (_enemyFlag != null)
                _previousDistanceToEnemyFlag = Vector3.Distance(transform.position, _enemyFlag.transform.position);
            
            _previousDistanceToFriendlyBase = Vector3.Distance(transform.position, _friendlyBasePosition);
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
                for (int i = 0; i < 19; i++) sensor.AddObservation(0f);
                return;
            }

            // 1-3. Internal State
            float health = GetTrooperValue("Health", 100f);
            float ammo = GetTrooperValue("Ammo", 30f);

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
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (!SetupReferences()) return;

            // 3 Branches: [Movement(3), Rotation(3), Combat(2)]
            var discreteActions = actions.DiscreteActions;
            
            // Branch 0: Movement (0=Idle, 1=Forward, 2=Backward)
            float moveVal = 0;
            if (discreteActions[0] == 1) moveVal = 1f;
            else if (discreteActions[0] == 2) moveVal = -1f;
            // Override Kaiju control and apply physical velocity directly
            _rb.linearVelocity = transform.forward * moveVal * _kaijuAgent.MoveSpeed;

            // Branch 1: Rotation (0=Idle, 1=Right, 2=Left)
            float rotateVal = 0;
            if (discreteActions[1] == 1) rotateVal = 1f;
            else if (discreteActions[1] == 2) rotateVal = -1f;
            if (rotateVal != 0)
            {
                transform.Rotate(Vector3.up, rotateVal * _kaijuAgent.LookSpeed * Time.fixedDeltaTime);
            }

            // Branch 2: Combat (0=Idle, 1=Shoot)
            // Using SendMessage as a safe fallback in case of assembly mismatch on the Trooper component
            if (discreteActions[2] == 1)
            {
                _trooperComp.SendMessage("Attack", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                _trooperComp.SendMessage("StopAttacking", SendMessageOptions.DontRequireReceiver);
            }
            
            // --- Exploit-Proof Reward Shaping ---
        
            if (!_hasFlag && _enemyFlag != null)
            {
                float currentDist = Vector3.Distance(transform.position, _enemyFlag.transform.position);
            
                // Only trigger if they walked closer than they have EVER been this episode
                if (currentDist < _previousDistanceToEnemyFlag) 
                {
                    // Reward based on how much new ground they covered
                    float distanceCovered = _previousDistanceToEnemyFlag - currentDist;
                    AddReward(distanceCovered * 0.2f);
                
                    // Shrink the safe zone (update the high score)
                    _previousDistanceToEnemyFlag = currentDist; 
                }
            }
            else if (_hasFlag && _friendlyFlag != null)
            {
                float currentDist = Vector3.Distance(transform.position, _friendlyBasePosition);
            
                if (currentDist < _previousDistanceToFriendlyBase) 
                {
                    float distanceCovered = _previousDistanceToFriendlyBase - currentDist;
                    AddReward(distanceCovered * 0.05f);
                
                    _previousDistanceToFriendlyBase = currentDist;
                }
            }
            // Add a tiny penalty to encourage the agent to finish the task quickly.
            // Assuming a Max Step of 5000, this adds a -1.0 total penalty if they time out.
            AddReward(-1f / 5000f);
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
        
        private float GetTrooperValue(string propName, float fallback)
        {
            if (_trooper != null)
            {
                var p = _trooper.GetType().GetProperty(propName);
                if (p != null) return Convert.ToSingle(p.GetValue(_trooper));
            }
            if (_trooperComp != null)
            {
                var p = _trooperComp.GetType().GetProperty(propName);
                if (p != null) return Convert.ToSingle(p.GetValue(_trooperComp));
            }
            return fallback;
        }

        private bool GetTrooperBool(string propName, bool fallback)
        {
            if (_trooper != null)
            {
                var p = _trooper.GetType().GetProperty(propName);
                if (p != null) return (bool)p.GetValue(_trooper);
            }
            if (_trooperComp != null)
            {
                var p = _trooperComp.GetType().GetProperty(propName);
                if (p != null) return (bool)p.GetValue(_trooperComp);
            }
            return fallback;
        }

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
            _previousDistanceToFriendlyBase = Vector3.Distance(transform.position, _friendlyBasePosition);
            AddReward(1.0f); // BIG reward for grabbing the enemy flag!
        }

        private void HandleFlagDropped(Flag flag)
        {
            _hasFlag = false; 
            // Reset enemy flag distance so they can earn rewards for going back to it
            if (_enemyFlag != null)
                _previousDistanceToEnemyFlag = Vector3.Distance(transform.position, _enemyFlag.transform.position);
        }
        private void HandleFlagCaptured(Flag flag) { _hasFlag = false; AddReward(5.0f); EndEpisode(); }

        private void HandleFlagReturned(Flag flag)
        {
            _hasFlag = false; AddReward(2.0f); 
            AddReward(0.5f); // Reward for saving your own flag
        }
        private void HandleEliminatedEnemy(Trooper e) { AddReward(1.0f); }

        private void HandleHitEnemy(Trooper e)
        {
            AddReward(0.2f); // Slightly higher reward for combat success
        }
        private void HandleEliminatedByEnemy(Trooper e) { AddReward(-1.0f); EndEpisode(); }
    }
}
