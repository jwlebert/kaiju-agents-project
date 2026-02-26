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
            Agent.Pursue(pos, 0.1f);

            // Look at mate as we move to it.
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
            // (better for moving targets since it predicts future position)
            Agent.Pursue(pos, distance: 0.1f);

            // Face the prey while hunting 
            Agent.LookTransform = pos;
        }
        
        /// <summary>
        /// Flee from microbe of different species using Evade.
        /// </summary>
        private void FleeHunter(Microbe hunter)
        {
            // Evade predicts where the hunter is going and moves in opposite direction
            Agent.Evade(hunter.transform, distance: 2.0f);
            
            // Look at where we are running, not the hunter
            Agent.LookTransform = null; 
        }
        
        /// <summary> 
        /// Called every frame to envoke the current state's action
        /// </summary> 
        public void Step(MicrobeState state) {
            switch (state)
            {
                case MicrobeState.Wandering:
                    this.StartWandering();
                    break;
                case MicrobeState.Foraging:
                    // Only seek energy if energy is detected
                    if (!this.energyPos) return;
                    this.SeekFood(this.energyPos);
                    break;
                case MicrobeState.Mating:
                    // Only seek mate if mate detected
                    if (!this.mate) return;
                    this.SeekMate(this.mate);
                    break;
                case MicrobeState.Hunting:
                    // Only hunt prey if prey is detected
                    if (!this.prey) return;
                    this.HuntEnemy(this.prey);
                    break;
                case MicrobeState.Fleeing:
                    // Only flee if hunter is detected
                    if (!this.hunter) return;
                    this.FleeHunter(this.hunter);
                    break;
            }
        }
    }
}