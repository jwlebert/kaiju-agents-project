using KaijuSolutions.Agents;
using KaijuSolutions.Agents.Actuators;
using KaijuSolutions.Agents.Extensions;
using KaijuSolutions.Agents.Movement;
using KaijuSolutions.Agents.Sensors;
using UnityEngine;

public class CleanerController : KaijuController
{
    /// <summary>
    /// Cache the sensor.
    /// </summary>
    private CleanerVisionSensor _sensor;
    
    /// <summary>
    /// Cache the actuator.
    /// </summary>
    private CleanerActuator _actuator;

    /// <summary>
    /// Get and store sensor and actuator, and start searching.
    /// </summary>
    protected override void OnEnabled()
    {
        _sensor = Agent.GetSensor<CleanerVisionSensor>();
        _actuator = Agent.GetActuator<CleanerActuator>();
        
        StartSearching();
    }

    /// <summary>
    /// Turn on sensor, wander while avoiding obstacles, until we sense a dirty tile.
    /// </summary>
    private void StartSearching()
    {
        // Look where we move.
        Agent.LookTransform = null;
        
        // Automatically sense tiles, triggering OnSense().
        _sensor.automatic = true;

        Agent.Wander();

        // Avoid obstacles, without clearing the Wander instruction.
        Agent.ObstacleAvoidance(clear: false);
    }

    /// <summary>
    /// Triggered when a dirty floor is sensed; directs agent to seek to floor to clean it.
    /// </summary>
    /// <param name="sensor">The sensor which triggered this event.</param>
    protected override void OnSense(KaijuSensor sensor)
    {
        // Make sure we actually see a box.
        // Making sure it's the same sensor is unnecessary (there's only one); but doesn't hurt.
        if (sensor != _sensor || _sensor.ObservedCount < 1) return;
        
        // Don't need to look for now
        _sensor.automatic = false; 
        
        // Find the closest dirty detected floor, and go to it.
        // This will cancel the Agent.Wander() from StartSearching().
        Transform nearest = Position.Nearest(_sensor.Observed, out float _);
        Agent.Seek(nearest, 0.1f);
        
        // Look at the floor we found.
        Agent.LookTransform = nearest;
    }

    /// <summary>
    /// When we stop, check if it was a seek; if it is, then we stopped at a floor we sensed, so we clean it.
    /// </summary>
    /// <param name="movement"></param>
    protected override void OnMovementStopped(KaijuMovement movement)
    {
        // Check if we were Seeking (i.e., if we were in OnSense)
        if (movement is KaijuSeekMovement)
        {
            // Start cleaning the floor
            _actuator.Begin();
        }
    }

    /// <summary>
    /// Once we've cleaned the floor, we start looking for another floor.
    /// </summary>
    /// <param name="actuator">The actuator which just cleaned the tile.</param>
    protected override void OnActuatorDone(KaijuActuator actuator)
    {
        // Make sure it's the right actuator. We only have one; unnecessary, but doesn't hurt.
        if (actuator != _actuator) return;
        
        // Start looking for another floor.
        StartSearching();
    }

    /// <summary>
    /// If something goes wrong, and the actuator fails, we just start again.
    /// </summary>
    /// <param name="actuator">The actuator which just cleaned the tile.</param>
    protected override void OnActuatorFailed(KaijuActuator actuator)
    {
        // This should not happen. But, if it does, we just keep going!
        StartSearching();
    }
}
