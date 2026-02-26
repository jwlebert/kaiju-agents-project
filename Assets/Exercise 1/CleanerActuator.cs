using KaijuSolutions.Agents.Actuators;
using KaijuSolutions.Agents.Exercises.Cleaner;
using UnityEngine;

public class CleanerActuator : KaijuAttackActuator
{
    /// <summary>
    /// Cleans the floor which the ray collides with.
    /// </summary>
    /// <param name="hit">The raycast hit for the floor we are cleaning.</param>
    /// <param name="t">The transform of the floor we hit.</param>
    /// <returns>Boolean, corresponding to if the hit was a success or not.</returns>
    protected override bool HandleHit(RaycastHit hit, Transform t)
    {
        // Get the floor from its transform.
        if (t.TryGetComponent(out Floor flr))
        {
            // Clean it and return true.
            flr.Clean();
            return true;
        }
        
        return false;
    }
}
