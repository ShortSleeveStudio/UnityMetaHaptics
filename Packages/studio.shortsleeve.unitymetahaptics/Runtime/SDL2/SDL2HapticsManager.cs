// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;
using SDL2;

namespace Studio.ShortSleeve.UnityMetaHaptics.SDL2
{
    /// <summary>
    /// Singleton manager for SDL2 haptic subsystem.
    /// Handles initialization, device enumeration, and lifecycle management.
    /// </summary>
    public class SDL2HapticsManager
    {
        #region Singleton
        static SDL2HapticsManager _instance;
        static readonly object _lock = new();

        /// <summary>
        /// Gets the singleton instance of the SDL2HapticsManager.
        /// </summary>
        public static SDL2HapticsManager Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new SDL2HapticsManager();
                    }
                    return _instance;
                }
            }
        }

        SDL2HapticsManager()
        {
            // Private constructor for singleton
        }
        #endregion

        #region State
        bool _initialized;
        #endregion

        #region Properties
        /// <summary>
        /// Whether the SDL2 GameController subsystem has been initialized.
        /// </summary>
        public bool IsInitialized => _initialized;
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the SDL2 GameController subsystem.
        /// </summary>
        /// <returns>True if initialization succeeded, false otherwise.</returns>
        public bool Initialize()
        {
            if (_initialized) return true;

            try
            {
                // Set hints for better controller support on macOS
                // Enable HIDAPI backend globally (improves detection of all HID devices)
                SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI, "1");

                // Enable HIDAPI drivers for specific controllers
                // PlayStation 4 DualShock
                SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_PS4, "1");
                SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_PS4_RUMBLE, "1");

                // PlayStation 5 DualSense
                SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_PS5, "1");
                SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_PS5_RUMBLE, "1");

                // Nintendo Switch Pro Controller
                SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_SWITCH, "1");
                SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_SWITCH_HOME_LED, "0");
                // Note: SDL_HINT_JOYSTICK_HIDAPI_SWITCH_PLAYER_LED not available in SDL2 2.0.x
                // Only available in SDL3+

                // Joy-Cons support (if users pair Joy-Cons as a single controller)
                SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_JOY_CONS, "1");

                // Xbox Controllers
                SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_XBOX, "1");

                // General gamepad hints
                SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");
                SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_THREAD, "1"); // Improved hot-plug detection

                // Initialize GameController subsystem (includes Joystick)
                if (SDL.SDL_Init(SDL.SDL_INIT_GAMECONTROLLER) < 0)
                {
                    Debug.LogError($"SDL2 GameController init failed: {SDL.SDL_GetError()}");
                    return false;
                }

                // Add custom controller mappings for better compatibility
                AddCustomMappings();

                _initialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"SDL2HapticsManager: Exception during initialization: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Shuts down the SDL2 GameController subsystem.
        /// </summary>
        public void Shutdown()
        {
            if (!_initialized) return;

            SDL.SDL_QuitSubSystem(SDL.SDL_INIT_GAMECONTROLLER);
            _initialized = false;
        }
        #endregion

        #region Custom Mappings
        /// <summary>
        /// Adds custom controller mappings as fallback for older SDL2 versions.
        ///
        /// NOTE: With HIDAPI hints enabled, SDL2 2.0.14+ should recognize controllers
        /// automatically. These mappings are only needed if:
        /// - The SDL2 version is older than 2.0.14
        /// - The built-in SDL_gamecontrollerdb.h is outdated
        /// - SDL_IsGameController() returns FALSE after hints are set
        ///
        /// GUID format: 030000007e0500000920000000000000
        /// - VID: 0x057e (Nintendo)
        /// - PID: 0x2009 (Switch Pro Controller)
        /// - This is the standard SDL2 HIDAPI format for Switch Pro
        /// - USB and Bluetooth may have different GUIDs, but HIDAPI normalizes them
        /// </summary>
        void AddCustomMappings()
        {
            // Nintendo Switch Pro Controller mappings
            // Source: https://github.com/gabomdq/SDL_GameControllerDB
            // GUID verified by SDL2 expert as correct for Switch Pro via HIDAPI
            string[] switchMappings = new[]
            {
                // macOS - Works for both USB and Bluetooth when HIDAPI is enabled
                "030000007e0500000920000000000000,Nintendo Switch Pro Controller,a:b0,b:b1,back:b8,dpdown:h0.4,dpleft:h0.8,dpright:h0.2,dpup:h0.1,guide:b12,leftshoulder:b4,leftstick:b10,lefttrigger:b6,leftx:a0,lefty:a1,rightshoulder:b5,rightstick:b11,righttrigger:b7,rightx:a2,righty:a3,start:b9,x:b2,y:b3,platform:Mac OS X,",
                // Windows
                "030000007e0500000920000000000000,Nintendo Switch Pro Controller,a:b0,b:b1,back:b8,dpdown:h0.4,dpleft:h0.8,dpright:h0.2,dpup:h0.1,guide:b12,leftshoulder:b4,leftstick:b10,lefttrigger:b6,leftx:a0,lefty:a1,rightshoulder:b5,rightstick:b11,righttrigger:b7,rightx:a2,righty:a3,start:b9,x:b2,y:b3,platform:Windows,",
                // Linux
                "030000007e0500000920000000000000,Nintendo Switch Pro Controller,a:b0,b:b1,back:b8,dpdown:h0.4,dpleft:h0.8,dpright:h0.2,dpup:h0.1,guide:b12,leftshoulder:b4,leftstick:b10,lefttrigger:b6,leftx:a0,lefty:a1,rightshoulder:b5,rightstick:b11,righttrigger:b7,rightx:a2,righty:a3,start:b9,x:b2,y:b3,platform:Linux,"
            };

            foreach (string mapping in switchMappings)
            {
                int result = SDL.SDL_GameControllerAddMapping(mapping);
                if (result < 0)
                {
                    Debug.LogWarning($"SDL2HapticsManager: Failed to add custom mapping: {SDL.SDL_GetError()}");
                }
            }
        }

        /// <summary>
        /// Adds a custom controller mapping string.
        /// Useful for adding support for uncommon or new controllers.
        /// </summary>
        /// <param name="mappingString">SDL2 controller mapping string</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool AddControllerMapping(string mappingString)
        {
            if (!_initialized)
            {
                Debug.LogError("SDL2HapticsManager: Cannot add mapping before initialization");
                return false;
            }

            if (string.IsNullOrEmpty(mappingString))
                return false;

            int result = SDL.SDL_GameControllerAddMapping(mappingString);
            if (result < 0)
            {
                Debug.LogWarning($"SDL2HapticsManager: Failed to add mapping: {SDL.SDL_GetError()}");
                return false;
            }

            return true;
        }
        #endregion

    }
}
