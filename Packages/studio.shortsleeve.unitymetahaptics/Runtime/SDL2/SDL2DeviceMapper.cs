using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using SDL2;

namespace Studio.ShortSleeve.UnityMetaHaptics.SDL2
{
    /// <summary>
    /// Maps Unity Input System Gamepad devices to SDL2 GameControllers.
    /// Uses VID/PID (Vendor ID/Product ID) matching for reliable device pairing.
    /// </summary>
    public class SDL2DeviceMapper
    {
        #region Structures
        /// <summary>
        /// Represents an SDL joystick device with its hardware identifiers.
        /// </summary>
        struct JoystickInfo
        {
            public int Index;
            public ushort VendorId;
            public ushort ProductId;
            public string Name;
        }

        /// <summary>
        /// Helper class for deserializing HID capabilities JSON.
        /// </summary>
        [Serializable]
        class HIDCapabilities
        {
            public int vendorId;
            public int productId;
        }
        #endregion

        #region Fields
        readonly Dictionary<string, JoystickInfo> _vidPidToJoystick = new();
        readonly Dictionary<Gamepad, SDL2GameController> _gamepadToController = new();
        readonly SDL2HapticsManager _manager;
        bool _initialized;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new device mapper.
        /// </summary>
        /// <param name="manager">The SDL2HapticsManager instance to use.</param>
        public SDL2DeviceMapper(SDL2HapticsManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }
        #endregion

