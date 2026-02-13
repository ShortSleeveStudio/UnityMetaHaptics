using System.Threading;

namespace Studio.ShortSleeve.UnityMetaHaptics.Common
{
    /// <summary>
    /// Interface for haptic playback controllers.
    /// </summary>
    public interface IHapticsPlayer
    {
        /// <summary>
        /// Plays a haptic clip with the specified request parameters.
        /// </summary>
        /// <param name="request">The haptic playback request containing the clip and device information.</param>
        /// <param name="token">Optional cancellation token to stop playback.</param>
        /// <returns>A HapticResponse that can be used to track or stop the playback.</returns>
        public HapticResponse Play(HapticRequest request, CancellationToken token = default);

        /// <summary>
        /// Checks if the specified gamepad is currently playing a haptic effect.
        /// </summary>
        /// <param name="gamepad">The gamepad device to check.</param>
        /// <returns>True if the gamepad is busy playing a haptic, false otherwise.</returns>
        public bool IsBusy(UnityEngine.InputSystem.Gamepad gamepad);

        /// <summary>
        /// Checks if a haptic response is still valid and actively playing.
        /// </summary>
        /// <param name="response">The haptic response to validate.</param>
        /// <returns>True if the response is valid and playing, false otherwise.</returns>
        public bool IsValid(HapticResponse response);

        /// <summary>
        /// Stops the haptic playback associated with the specified response.
        /// </summary>
        /// <param name="response">The haptic response to stop.</param>
        public void Stop(HapticResponse response);

        /// <summary>
        /// Stops all active haptic playback on all devices.
        /// </summary>
        public void StopAll();
    }
}
