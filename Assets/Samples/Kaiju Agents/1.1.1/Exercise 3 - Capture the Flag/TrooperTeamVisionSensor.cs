using System.Collections.Generic;
using UnityEngine;

namespace KaijuSolutions.Agents.Exercises.CTF
{
    /// <summary>
    /// Sensor to get all friendly <see cref="TrooperOld"/>s.
    /// </summary>
    [AddComponentMenu("Kaiju Solutions/Agents/Exercises/Capture the Flag/Trooper Team Vision Sensor", 29)]
    [HelpURL("https://agents.kaijusolutions.ca/manual/capture-the-flag.html#trooper-team-vision-sensor")]
    public class TrooperTeamVisionSensor : TrooperVisionSensor
    {
        /// <summary>
        /// If there are no explicitly defined observable objects, define how to query for default observables.
        /// </summary>
        /// <returns>All active <see cref="TrooperOld"/>s on the same team.</returns>
        protected override IEnumerable<TrooperOld> DefaultObservables()
        {
            return Attached == null ? base.DefaultObservables() : Attached.TeamOne ? TrooperOld.AllOne : TrooperOld.AllTwo;
        }
    }
}