        #region Mapping Table Building
        /// <summary>
        /// Builds the mapping table by enumerating SDL2 joysticks.
        /// Must be called after SDL2 initialization.
        /// </summary>
        /// <returns>True if successful, false otherwise.</returns>
        public bool BuildMappingTable()
        {
            if (!_manager.IsInitialized)
            {
                Debug.LogError("SDL2DeviceMapper: SDL2 not initialized");
                return false;
            }

            _vidPidToJoystick.Clear();

            try
            {
                int numJoysticks = SDL.SDL_NumJoysticks();

                for (int i = 0; i < numJoysticks; i++)
                {
                    // Only process game controllers
                    if (SDL.SDL_IsGameController(i) == SDL.SDL_bool.SDL_FALSE)
                        continue;

                    ushort vendorId = SDL.SDL_JoystickGetDeviceVendor(i);
                    ushort productId = SDL.SDL_JoystickGetDeviceProduct(i);
                    string name = SDL.SDL_GameControllerNameForIndex(i);

                    string key = MakeVidPidKey(vendorId, productId);

                    JoystickInfo info = new()
                    {
                        Index = i,
                        VendorId = vendorId,
                        ProductId = productId,
                        Name = name ?? $"Controller {i}"
                    };

                    _vidPidToJoystick[key] = info;
                }

                _initialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"SDL2DeviceMapper: Exception building mapping table: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Refreshes the mapping table. Called when devices are hot-plugged.
        /// </summary>
        public void RefreshMappingTable()
        {
            if (!_manager.IsInitialized) return;

            BuildMappingTable();

            // Validate existing gamepad mappings
            List<Gamepad> invalidGamepads = new();
            foreach (KeyValuePair<Gamepad, SDL2GameController> kvp in _gamepadToController)
            {
                if (kvp.Value == null || !kvp.Value.IsValid)
                {
                    invalidGamepads.Add(kvp.Key);
                }
            }

            // Remove invalid mappings
            foreach (Gamepad gamepad in invalidGamepads)
            {
                RemoveGamepad(gamepad);
            }
        }
        #endregion

        #region Device Mapping
        /// <summary>
        /// Gets an SDL2 game controller for the given Unity gamepad.
        /// Implements lazy validation: checks cache first, validates device, remaps if needed.
        /// </summary>
        /// <param name="gamepad">The Unity gamepad to map.</param>
        /// <returns>An SDL2GameController, or null if mapping failed.</returns>
        public SDL2GameController GetControllerForGamepad(Gamepad gamepad)
        {
            if (gamepad == null || !_initialized) return null;

            // Check cache first
            if (_gamepadToController.TryGetValue(gamepad, out SDL2GameController cached))
            {
                if (cached != null && cached.IsValid)
                    return cached;

                // Cached controller is invalid - remove and remap
                _gamepadToController.Remove(gamepad);
                cached?.Dispose();
            }

            // Extract VID/PID from Unity gamepad
            if (!TryExtractVidPid(gamepad, out ushort vendorId, out ushort productId))
                return null;

            // Find matching SDL joystick
            string key = MakeVidPidKey(vendorId, productId);
            if (!_vidPidToJoystick.TryGetValue(key, out JoystickInfo joystickInfo))
                return null;

            // Open game controller
            SDL2GameController controller = new(joystickInfo.Index, joystickInfo.Name);
            if (!controller.IsValid)
            {
                Debug.LogError($"SDL2DeviceMapper: Failed to open controller {joystickInfo.Name}");
                return null;
            }

            // Cache the mapping
            _gamepadToController[gamepad] = controller;
            return controller;
        }

        /// <summary>
        /// Handles a gamepad being added to the system.
        /// </summary>
        public void OnGamepadAdded(Gamepad gamepad)
        {
            if (gamepad == null) return;
            RefreshMappingTable();
        }

        /// <summary>
        /// Handles a gamepad being removed from the system.
        /// </summary>
        public void OnGamepadRemoved(Gamepad gamepad)
        {
            if (gamepad == null) return;
            RemoveGamepad(gamepad);
        }

        /// <summary>
        /// Removes a gamepad from the mapping cache and disposes its controller.
        /// </summary>
        void RemoveGamepad(Gamepad gamepad)
        {
            if (gamepad == null) return;

            if (_gamepadToController.TryGetValue(gamepad, out SDL2GameController controller))
            {
                controller?.Dispose();
                _gamepadToController.Remove(gamepad);
            }
        }
        #endregion

        #region Cleanup
        /// <summary>
        /// Closes all open controllers.
        /// </summary>
        public void Shutdown()
        {
            foreach (SDL2GameController controller in _gamepadToController.Values)
            {
                controller?.Dispose();
            }
            _gamepadToController.Clear();

            _vidPidToJoystick.Clear();
            _initialized = false;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Creates a normalized VID/PID key for dictionary lookup.
        /// </summary>
        static string MakeVidPidKey(ushort vendorId, ushort productId)
        {
            return $"vid_{vendorId:x4}_pid_{productId:x4}";
        }

        /// <summary>
        /// Extracts VID/PID from a Unity gamepad.
        /// </summary>
        bool TryExtractVidPid(Gamepad gamepad, out ushort vendorId, out ushort productId)
        {
            vendorId = 0;
            productId = 0;

            try
            {
                InputDeviceDescription description = gamepad.description;

                // Try JSON capabilities first
                if (!string.IsNullOrEmpty(description.capabilities))
                {
                    try
                    {
                        HIDCapabilities caps = JsonUtility.FromJson<HIDCapabilities>(description.capabilities);
                        if (caps != null && caps.vendorId > 0 && caps.productId > 0 &&
                            caps.vendorId <= ushort.MaxValue && caps.productId <= ushort.MaxValue)
                        {
                            vendorId = (ushort)caps.vendorId;
                            productId = (ushort)caps.productId;
                            return true;
                        }
                    }
                    catch
                    {
                        // Fallback to regex
                        Match vidMatch = Regex.Match(description.capabilities,
                            @"""vendorId""\s*:\s*(\d+)", RegexOptions.IgnoreCase);
                        Match pidMatch = Regex.Match(description.capabilities,
                            @"""productId""\s*:\s*(\d+)", RegexOptions.IgnoreCase);

                        if (vidMatch.Success && ushort.TryParse(vidMatch.Groups[1].Value, out vendorId) &&
                            pidMatch.Success && ushort.TryParse(pidMatch.Groups[1].Value, out productId))
                        {
                            return true;
                        }
                    }
                }

                // Fallback: interface name (Windows HID format)
                string interfaceName = description.interfaceName ?? string.Empty;
                Match match = Regex.Match(interfaceName,
                    @"vid[_&]([0-9a-f]{4})[_&]pid[_&]([0-9a-f]{4})",
                    RegexOptions.IgnoreCase);

                if (match.Success &&
                    ushort.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber, null, out vendorId) &&
                    ushort.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.HexNumber, null, out productId))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"SDL2DeviceMapper: Exception extracting VID/PID: {ex.Message}");
            }

            return false;
        }
        #endregion

        #region Debug/Info
        /// <summary>
        /// Gets debug information about the current mappings.
        /// </summary>
        public string GetMappingInfo()
        {
            System.Text.StringBuilder sb = new();
            sb.AppendLine($"SDL2DeviceMapper - Initialized: {_initialized}");
            sb.AppendLine($"Controllers found: {_vidPidToJoystick.Count}");

            foreach (JoystickInfo info in _vidPidToJoystick.Values)
            {
                sb.AppendLine($"  [{info.Index}] {info.Name} (VID: 0x{info.VendorId:X4}, PID: 0x{info.ProductId:X4})");
            }

            sb.AppendLine($"Mapped gamepads: {_gamepadToController.Count}");
            foreach (KeyValuePair<Gamepad, SDL2GameController> kvp in _gamepadToController)
            {
                sb.AppendLine($"  {kvp.Key.name} -> {kvp.Value?.Name}");
            }

            return sb.ToString();
        }
        #endregion
    }
}
