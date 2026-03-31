using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors; 
using KaijuSolutions.Agents.Exercises.CTF;


public class TrooperMLAgent : Agent
{
    private Trooper trooper;
    
    // Objective References
    private Flag enemyFlag;
    private Flag friendlyFlag;
    private Vector3 friendlyBasePosition;
    
    // Local Flag Tracking
    private bool hasFlag = false;
    
    // Reward Shaping
    private float previousDistanceToEnemyFlag;
    private float previousDistanceToFriendlyBase;
    
    // Normalization constants
    private const float MAX_HEALTH = 100f; 
    private const float MAX_AMMO = 30f;
    private float maxMapDistance = 150f; // Used to normalize distance observations

    public override void Initialize()
    {
        // Grab reference to the trooper component
        trooper = GetComponent<Trooper>();
        
        // Find all flags in the scene
        Flag[] flags = FindObjectsByType<Flag>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (Flag f in flags)
        {
            if (f.TeamOne == trooper.TeamOne)
            {
                friendlyFlag = f;
                // A team's base is exactly where their flag spawns at the start
                friendlyBasePosition = f.transform.position; 
            }
            else
            {
                enemyFlag = f;
            }
        }
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        if (trooper == null) return;
        
        trooper.OnFlagCaptured += HandleFlagCaptured;
        trooper.OnFlagReturned += HandleFlagReturned;
        trooper.OnEliminatedTrooper += HandleEliminatedEnemy;
        trooper.OnEliminatedByTrooper += HandleEliminatedByEnemy;
        trooper.OnHitTrooper += HandleHitEnemy;
        trooper.OnFlagPickedUp += HandleFlagPickedUp;
        trooper.OnFlagDropped += HandleFlagDropped;
    }
    
    protected override void OnDisable()
    {
        base.OnDisable();
        if (trooper == null) return;

        // Unsubscribe to prevent memory leaks
        trooper.OnFlagCaptured -= HandleFlagCaptured;
        trooper.OnFlagReturned -= HandleFlagReturned;
        trooper.OnEliminatedTrooper -= HandleEliminatedEnemy;
        trooper.OnEliminatedByTrooper -= HandleEliminatedByEnemy;
        trooper.OnHitTrooper -= HandleHitEnemy;
        trooper.OnFlagPickedUp += HandleFlagPickedUp;
        trooper.OnFlagDropped += HandleFlagDropped;
    }
    
    public override void OnEpisodeBegin()
    {
        // Reset our reward shaping distances when an agent respawns
        if (enemyFlag != null)
            previousDistanceToEnemyFlag = Vector3.Distance(transform.position, enemyFlag.transform.position);
        
        previousDistanceToFriendlyBase = Vector3.Distance(transform.position, friendlyBasePosition);
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // Internal State
        sensor.AddObservation(trooper.Health / MAX_HEALTH);
        sensor.AddObservation(trooper.Ammo / MAX_AMMO);
        sensor.AddObservation(hasFlag ? 1.0f : 0.0f);
        
        // Spatial Awareness (based on dungeon example)
        if (enemyFlag != null)
        {
            sensor.AddObservation(Vector3.Distance(transform.position, enemyFlag.transform.position) / maxMapDistance);
            
            Vector3 dirToFlag = (enemyFlag.transform.position - transform.position).normalized;
            sensor.AddObservation(dirToFlag.x);
            sensor.AddObservation(dirToFlag.z);
        }
        else
        {
            // Safe fallbacks if a flag is missing
            sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f);
        }

        // Distance to Friendly Base
        sensor.AddObservation(Vector3.Distance(transform.position, friendlyBasePosition) / maxMapDistance);
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        var discreteActions = actions.DiscreteActions;

        // --- Movement ---
        int moveAction = discreteActions[0];
        if (moveAction == 1)
        {
            trooper.Agent.Control = new Vector2(0, 1f);// Move Forward y axis
        }
        else if (moveAction == 2)
        {
            trooper.Agent.Control = new Vector2(0, -1f); // Move Backward (Y axis)
        }
        else
        {
            trooper.Agent.Control = Vector2.zero; // Idle
        }

        // --- Rotation ---
        int rotateAction = discreteActions[1];
        if (rotateAction == 1)
        {
            trooper.Agent.Spin = 1f; // Turn Right
        }
        else if (rotateAction == 2)
        {
            trooper.Agent.Spin = -1f; // Turn Left
        }
        else
        {
            trooper.Agent.Spin = null; // Idle
        }

        // --- Shooting ---
        int shootAction = discreteActions[2];
        if (shootAction == 1)
        {
            trooper.Attack();
        }
        
        // Reward Shaping (inspiration from Dungeon example)
        AddReward(-0.0005f); // Tiny existential penalty to encourage fast action
        
        if (!hasFlag && enemyFlag != null)
        {
            float currentDistToFlag = Vector3.Distance(transform.position, enemyFlag.transform.position);
            if (currentDistToFlag < previousDistanceToEnemyFlag)
            {
                AddReward(0.001f); 
            }
            previousDistanceToEnemyFlag = currentDistToFlag;
        }
        else if (hasFlag)
        {
            float currentDistToBase = Vector3.Distance(transform.position, friendlyBasePosition);
            if (currentDistToBase < previousDistanceToFriendlyBase)
            {
                AddReward(0.002f); 
            }
            previousDistanceToFriendlyBase = currentDistToBase;
        }
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // This allows to control the agent with the keyboard for testing
        // before you start the Python training server.
        var discreteActionsOut = actionsOut.DiscreteActions;
        
        // Reset defaults
        discreteActionsOut[0] = 0; 
        discreteActionsOut[1] = 0; 
        discreteActionsOut[2] = 0;

        // Movement Keyboard Mapping (W/S)
        if (Input.GetKey(KeyCode.W)) discreteActionsOut[0] = 1;
        else if (Input.GetKey(KeyCode.S)) discreteActionsOut[0] = 2;

        // Rotation Keyboard Mapping (A/D)
        if (Input.GetKey(KeyCode.D)) discreteActionsOut[1] = 1;
        else if (Input.GetKey(KeyCode.A)) discreteActionsOut[1] = 2;

        // Shoot Keyboard Mapping (Spacebar)
        if (Input.GetKey(KeyCode.Space)) discreteActionsOut[2] = 1;
    }
    
    // Rewards
    private void HandleFlagPickedUp(Flag flag) { hasFlag = true; }
    private void HandleFlagDropped(Flag flag)  { hasFlag = false; }
    private void HandleFlagCaptured(Flag flag)
    {
        hasFlag = false;
        AddReward(2.0f); // Massive reward for the ultimate objective
        EndEpisode();    // End the learning cycle for this agent
    }

    private void HandleFlagReturned(Flag flag)
    {
        hasFlag = false;
        AddReward(1.0f);
    }

    private void HandleEliminatedEnemy(Trooper eliminatedEnemy)
    {
        AddReward(0.5f);
    }

    private void HandleHitEnemy(Trooper hitEnemy)
    {
        AddReward(0.1f);
    }

    private void HandleEliminatedByEnemy(Trooper eliminatedBy)
    {
        AddReward(-1.0f); // Big penalty for dying
        EndEpisode();
    }
    
}
