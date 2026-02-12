using System;
using SDL2;
using UnityEngine;

namespace Studio.ShortSleeve.UnityMetaHaptics.SDL2
{
    /// <summary>
    /// C# wrapper around SDL_GameController pointer for rumble control.
    /// </summary>
    public class SDL2GameController : IDisposable
    {
        public IntPtr Handle { get; private set; }
        public int JoystickIndex { get; }
        public string Name { get; }
        public bool IsValid => Handle != IntPtr.Zero;

        public SDL2GameController(int joystickIndex, string name)
        {
            JoystickIndex = joystickIndex;
            Name = name ?? $"Controller {joystickIndex}";
            Handle = SDL.SDL_GameControllerOpen(joystickIndex);

            if (Handle == IntPtr.Zero)
            {
                Debug.LogError($"SDL2GameController: Failed to open controller {joystickIndex}: {SDL.SDL_GetError()}");
            }
        }

        /// <summary>
        /// Send rumble command to controller.
        /// </summary>
        /// <param name="lowFreq">Low frequency motor intensity (0-65535)</param>
        /// <param name="highFreq">High frequency motor intensity (0-65535)</param>
        /// <param name="durationMs">Duration in milliseconds</param>
        /// <returns>True if rumble command succeeded</returns>
        public bool Rumble(ushort lowFreq, ushort highFreq, uint durationMs)
        {
            if (!IsValid) return false;

            int result = SDL.SDL_GameControllerRumble(Handle, lowFreq, highFreq, durationMs);
            return result >= 0;
        }

        /// <summary>
        /// Stop all rumble on this controller immediately.
        /// </summary>
        public void StopRumble()
        {
            // CRITICAL: For Switch Pro Controller, duration=0 forces immediate "All Off" HID report
            // Non-zero duration schedules a future stop event, which conflicts with queued packets
            // This prevents the 1-2 second "tail-off" caused by Bluetooth buffer saturation
            Rumble(0, 0, 0);
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                StopRumble();
                SDL.SDL_GameControllerClose(Handle);
                Handle = IntPtr.Zero;
            }
        }
    }
}
