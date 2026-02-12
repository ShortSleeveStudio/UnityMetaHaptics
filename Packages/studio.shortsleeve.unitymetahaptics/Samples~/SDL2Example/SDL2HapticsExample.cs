using UnityEngine;
using UnityEngine.InputSystem;
using Studio.ShortSleeve.UnityMetaHaptics.Common;
using Studio.ShortSleeve.UnityMetaHaptics.SDL2;

namespace Studio.ShortSleeve.UnityMetaHaptics.Samples
{
    /// <summary>
    /// Example demonstrating SDL2 haptics integration with Unity Input System.
    /// Shows three usage patterns: automatic (HapticRequest), convenient (Gamepad), and advanced (SDL2HapticDevice).
    /// </summary>
    public class SDL2HapticsExample : MonoBehaviour
    {
        [Header("Haptic Clips")]
        [SerializeField] HapticClip impactClip;
        [SerializeField] HapticClip loopingClip;
        [SerializeField] HapticClip emphasisClip;

        [Header("Settings")]
        [SerializeField] bool useFirstGamepad = true;
        [SerializeField] float amplitudeScale = 1.0f;

        SDL2HapticsPlayer player;
        Gamepad currentGamepad;
        HapticResponse activeLoop;

        void Start()
        {
            // Create SDL2 haptics system if it doesn't exist
            if (SDL2HapticsSystem.Instance == null)
            {
                SDL2HapticsSystem.CreateInstance();
            }

            // Verify system is initialized
            if (!SDL2HapticsSystem.Instance.IsInitialized)
            {
                Debug.LogError("SDL2HapticsExample: SDL2HapticsSystem failed to initialize");
                enabled = false;
                return;
            }

            // Create player
            player = new SDL2HapticsPlayer();

            // Log system info
            Debug.Log(SDL2HapticsSystem.Instance.GetSystemInfo());

            // Get first gamepad if requested
            if (useFirstGamepad && Gamepad.all.Count > 0)
            {
                currentGamepad = Gamepad.all[0];
                Debug.Log($"Using gamepad: {currentGamepad.name}");
            }
        }

        void Update()
        {
            // Auto-select first gamepad if none selected
            if (currentGamepad == null && Gamepad.all.Count > 0)
            {
                currentGamepad = Gamepad.all[0];
                Debug.Log($"Gamepad connected: {currentGamepad.name}");
            }

            if (currentGamepad == null)
                return;

            // Example 1: Play impact on button press (using HapticRequest)
            if (currentGamepad.aButton.wasPressedThisFrame && impactClip != null)
            {
                PlayImpactWithRequest();
            }

            // Example 2: Start/stop looping (using Gamepad convenience method)
            if (currentGamepad.bButton.wasPressedThisFrame && loopingClip != null)
            {
                ToggleLooping();
            }

            // Example 3: Play with emphasis (using direct device access - advanced)
            if (currentGamepad.xButton.wasPressedThisFrame && emphasisClip != null)
            {
                PlayWithAdvancedAccess();
            }

            // Example 4: Stop all haptics
            if (currentGamepad.yButton.wasPressedThisFrame)
            {
                StopAllHaptics();
            }
        }

        /// <summary>
        /// Example 1: Using HapticRequest (standard Unity API pattern)
        /// </summary>
        void PlayImpactWithRequest()
        {
            HapticRequest request = new HapticRequest
            {
                Clip = impactClip,
                GamepadDevice = currentGamepad,
                ShouldLoop = false,
                UseFixedTime = false,
                ApplyTimeScale = true
            };

            HapticResponse response = player.Play(request);

            if (player.IsValid(response))
            {
                Debug.Log("Playing impact haptic via HapticRequest");
            }
        }

        /// <summary>
        /// Example 2: Using Gamepad convenience method (simpler API)
        /// </summary>
        void ToggleLooping()
        {
            // Check if already looping
            if (activeLoop != null && player.IsValid(activeLoop))
            {
                Debug.Log("Stopping looping haptic");
                player.Stop(activeLoop);
                activeLoop = null;
            }
            else
            {
                Debug.Log("Starting looping haptic");
                activeLoop = player.Play(
                    clip: loopingClip,
                    gamepad: currentGamepad,
                    shouldLoop: true,
                    amplitudeScale: amplitudeScale
                );
            }
        }

        /// <summary>
        /// Example 3: Using SDL2HapticDevice directly (advanced - full control)
        /// </summary>
        void PlayWithAdvancedAccess()
        {
            // Get SDL2 device directly
            SDL2HapticDevice device = SDL2HapticsSystem.Instance.GetDeviceForGamepad(currentGamepad);

            if (device == null)
            {
                Debug.LogWarning("Failed to get SDL2 device for gamepad");
                return;
            }

            // Check device capabilities
            Debug.Log($"Device: {device.DeviceName}");
            Debug.Log($"Supports Custom Waveforms: {device.SupportsCustomWaveforms}");
            Debug.Log($"Max Effects: {device.MaxEffects}");

            // Play with direct device access
            HapticResponse response = player.Play(
                clip: emphasisClip,
                device: device,
                shouldLoop: false,
                amplitudeScale: amplitudeScale
            );

            if (player.IsValid(response))
            {
                Debug.Log("Playing emphasis haptic via direct SDL2 device access");
            }
        }

        /// <summary>
        /// Example 4: Stop all haptics
        /// </summary>
        void StopAllHaptics()
        {
            Debug.Log("Stopping all haptics");
            player.StopAll();
            activeLoop = null;
        }

        void OnDestroy()
        {
            // Clean up
            player?.StopAll();
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            GUILayout.Label("SDL2 Haptics Example", GUI.skin.box);
            GUILayout.Space(10);

            if (currentGamepad == null)
            {
                GUILayout.Label("No gamepad connected", GUI.skin.box);
            }
            else
            {
                GUILayout.Label($"Gamepad: {currentGamepad.name}", GUI.skin.box);
                GUILayout.Space(5);

                GUILayout.Label("Controls:");
                GUILayout.Label("  A Button - Play impact haptic (HapticRequest)");
                GUILayout.Label("  B Button - Toggle looping haptic (Gamepad API)");
                GUILayout.Label("  X Button - Play emphasis (Advanced SDL2 API)");
                GUILayout.Label("  Y Button - Stop all haptics");
                GUILayout.Space(5);

                bool isBusy = player?.IsBusy(currentGamepad) ?? false;
                GUILayout.Label($"Status: {(isBusy ? "Playing" : "Idle")}");

                if (activeLoop != null && player.IsValid(activeLoop))
                {
                    GUILayout.Label("Looping: Active");
                }
            }

            GUILayout.EndArea();
        }
    }
}
