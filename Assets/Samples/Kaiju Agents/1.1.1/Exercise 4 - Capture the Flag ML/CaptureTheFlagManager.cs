using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using UnityEngine.AI; 
using Unity.AI.Navigation;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

#endif
namespace KaijuSolutions.Agents.Exercises.CTF.ML
{
    /// <summary>
    /// Manager for <see cref="Trooper"/>s to play capture the flag.
    /// </summary>
    [DisallowMultipleComponent]
    [HelpURL("https://agents.kaijusolutions.ca/manual/capture-the-flag.html#capture-the-flag-manager")]
    [AddComponentMenu("Kaiju Solutions/Agents/Exercises/Capture the Flag/Capture the Flag Manager", 31)]
    public class CaptureTheFlagManager : KaijuGlobalController
    {
        
        [Header("Curriculum - Map Toggles")]
        public GameObject innerWalls;
        public GameObject trainingWalls; // Small boundaries for Level 1
        public GameObject level0Walls;   // New: Even smaller "Tiny" boundaries for Level 0
        public GameObject allPickups; // Drag the parent "Pickups" object here
        public NavMeshSurface navMeshSurface; // Drag your 'Navigation Mesh' object here

        [Header("Curriculum - Spawns")]
        public GameObject level0Spawns; // New: Very close to flag (3m)
        public GameObject centerSpawns; // Parent object holding your close spawn points
        public GameObject baseSpawns;   // Parent object holding your far spawn points

        [Header("Curriculum - Flags")]
        public Transform teamOneFlag;
        public Transform teamTwoFlag;
        
        [Header("Curriculum - Flag Placeholders")]
        public Transform teamOneCenterPos;
        public Transform teamOneBasePos;
        public Transform teamTwoCenterPos;
        public Transform teamTwoBasePos;
        
        /// <summary>
        /// The singleton manager instance.
        /// </summary>
        private static CaptureTheFlagManager _instance;
        
        /// <summary>
        /// The singleton manager instance.
        /// </summary>
        public static CaptureTheFlagManager Instance
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return null;
                }
#endif
                if (_instance != null)
                {
                    return _instance;
                }
                
                _instance = FindAnyObjectByType<CaptureTheFlagManager>();
                return _instance;
            }
        }
#if UNITY_EDITOR
        /// <summary>
        /// Handle manually resetting the domain.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitOnPlayMode()
        {
            Domain();
            EditorApplication.playModeStateChanged -= Domain;
            EditorApplication.playModeStateChanged += Domain;
        }
        
        /// <summary>
        /// Handle manually resetting the domain.
        /// </summary>
        /// <param name="state">The current editor state change.</param>
        private static void Domain(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode)
            {
                return;
            }
            
            EditorApplication.playModeStateChanged -= Domain;
            Domain();
        }
        
        /// <summary>
        /// Handle manually resetting the domain.
        /// </summary>
        private static void Domain()
        {
            _instance = null;
        }
#endif
        /// <summary>
        /// The prefab for <see cref="Trooper"/>s.
        /// </summary>
        [Header("Troopers")]
        [Tooltip("The prefab for troopers.")]
        [SerializeField]
        private KaijuAgent prefab;
        
        /// <summary>
        /// The maximum and starting <see cref="Trooper.Health"/> of <see cref="Trooper"/>s.
        /// </summary>
        public static int Health
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return 0;
                }
#endif
                return Instance != null ? Instance.health : 0;
            }
        }
        
        /// <summary>
        /// The maximum and starting <see cref="Trooper.Health"/> of <see cref="Trooper"/>s.
        /// </summary>
        [Tooltip("The maximum and starting health of troopers.")]
        [Min(1)]
        [SerializeField]
        private int health = 100;
        
        /// <summary>
        /// The damage the <see cref="BlasterActuator"/>s deal to <see cref="Trooper"/>s.
        /// </summary>
        public static int Damage
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return 0;
                }
#endif
                return Instance != null ? Instance.damage : 0;
            }
        }
        
        /// <summary>
        /// The damage the <see cref="BlasterActuator"/>s deal to <see cref="Trooper"/>s.
        /// </summary>
        [Tooltip("The damage the blasters deals to troopers.")]
        [Min(1)]
        [SerializeField]
        private int damage = 10;
        
        /// <summary>
        /// The maximum and starting <see cref="Trooper.Ammo"/> of <see cref="Trooper"/>s for their blaster.
        /// </summary>
        public static int Ammo
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return 0;
                }
#endif
                return Instance != null ? Instance.ammo : 0;
            }
        }
        
        /// <summary>
        /// The maximum and starting <see cref="Trooper.Ammo"/> of <see cref="Trooper"/>s for their blaster.
        /// </summary>
        [Tooltip("The maximum and starting ammo of troopers for their blaster.")]
        [Min(1)]
        [SerializeField]
        private int ammo = 30;
        
        /// <summary>
        /// The cooldown timer for <see cref="HealthPickup"/>s and <see cref="AmmoPickup"/>s.
        /// </summary>
        public static float Cooldown
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return 0;
                }
