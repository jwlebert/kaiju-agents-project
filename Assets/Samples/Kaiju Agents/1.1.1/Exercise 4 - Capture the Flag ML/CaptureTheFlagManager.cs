using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using UnityEngine.AI; 
using Unity.AI.Navigation;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

#endif
namespace KaijuSolutions.Agents.Exercises.CTF
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
        public GameObject allPickups; // Drag the parent "Pickups" object here
        public NavMeshSurface navMeshSurface; // Drag your 'Navigation Mesh' object here

        [Header("Curriculum - Spawns")]
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
                
                CaptureTheFlagManager manager = FindAnyObjectByType<CaptureTheFlagManager>();
                return manager != null ? manager : new GameObject("Capture the Flag Manager") { isStatic = true }.AddComponent<CaptureTheFlagManager>();
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
                return Instance.health;
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
                return Instance.damage;
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
                return Instance.ammo;
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
                return Instance.cooldown;
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
                return Instance.captureDistance;
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
                return Instance.respawn;
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
                return Instance.size;
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
                return Instance.colorOne;
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
                return Instance.colorTwo;
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
            // 1. Get the current lesson level from the YAML (Defaults to 4 for playing in Editor)
            int level = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("map_level", 4f);

            // 2. Destroy any existing troopers to prevent glitches between episodes
            foreach (var trooper in Trooper.AllOne.ToArray()) { DestroyImmediate(trooper.gameObject); }
            foreach (var trooper in Trooper.AllTwo.ToArray()) { DestroyImmediate(trooper.gameObject); }

            // 3. Configure the environment based on the 4 levels
            switch (level)
            {
                case 1: // Level 1: 1v1, No Walls, Center Spawns/Flags, No Pickups
                    size = 1;
                    innerWalls.SetActive(false);
                    allPickups.SetActive(false);
                    centerSpawns.SetActive(true);
                    baseSpawns.SetActive(false);
                    teamOneFlag.position = teamOneCenterPos.position;
                    teamTwoFlag.position = teamTwoCenterPos.position;
                    break;

                case 2: // Level 2: 5v5, No Walls, Base Spawns/Flags, No Pickups
                    size = 5;
                    innerWalls.SetActive(false);
                    allPickups.SetActive(false);
                    centerSpawns.SetActive(false);
                    baseSpawns.SetActive(true);
                    teamOneFlag.position = teamOneBasePos.position;
                    teamTwoFlag.position = teamTwoBasePos.position;
                    break;

                case 3: // Level 3: 5v5, Inner Walls Active, Base Spawns/Flags, No Pickups
                    size = 5;
                    innerWalls.SetActive(true);
                    allPickups.SetActive(false);
                    centerSpawns.SetActive(false);
                    baseSpawns.SetActive(true);
                    teamOneFlag.position = teamOneBasePos.position;
                    teamTwoFlag.position = teamTwoBasePos.position;
                    break;

                case 4: // Level 4: 11v11, Inner Walls Active, Base Spawns/Flags, Pickups Active
                default:
                    size = 11;
                    innerWalls.SetActive(true);
                    allPickups.SetActive(true);
                    centerSpawns.SetActive(false);
                    baseSpawns.SetActive(true);
                    teamOneFlag.position = teamOneBasePos.position;
                    teamTwoFlag.position = teamTwoBasePos.position;
                    break;
            }

            // 4. Rebuild the NavMesh at runtime so paths match the new wall layout!
            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
            }

            // 5. Respawn the new teams at the newly active spawn points
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