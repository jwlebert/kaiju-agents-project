using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors; 
using KaijuSolutions.Agents.Exercises.CTF;
using KaijuSolutions.Agents.Sensors;
using UnityEngine.InputSystem;
using KaijuSolutions.Agents;
using System;

namespace Samples.Kaiju_Agents._1._1._1.Exercise_4___Capture_the_Flag_ML
{
    public class TrooperMLAgent : Agent
    {
        private Component _trooperComp; 
        private Trooper _trooper;
        private KaijuAgent _kaijuAgent;
        private Rigidbody _rb;
        
        // Objective References
        private Flag _enemyFlag;
        private Flag _friendlyFlag;
        private Vector3 _friendlyBasePosition;
        
        // Local Flag Tracking
        private bool _hasFlag;
        
        // Sensors
        private TrooperEnemyVisionSensor _enemySensor;
        private AmmoVisionSensor _ammoSensor;
        private HealthVisionSensor _healthSensor;
        
        // Rewards Shaping
        private float _previousDistanceToEnemyFlag;
        private float _previousDistanceToFriendlyBase;
        
        // Normalization Constants
        private const float MaxHealth = 100f; 
        private const float MaxAmmo = 30f;
        private readonly float _maxMapDistance = 150f; 
        private const float MaxSensorDist = 20f;

        public override void Initialize()
        {
            SetupReferences();
        }

        private bool SetupReferences()
        {
            if (_kaijuAgent != null && _rb != null && _trooperComp != null) return true;

            _kaijuAgent = GetComponent<KaijuAgent>();
            _rb = GetComponent<Rigidbody>();
            
            // Assembly-Safe lookup for the Trooper script
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

            // Grab sensors using the native Kaiju Agent API
            _enemySensor = _kaijuAgent.GetSensor<TrooperEnemyVisionSensor>();
            _ammoSensor = _kaijuAgent.GetSensor<AmmoVisionSensor>();
            _healthSensor = _kaijuAgent.GetSensor<HealthVisionSensor>();
            
            bool isTeamOne = GetTrooperBool("TeamOne", true);

            // Find world objectives
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

            // Subscribe to game events
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
            _kaijuAgent.Stop();
            
            if (_enemyFlag != null)
                _previousDistanceToEnemyFlag = Vector3.Distance(transform.position, _enemyFlag.transform.position);
            
            _previousDistanceToFriendlyBase = Vector3.Distance(transform.position, _friendlyBasePosition);
        }
        
        public override void CollectObservations(VectorSensor sensor)
        {
            if (!SetupReferences())
            {
                for (int i = 0; i < 19; i++) sensor.AddObservation(0f);
                return;
            }

            // Clean, assignment-style data grabbing
            float health = GetTrooperValue("Health", 100f);
            float ammo = GetTrooperValue("Ammo", 30f);

            sensor.AddObservation(health / MaxHealth);
            sensor.AddObservation(ammo / MaxAmmo);
            sensor.AddObservation(_hasFlag ? 1.0f : 0.0f);
            
            if (_enemyFlag != null)
            {
                sensor.AddObservation(Vector3.Distance(transform.position, _enemyFlag.transform.position) / _maxMapDistance);
                Vector3 dirToFlag = (Quaternion.Inverse(transform.rotation) * (_enemyFlag.transform.position - transform.position)).normalized;
                sensor.AddObservation(dirToFlag.x);
                sensor.AddObservation(dirToFlag.z);
            }
            else { sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f); }

            if (_friendlyFlag != null)
            {
                sensor.AddObservation(Vector3.Distance(transform.position, _friendlyFlag.transform.position) / _maxMapDistance);
                Vector3 dirToFlag = (Quaternion.Inverse(transform.rotation) * (_friendlyFlag.transform.position - transform.position)).normalized;
                sensor.AddObservation(dirToFlag.x);
                sensor.AddObservation(dirToFlag.z);
            }
            else { sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f); }

            sensor.AddObservation(Vector3.Distance(transform.position, _friendlyBasePosition) / _maxMapDistance);

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

            AddPickupObs(sensor, _healthSensor);
            AddPickupObs(sensor, _ammoSensor);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (!SetupReferences()) return;

            var discreteActions = actions.DiscreteActions;
            
            // Movement
            float moveVal = 0;
            if (discreteActions[0] == 1) moveVal = 1f;
            else if (discreteActions[0] == 2) moveVal = -1f;
            _rb.linearVelocity = transform.forward * moveVal * _kaijuAgent.MoveSpeed;

            // Rotation
            float rotateVal = 0;
            if (discreteActions[1] == 1) rotateVal = 1f;
            else if (discreteActions[1] == 2) rotateVal = -1f;
            if (rotateVal != 0)
            {
                transform.Rotate(Vector3.up, rotateVal * _kaijuAgent.LookSpeed * Time.fixedDeltaTime);
            }

            // Combat
            if (discreteActions[2] == 1)
            {
                _trooperComp.SendMessage("Attack", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                _trooperComp.SendMessage("StopAttacking", SendMessageOptions.DontRequireReceiver);
            }
            
            // Reward Shaping (Anti-Wiggle Logic)
            AddReward(-0.0005f);
            if (!_hasFlag && _enemyFlag != null)
            {
                float currentDist = Vector3.Distance(transform.position, _enemyFlag.transform.position);
                // Only reward for breaking NEW records of closeness
                if (currentDist < _previousDistanceToEnemyFlag) 
                {
                    AddReward(0.001f);
                    _previousDistanceToEnemyFlag = currentDist;
                }
            }
            else if (_hasFlag)
            {
                float currentDist = Vector3.Distance(transform.position, _friendlyBasePosition);
                // Only reward for breaking NEW records of closeness to base
                if (currentDist < _previousDistanceToFriendlyBase) 
                {
                    AddReward(0.002f);
                    _previousDistanceToFriendlyBase = currentDist;
                }
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
        
        private void HandleFlagPickedUp(Flag flag) { _hasFlag = true; }
        private void HandleFlagDropped(Flag flag)  { _hasFlag = false; }
        private void HandleFlagCaptured(Flag flag) { _hasFlag = false; AddReward(5.0f); EndEpisode(); }
        private void HandleFlagReturned(Flag flag) { _hasFlag = false; AddReward(2.0f); }
        private void HandleEliminatedEnemy(Trooper e) { AddReward(1.0f); }
        private void HandleHitEnemy(Trooper e) { AddReward(0.1f); }
        private void HandleEliminatedByEnemy(Trooper e) { AddReward(-1.0f); EndEpisode(); }
    }
}
