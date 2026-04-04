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
        /// <summary>
        /// Inner walls of the arena.
        /// </summary>
        [Header("Curriculum - Map Toggles")]
        public GameObject innerWalls;
        
        /// <summary>
        /// Small boundaries for training levels.
        /// </summary>
        public GameObject trainingWalls;
        
        /// <summary>
        /// Tiny boundaries for initial training levels.
        /// </summary>
        public GameObject level0Walls;
        
        /// <summary>
        /// Parent object containing all pickups.
        /// </summary>
        public GameObject allPickups;
        
        /// <summary>
        /// Navigation mesh surface for the arena.
        /// </summary>
        public NavMeshSurface navMeshSurface;

        /// <summary>
        /// Spawn points close to the flag.
        /// </summary>
        [Header("Curriculum - Spawns")]
        public GameObject level0Spawns;
        
        /// <summary>
        /// Spawn points located in the center.
        /// </summary>
        public GameObject centerSpawns;
        
        /// <summary>
        /// Spawn points located at the base.
        /// </summary>
        public GameObject baseSpawns;

        /// <summary>
        /// Transform of team one's flag.
        /// </summary>
        [Header("Curriculum - Flags")]
        public Transform teamOneFlag;
        
        /// <summary>
        /// Transform of team two's flag.
        /// </summary>
        public Transform teamTwoFlag;
        
        /// <summary>
        /// Center placement position for team one's flag.
        /// </summary>
        [Header("Curriculum - Flag Placeholders")]
        public Transform teamOneCenterPos;
        
        /// <summary>
        /// Base placement position for team one's flag.
        /// </summary>
        public Transform teamOneBasePos;
        
        /// <summary>
        /// Center placement position for team two's flag.
        /// </summary>
        public Transform teamTwoCenterPos;
        
        /// <summary>
        /// Base placement position for team two's flag.
        /// </summary>
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

        /// <summary>
        /// Handles domain enabling and sets up singleton instance.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
    
            if (_instance == this)
            {
                return;
            }
    
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
    
            _instance = this;
        }
        
        /// <summary>
        /// Initialize the curriculum when the component starts.
        /// </summary>
        private void Start()
        {
            ApplyCurriculum();
        }

        /// <summary>
        /// Handles domain disabling and clears singleton instance.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
    
            _respawnsOne.Clear();
            _respawnsTwo.Clear();
    
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Tracks the current curriculum level.
        /// </summary>
        private int _currentLevel = 0;
        
        /// <summary>
        /// Tracks whether episode setup has completed.
        /// </summary>
        private bool _episodeSetupDone = false;

        /// <summary>
        /// Notifies the manager that an episode has begun to setup curriculum level.
        /// </summary>
        public void NotifyEpisodeBegin()
        {
            if (_episodeSetupDone)
            {
                return;
            }
            
            _episodeSetupDone = true;

            int newLevel = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("map_level", 5f);
            _currentLevel = newLevel;
            ApplyCurriculum();
        }

        /// <summary>
        /// Notifies the manager that an episode has ended.
        /// </summary>
        public void NotifyEpisodeEnd()
        {
            _episodeSetupDone = false;
        }
        
        /// <summary>
        /// The current home position of team one's flag.
        /// </summary>
        private Vector3 _currentFlagOneHome;
        
        /// <summary>
        /// The current home position of team two's flag.
        /// </summary>
        private Vector3 _currentFlagTwoHome;
        
        /// <summary>
        /// Maximum allowed steps based on the current curriculum level.
        /// </summary>
        public int MaxStepsForLevel => 500 + 500 * _currentLevel;
        
        /// <summary>
        /// Rebuilds the scene based on the current curriculum level.
        /// </summary>
        private void ApplyCurriculum()
        {
            int level = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("map_level", 5f);
            _currentLevel = level;

            // Remove existing troopers to establish a clean state for the new episode.
            foreach (var trooper in Trooper.AllOne.ToArray()) 
            { 
                DestroyImmediate(trooper.gameObject); 
            }
            
            foreach (var trooper in Trooper.AllTwo.ToArray()) 
            { 
                DestroyImmediate(trooper.gameObject); 
            }

            // Reset environmental features to default states before applying specific level configurations.
            innerWalls.SetActive(false);
            
            if (trainingWalls != null)
            {
                trainingWalls.SetActive(false);
            }
            
            if (level0Walls != null)
            {
                level0Walls.SetActive(false);
            }
            
            allPickups.SetActive(false);
            level0Spawns.SetActive(false);
            centerSpawns.SetActive(false);
            baseSpawns.SetActive(false);

            // Apply environment bounds, spawn points, and rules for the specified curriculum level.
            switch (level)
            {
                case 0:
                    // 1v1 training layout with highly restricted movement and close proximity.
                    size = 1;
                    ammo = 9999; 
                    if (level0Walls != null)
                    {
                        level0Walls.SetActive(true);
                    }
                    level0Spawns.SetActive(true);
                    teamOneFlag.position = teamOneCenterPos.position;
                    teamTwoFlag.position = teamTwoCenterPos.position;
                    break;

                case 1:
                    // 1v1 setup with slightly expanded boundaries and center map spawning.
                    size = 1;
                    ammo = 9999;
                    if (trainingWalls != null)
                    {
                        trainingWalls.SetActive(true);
                    }
                    centerSpawns.SetActive(true);
                    teamOneFlag.position = teamOneCenterPos.position;
                    teamTwoFlag.position = teamTwoCenterPos.position;
                    break;

                case 2:
                    // 1v1 configuration utilizing the full map size with unlimited resources.
                    size = 1;
                    ammo = 9999;
                    baseSpawns.SetActive(true);
                    teamOneFlag.position = teamOneBasePos.position;
                    teamTwoFlag.position = teamTwoBasePos.position;
                    break;

                case 3:
                    // 5v5 introductory team scenario without internal map obstacles.
                    size = 5;
                    baseSpawns.SetActive(true);
                    teamOneFlag.position = teamOneBasePos.position;
                    teamTwoFlag.position = teamTwoBasePos.position;
                    break;

                case 4:
                    // 5v5 mid-tier scenario introducing navigational challenges via inner walls.
                    size = 5;
                    innerWalls.SetActive(true);
                    baseSpawns.SetActive(true);
                    teamOneFlag.position = teamOneBasePos.position;
                    teamTwoFlag.position = teamTwoBasePos.position;
                    break;

                case 5:
                default:
                    // Complete 11v11 match environment incorporating all mechanics including pickups.
                    size = 11;
                    innerWalls.SetActive(true);
                    allPickups.SetActive(true);
                    baseSpawns.SetActive(true);
                    teamOneFlag.position = teamOneBasePos.position;
                    teamTwoFlag.position = teamTwoBasePos.position;
                    break;
            }
            
            teamOneFlag.GetComponent<Flag>().UpdateHome();
            teamTwoFlag.GetComponent<Flag>().UpdateHome();

            // Rebuild pathfinding data to ensure agents navigate the updated layout correctly.
            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
            }

            _respawnsOne.Clear();
            _respawnsTwo.Clear();
            
            // Populate both teams based on the configured size.
            for (int i = 0; i < size; i++)
            {
                Spawn(true);
                Spawn(false);
            }
            
            _currentFlagOneHome = teamOneFlag.position;
            _currentFlagTwoHome = teamTwoFlag.position;
        }
        
        /// <summary>
        /// Spawn a <see cref="Trooper"/>.
        /// </summary>
        /// <param name="teamOne">The team to spawn the <see cref="Trooper"/> for.</param>
        private bool Spawn(bool teamOne)
        {
            // Abort spawning if the respective team has already reached its maximum capacity.
            if ((teamOne ? Trooper.AllOne.Count : Trooper.AllTwo.Count) >= size)
            {
                return false;
            }
            
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
            // Process independent respawn queues for each team.
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