#endif
                return Instance != null ? Instance.cooldown : 0f;
            }
        }
        
        /// <summary>
        /// The cooldown timer for <see cref="HealthPickup"/>s and <see cref="AmmoPickup"/>s.
        /// </summary>
        [Tooltip("The cooldown timer for health and ammo pickups.")]
        [Min(float.Epsilon)]
        [SerializeField]
        private float cooldown = 10;
        
        /// <summary>
        /// How close to a <see cref="Trooper"/>'s own base to capture a <see cref="Flag"/> they are carrying.
        /// </summary>
        public static float CaptureDistance
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return 0;
                }
#endif
                return Instance != null ? Instance.captureDistance : 0f;
            }
        }
        
        /// <summary>
        /// How close to a <see cref="Trooper"/>'s own base to capture a <see cref="Flag"/> they are carrying.
        /// </summary>
        [Tooltip("How close to a trooper's own base to capture a flag they are carrying.")]
        [Min(float.Epsilon)]
        [SerializeField]
        private float captureDistance = 1f;
        
        /// <summary>
        /// The time in seconds for <see cref="Trooper"/>s to respawn.
        /// </summary>
        public static float Respawn
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return 0;
                }
#endif
                return Instance != null ? Instance.respawn : 0f;
            }
        }
        
        /// <summary>
        /// The time in seconds for <see cref="Trooper"/>s to respawn.
        /// </summary>
        [Header("Spawning")]
        [Tooltip("The time in seconds for troopers to respawn.")]
        [Min(0)]
        [SerializeField]
        private float respawn = 5;
        
        /// <summary>
        /// The number of <see cref="Trooper"/>s per team.
        /// </summary>
        public static int Size
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return 0;
                }
#endif
                return Instance != null ? Instance.size : 0;
            }
        }
        
        /// <summary>
        /// The number of <see cref="Trooper"/>s per team.
        /// </summary>
        [Tooltip("The number of troopers per team.")]
        [Min(1)]
        [SerializeField]
        private int size = 11;
        
        /// <summary>
        /// The color for team one.
        /// </summary>
        public static Color ColorOne
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return Color.red;
                }
#endif
                return Instance != null ? Instance.colorOne : Color.red;
            }
        }
        
        /// <summary>
        /// The color for team one.
        /// </summary>
        [Header("Colors")]
        [Tooltip("The color for team one.")]
        [SerializeField]
        private Color colorOne = Color.red;
        
        /// <summary>
        /// The color for team two.
        /// </summary>
        public static Color ColorTwo
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return Color.blue;
                }
