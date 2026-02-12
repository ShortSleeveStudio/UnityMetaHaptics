using System;
using SDL2;
using Studio.ShortSleeve.UnityMetaHaptics.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Studio.ShortSleeve.UnityMetaHaptics.SDL2
{
    /// <summary>
    /// MonoBehaviour that manages the SDL2 haptics system lifecycle.
    /// Handles initialization, device mapping, and hot-plug detection via Unity Input System events.
    /// Use this as the main entry point for SDL2 haptics in your Unity project.
    /// </summary>
    public class SDL2HapticsSystem : MonoBehaviour
    {
        #region Singleton
        static SDL2HapticsSystem _instance;

        /// <summary>
        /// Gets the singleton instance.
        /// Returns null if no instance exists - use CreateInstance() or manually add component to scene.
        /// </summary>
        public static SDL2HapticsSystem Instance => _instance;

        /// <summary>
        /// Creates a new SDL2HapticsSystem instance on a new GameObject.
        /// Returns existing instance if one already exists.
        /// </summary>
        public static SDL2HapticsSystem CreateInstance()
        {
            if (_instance != null)
                return _instance;

            GameObject go = new GameObject("[SDL2HapticsSystem]");
            return go.AddComponent<SDL2HapticsSystem>();
        }
        #endregion

        #region Inspector
        [Header("Lifecycle Settings")]
        [Tooltip("Automatically initialize SDL2 on Awake")]
        [SerializeField]
        bool autoInitialize = true;

        [Tooltip("Persist this GameObject across scene loads")]
        [SerializeField]
        bool dontDestroyOnLoad = true;

        [Header("Debug")]
        [Tooltip("Log initialization and device mapping info")]
        [SerializeField]
        bool verboseLogging = false;
        #endregion

        #region State
        bool _initialized;
        SDL2DeviceMapper _mapper;
        SDL2HapticsManager _manager;
        SDL2RumblePlayer _player;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the SDL2HapticsManager instance.
        /// </summary>
        public SDL2HapticsManager Manager => _manager;

        /// <summary>
        /// Gets the SDL2DeviceMapper instance.
        /// </summary>
        public SDL2DeviceMapper Mapper => _mapper;

        /// <summary>
        /// Whether the system has been initialized.
        /// </summary>
        public bool IsInitialized => _initialized;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            // Handle singleton enforcement
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning(
                    "SDL2HapticsSystem: Multiple instances detected. Destroying duplicate."
                );
                Destroy(gameObject);
                return;
            }

            _instance = this;

            // Optional persistence
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            // Initialize manager, mapper, and player
            _manager = SDL2HapticsManager.Instance;
            _mapper = new SDL2DeviceMapper(_manager);
            _player = new SDL2RumblePlayer();

            // Optional auto-initialization
            if (autoInitialize)
            {
                Initialize();
            }
        }

        void OnEnable()
        {
            // Subscribe to Unity Input System device change events
            InputSystem.onDeviceChange += OnInputDeviceChange;
        }

        void OnDisable()
        {
            // Unsubscribe from events
            InputSystem.onDeviceChange -= OnInputDeviceChange;
        }

        void OnDestroy()
        {
            Shutdown();

            if (_instance == this)
            {
                _instance = null;
            }
        }

        void OnApplicationQuit()
        {
            Shutdown();
        }

        void Update()
        {
            if (_initialized && _player != null)
            {
                _player.Update();
            }
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the SDL2 haptics system.
        /// Safe to call multiple times - will only initialize once.
        /// </summary>
        /// <returns>True if successful, false otherwise.</returns>
        public bool Initialize()
        {
            if (_initialized)
            {
                if (verboseLogging)
                    Debug.Log("SDL2HapticsSystem: Already initialized");
                return true;
            }

            try
            {
                // Initialize SDL2 GameController subsystem
                if (!_manager.Initialize())
                {
                    Debug.LogError("SDL2HapticsSystem: Failed to initialize SDL2HapticsManager");
                    return false;
                }

                // Build device mapping table
                if (!_mapper.BuildMappingTable())
                {
                    Debug.LogError("SDL2HapticsSystem: Failed to build device mapping table");
                    return false;
                }

                _initialized = true;

                if (verboseLogging)
                {
                    Debug.Log("SDL2HapticsSystem: Initialized successfully");
                    Debug.Log(_mapper.GetMappingInfo());
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"SDL2HapticsSystem: Exception during initialization: {ex.Message}\n{ex.StackTrace}"
                );
                return false;
            }
        }

        /// <summary>
        /// Shuts down the SDL2 haptics system and releases all resources.
        /// Safe to call multiple times - will only shutdown once.
        /// </summary>
        public void Shutdown()
        {
            if (!_initialized)
                return;

            try
            {
                // Stop all active playback
                _player?.StopAll();

                // Shutdown mapper (closes all controllers)
                _mapper?.Shutdown();

                // Shutdown manager
                _manager?.Shutdown();

                _initialized = false;

                if (verboseLogging)
                    Debug.Log("SDL2HapticsSystem: Shutdown complete");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SDL2HapticsSystem: Exception during shutdown: {ex.Message}");
            }
        }
        #endregion

        #region Device Mapping & Playback
        /// <summary>
        /// Gets an SDL2 game controller for the given Unity gamepad.
        /// </summary>
        public SDL2GameController GetControllerForGamepad(Gamepad gamepad)
        {
            if (!_initialized)
            {
                Debug.LogError("SDL2HapticsSystem: Not initialized");
                return null;
            }

            return _mapper.GetControllerForGamepad(gamepad);
        }

        /// <summary>
        /// Plays a haptic clip on the specified gamepad.
        /// </summary>
        public HapticResponse Play(HapticRequest request)
        {
            if (!_initialized)
            {
                Debug.LogError("SDL2HapticsSystem: Not initialized");
                return new HapticResponse(-1, request.GamepadDevice, null, _player);
            }

            SDL2GameController controller = GetControllerForGamepad(request.GamepadDevice);
            if (controller == null)
                return new HapticResponse(-1, request.GamepadDevice, null, _player);

            return _player.Play(request, controller);
        }

        /// <summary>
        /// Stops all haptic playback.
        /// </summary>
        public void StopAll()
        {
            _player?.StopAll();
        }

        /// <summary>
        /// Refreshes the device mapping table.
        /// </summary>
        public void RefreshDevices()
        {
            if (!_initialized)
                return;

            _mapper.RefreshMappingTable();

            if (verboseLogging)
            {
                Debug.Log("SDL2HapticsSystem: Device mapping refreshed");
                Debug.Log(_mapper.GetMappingInfo());
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles Unity Input System device change events.
        /// Implements event-driven hot-plug detection.
        /// </summary>
        void OnInputDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (!_initialized)
                return;

            // Only handle gamepad devices
            if (!(device is Gamepad gamepad))
                return;

            switch (change)
            {
                case InputDeviceChange.Added:
                    if (verboseLogging)
                        Debug.Log($"SDL2HapticsSystem: Gamepad added - {gamepad.name}");

                    _mapper.OnGamepadAdded(gamepad);

                    if (verboseLogging)
                        Debug.Log(_mapper.GetMappingInfo());
                    break;

                case InputDeviceChange.Removed:
                    if (verboseLogging)
                        Debug.Log($"SDL2HapticsSystem: Gamepad removed - {gamepad.name}");

                    _mapper.OnGamepadRemoved(gamepad);
                    break;

                case InputDeviceChange.Reconnected:
                    if (verboseLogging)
                        Debug.Log($"SDL2HapticsSystem: Gamepad reconnected - {gamepad.name}");

                    // Treat reconnection as a new device
                    _mapper.OnGamepadAdded(gamepad);
                    break;

                case InputDeviceChange.Disconnected:
                    if (verboseLogging)
                        Debug.Log($"SDL2HapticsSystem: Gamepad disconnected - {gamepad.name}");

                    // Remove from mapping
                    _mapper.OnGamepadRemoved(gamepad);
                    break;
            }
        }
        #endregion

        #region Debug/Info
        /// <summary>
        /// Gets debug information about the current system state.
        /// </summary>
        /// <returns>A string with system information.</returns>
        public string GetSystemInfo()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== SDL2HapticsSystem Info ===");
            sb.AppendLine($"Initialized: {_initialized}");
            sb.AppendLine($"Manager: {(_manager?.IsInitialized ?? false)}");
            sb.AppendLine($"Auto Initialize: {autoInitialize}");
            sb.AppendLine($"Don't Destroy On Load: {dontDestroyOnLoad}");
            sb.AppendLine();

            if (_mapper != null)
            {
                sb.Append(_mapper.GetMappingInfo());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Logs the current system information to the console.
        /// </summary>
        [ContextMenu("Log System Info")]
        public void LogSystemInfo()
        {
            Debug.Log(GetSystemInfo());
        }
        #endregion
    }
}
