using UnityEngine;

namespace KaijuSolutions.Agents.Exercises.Microbes
{
    public class FiniteStateMachine : KaijuController
    {
        public Transform energyPos;
        public Microbe mate;
        public Microbe prey;
        public Microbe hunter;
        
        /// <summary>
        /// Start wandering, to find food, mates, or prey.
        /// </summary>
        private void StartWandering()
        {
            // Look where we move.
            Agent.LookTransform = null;

            Agent.Wander();

            // Avoid obstacles, without clearing the Wander instruction.
            Agent.ObstacleAvoidance(clear: false);
        }
        
        /// <summary>
        /// Seek energy to consume.
        /// </summary>
        private void SeekFood(Transform food)
        {
            // Go to the food with Seek.
            Agent.Seek(food, 0.1f);

            // Look at food as we move to it.
            Agent.LookTransform = food;
        }
        
        /// <summary>
        /// Seek same species microbe to mate.
        /// </summary>
        private void SeekMate(Microbe mate)
        {
            // Get position of mate.
            Transform pos = mate.transform;
            
            // Go to the food with Seek.
            Agent.Seek(pos, 0.1f);

            // Look at food as we move to it.
            Agent.LookTransform = pos;
        }

        /// <summary>
        /// Hunt microbe of different species using Pursue.
        /// </summary>
        private void HuntEnemy(Microbe prey)
        {
            // Get position of prey
            Transform pos = prey.transform;
            
            // Hunt prey using Pursue
            Agent.Pursue(pos, distance: 0.1f);

            // Face the prey while hunting 
            Agent.LookTransform = pos;
        }
        
        
        /// <summary> 
        /// Updates state
        /// </summary> 
        public void Step(MicrobeState state) {
            switch (state)
            {
                case MicrobeState.Wandering:
                    this.StartWandering();
                    break;
                case MicrobeState.Foraging:
                    // Seek energy if the position is set
                    if (!this.energyPos) return;
                    this.SeekFood(this.energyPos);
                    break;
                case MicrobeState.Mating:
                    // Seek mate if their position is set
                    if (!this.mate) return;
                    this.SeekMate(this.mate);
                    break;
                case MicrobeState.Hunting:
                    // Hunt prey if position is set
                    if (!this.prey) return;
                    this.HuntEnemy(this.prey);
                    break;
            }
        }
    }
}