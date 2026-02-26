using System.Collections.Generic;
using System.Linq;
using KaijuSolutions.Agents.Exercises.Cleaner;
using KaijuSolutions.Agents.Sensors;
using UnityEngine;

public class CleanerVisionSensor : KaijuVisionSensor<Floor>
{
    protected override IEnumerable<Floor> DefaultObservables()
    {
        // Default observables are dirty floors.
        // (floors from the generic we extend class from, dirty from the below filter)
        return base.DefaultObservables().Where(obj => obj.Dirty);
    }
}
