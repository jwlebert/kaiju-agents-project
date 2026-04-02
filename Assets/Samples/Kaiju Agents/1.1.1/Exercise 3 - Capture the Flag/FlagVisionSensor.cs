using System.Collections.Generic;
using KaijuSolutions.Agents.Sensors;
using UnityEngine;

namespace KaijuSolutions.Agents.Exercises.CTF
{
    /// <summary>
    /// Sensor for <see cref="FlagOld"/>s.
    /// </summary>
    [HelpURL("https://agents.kaijusolutions.ca/manual/capture-the-flag.html#flag-vision-sensor")]
    [AddComponentMenu("Kaiju Solutions/Agents/Exercises/Capture the Flag/Flag Vision Sensor", 32)]
    public class FlagVisionSensor : KaijuVisionSensor<FlagOld>
    {
        /// <summary>
        /// If there are no explicitly defined observable objects, define how to query for default observables.
        /// </summary>
        /// <returns>Both <see cref="FlagOld"/>s.</returns>
        protected override IEnumerable<FlagOld> DefaultObservables()
        {
            return FlagOld.Flags;
        }
    }
}