#endif
                return Instance != null ? Instance.colorTwo : Color.blue;
            }
        }
        
        /// <summary>
        /// The color for team two.
        /// </summary>
        [Tooltip("The color for team two.")]
        [SerializeField]
        private Color colorTwo = Color.blue;
        
        /// <summary>
        /// Respawn timers for team one.
        /// </summary>
        private readonly List<float> _respawnsOne = new();
        
        /// <summary>
        /// Respawn timers for team two.
        /// </summary>
        private readonly List<float> _respawnsTwo = new();

        protected override void OnEnable()
        {
            base.OnEnable();
    
            // Nothing to do if this is already the singleton.
            if (_instance == this) return;
    
            // If there is a singleton but this is not it, destroy this.
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
    
            _instance = this;

            // Subscribe to Academy reset for Curriculum Learning
            Academy.Instance.OnEnvironmentReset += ApplyCurriculum;
    
            // Note: We don't call Spawn() in the while loops here anymore. 
            // ApplyCurriculum() will handle the first spawn immediately when the episode starts!
        }

        protected override void OnDisable()
        {
            base.OnDisable();
    
            // Clear out the lists
            _respawnsOne.Clear();
            _respawnsTwo.Clear();
    
            // Unsubscribe from the ML-Agents Academy to prevent memory leaks
            if (Academy.IsInitialized)
            {
                Academy.Instance.OnEnvironmentReset -= ApplyCurriculum;
            }
    
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        private void ApplyCurriculum()
        {
            // 1. Get the current lesson level from the YAML (Defaults to 5 for playing in Editor)
            int level = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("map_level", 5f);

            // 2. Destroy any existing troopers to prevent glitches between episodes
            foreach (var trooper in Trooper.AllOne.ToArray()) { DestroyImmediate(trooper.gameObject); }
            foreach (var trooper in Trooper.AllTwo.ToArray()) { DestroyImmediate(trooper.gameObject); }

            // Default states
            innerWalls.SetActive(false);
            if (trainingWalls != null) trainingWalls.SetActive(false);
            if (level0Walls != null) level0Walls.SetActive(false);
            allPickups.SetActive(false);
            level0Spawns.SetActive(false);
            centerSpawns.SetActive(false);
            baseSpawns.SetActive(false);

            // 3. Configure the environment based on the new 6 levels
            switch (level)
            {
                case 0: // Level 0: 1v1, Tiny Map (Level 0 Walls), Spawns 3m from Flag, Unlimited Ammo
                    size = 1;
                    ammo = 9999; 
                    if (level0Walls != null) level0Walls.SetActive(true);
                    level0Spawns.SetActive(true);
                    teamOneFlag.position = teamOneCenterPos.position;
                    teamTwoFlag.position = teamTwoCenterPos.position;
                    break;

                case 1: // Level 1: 1v1, Small Map (Training Walls), Center Spawns, Unlimited Ammo
                    size = 1;
                    ammo = 9999;
                    if (trainingWalls != null) trainingWalls.SetActive(true);
                    centerSpawns.SetActive(true);
                    teamOneFlag.position = teamOneCenterPos.position;
                    teamTwoFlag.position = teamTwoCenterPos.position;
                    break;

                case 2: // Level 2: 1v1, Full Map (No Walls), Base Spawns, Unlimited Ammo
                    size = 1;
                    ammo = 9999;
                    baseSpawns.SetActive(true);
                    teamOneFlag.position = teamOneBasePos.position;
                    teamTwoFlag.position = teamTwoBasePos.position;
                    break;

                case 3: // Level 3: 5v5, Full Map (No Walls), Base Spawns
                    size = 5;
                    baseSpawns.SetActive(true);
                    teamOneFlag.position = teamOneBasePos.position;
                    teamTwoFlag.position = teamTwoBasePos.position;
                    break;

                case 4: // Level 4: 5v5, Inner Walls Active, Base Spawns/Flags
                    size = 5;
                    innerWalls.SetActive(true);
                    baseSpawns.SetActive(true);
                    teamOneFlag.position = teamOneBasePos.position;
                    teamTwoFlag.position = teamTwoBasePos.position;
                    break;

                case 5: // Level 5 (Final): 11v11, Inner Walls Active, Base Spawns, Pickups Active
                default:
                    size = 11;
                    innerWalls.SetActive(true);
                    allPickups.SetActive(true);
                    baseSpawns.SetActive(true);
                    teamOneFlag.position = teamOneBasePos.position;
                    teamTwoFlag.position = teamTwoBasePos.position;
                    break;
            }

            // 4. Rebuild the NavMesh at runtime
            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
            }

            // 5. Respawn the new teams
            _respawnsOne.Clear();
            _respawnsTwo.Clear();
            for (int i = 0; i < size; i++)
            {
                Spawn(true);
                Spawn(false);
            }
        }
        
        /// <summary>
        /// Spawn a <see cref="Trooper"/>.
        /// </summary>
        /// <param name="teamOne">The team to spawn the <see cref="Trooper"/> for.</param>
        private bool Spawn(bool teamOne)
        {
            // Don't spawn if the teams are full.
            if ((teamOne ? Trooper.AllOne.Count : Trooper.AllTwo.Count) >= size)
            {
                return false;
            }
            
            // There needs to be a point to spawn them.
            SpawnPoint point = SpawnPoint.NextSpawnPoint(teamOne);
            if (point == null)
            {
                Debug.LogError($"Capture the Flag Manager - No spawn points for team {(teamOne ? "one" : "two")}.");
                return false;
            }
            
            point.SpawnOccupy(Trooper.Spawn(prefab, point));
            return true;
        }
        
        /// <summary>
        /// Global callback for when a <see cref="KaijuAgent"/> has finishing becoming disabled.
        /// </summary>
        /// <param name="agent">The <see cref="KaijuAgent"/>.</param>
        protected override void OnAgentDisabled(KaijuAgent agent)
        {
            // Whenever a trooper is eliminated, start a respawn timer.
            if (agent.TryGetComponent(out Trooper trooper))
            {
                (trooper.TeamOne ? _respawnsOne : _respawnsTwo).Add(respawn);
            }
        }
        
        /// <summary>
        /// Frame-rate independent MonoBehaviour.FixedUpdate message for physics calculations.
        /// </summary>
        private void FixedUpdate()
        {
            // Respawn any troopers.
            float delta = Time.deltaTime;
            HandleRespawn(_respawnsOne, true, delta);
            HandleRespawn(_respawnsTwo, false, delta);
        }
        
        /// <summary>
        /// Handle respawns.
        /// </summary>
        /// <param name="respawns">The respawn timers.</param>
        /// <param name="teamOne">Which team this is for.</param>
        /// <param name="delta">The delta time.</param>
        private void HandleRespawn(List<float> respawns, bool teamOne, float delta)
        {
            for (int i = 0; i < respawns.Count; i++)
            {
                respawns[i] -= delta;
                if (respawns[i] > 0)
                {
                    continue;
                }
                
                respawns.RemoveAt(i--);
                Spawn(teamOne);
            }
        }
    }
}