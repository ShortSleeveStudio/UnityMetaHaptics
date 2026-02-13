using System.Collections.Generic;
using System.Threading;
using Studio.ShortSleeve.UnityMetaHaptics.Common;
using UnityEngine;
using static Studio.ShortSleeve.UnityMetaHaptics.Common.DataModel;

namespace Studio.ShortSleeve.UnityMetaHaptics.Gamepad
{
    /// <summary>
    /// Plays haptic clips on Unity Input System gamepads by converting amplitude and frequency envelopes
    /// into motor speeds for low and high frequency rumble motors.
    /// </summary>
    public class GamepadHapticsPlayer : MonoBehaviour, IHapticsPlayer
    {
        #region Constants
        const long InvalidID = -1;
        #endregion

        #region Static Fields
        static long IDCounter = 0;
        static readonly HapticResponse EmptyResponse = new(InvalidID, null, null, null);
        #endregion

        #region Inspector
        /// <summary>
        /// Animation curve used to crossfade between low and high frequency motors based on the frequency value.
        /// X-axis: frequency blend (0 = low frequency, 1 = high frequency)
        /// Y-axis: motor speed multiplier
        /// </summary>
        [SerializeField]
        AnimationCurve motorCrossfadeCurve;
        #endregion

        #region State
        Dictionary<UnityEngine.InputSystem.Gamepad, HapticResponse> _activeVibrations;
        List<UnityEngine.InputSystem.Gamepad> _cachedDeviceList;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            _activeVibrations = new();
            _cachedDeviceList = new();
        }

        void OnDestroy()
        {
            StopAll();
        }
        #endregion

        #region Public API
        /// <summary>
        /// Plays a haptic clip on the specified gamepad device.
        /// If the gamepad is already playing a haptic, it will be stopped first.
        /// </summary>
        /// <param name="request">The haptic playback request containing the clip, device, and playback options.</param>
        /// <param name="token">Optional cancellation token to stop playback externally.</param>
        /// <returns>A HapticResponse that can be used to track, await, or stop the playback.</returns>
        public HapticResponse Play(HapticRequest request, CancellationToken token = default)
        {
            // Validate input
            if (request.GamepadDevice == null)
            {
                Debug.LogError("GamepadHapticsPlayer: Cannot play haptic with null gamepad device");
                return EmptyResponse;
            }
            if (request.Clip == null)
            {
                Debug.LogError("GamepadHapticsPlayer: Cannot play haptic with null clip");
                return EmptyResponse;
            }
            if (
                request.Clip.DataModel == null
                || request.Clip.DataModel.signals?.continuous?.envelopes?.amplitude == null
                || request.Clip.DataModel.signals?.continuous?.envelopes?.frequency == null
                || request.Clip.DataModel.signals.continuous.envelopes.amplitude.Length == 0
                || request.Clip.DataModel.signals.continuous.envelopes.frequency.Length == 0
            )
            {
                Debug.LogError("GamepadHapticsPlayer: Clip data model is invalid or empty");
                return EmptyResponse;
            }
            if (motorCrossfadeCurve == null)
            {
                Debug.LogError(
                    "GamepadHapticsPlayer: motorCrossfadeCurve is not assigned in inspector"
                );
                return EmptyResponse;
            }

            if (IsBusy(request.GamepadDevice))
                Stop(request.GamepadDevice);

            long ID = Interlocked.Increment(ref IDCounter);
            HapticResponse response = new(
                id: ID,
                gamepad: request.GamepadDevice,
                awaitable: PlayInternalV2(ID, request, token),
                parent: this
            );
            StoreHapticEvent(request.GamepadDevice, response);
            return response;
        }

        /// <summary>
        /// Checks if the specified gamepad is currently playing a haptic effect.
        /// </summary>
        /// <param name="gamepad">The gamepad device to check.</param>
        /// <returns>True if the gamepad is actively playing a haptic, false otherwise or if gamepad is null.</returns>
        public bool IsBusy(UnityEngine.InputSystem.Gamepad gamepad)
        {
            if (gamepad == null)
                return false;

            return _activeVibrations.TryGetValue(gamepad, out HapticResponse val)
                && val.ID != InvalidID;
        }

        /// <summary>
        /// Validates that a haptic response is still actively playing and matches the currently tracked response.
        /// </summary>
        /// <param name="response">The haptic response to validate.</param>
        /// <returns>True if the response is valid and still playing, false otherwise.</returns>
        public bool IsValid(HapticResponse response)
        {
            if (response.GamepadDevice == null)
                return false;

            if (!_activeVibrations.TryGetValue(response.GamepadDevice, out HapticResponse val))
                return false;

            return val.ID != InvalidID && val.ID == response.ID;
        }

        /// <summary>
        /// Stops the haptic playback associated with the specified response.
        /// Does nothing if the response is no longer valid.
        /// </summary>
        /// <param name="response">The haptic response to stop.</param>
        public void Stop(HapticResponse response)
        {
            if (!IsValid(response))
                return;
            Stop(response.GamepadDevice);
        }

        /// <summary>
        /// Stops all active haptic playback on all tracked gamepad devices.
        /// </summary>
        public void StopAll()
        {
            // Copy keys to cached list to avoid collection modification during enumeration and allocation
            _cachedDeviceList.Clear();
            _cachedDeviceList.AddRange(_activeVibrations.Keys);

            foreach (UnityEngine.InputSystem.Gamepad device in _cachedDeviceList)
                Stop(device);

            // Clear to release references
            _cachedDeviceList.Clear();
        }
        #endregion

        #region Private API
        void Stop(UnityEngine.InputSystem.Gamepad device)
        {
            if (device == null)
                return;

            device.SetMotorSpeeds(0f, 0f);
            if (!_activeVibrations.TryGetValue(device, out HapticResponse response))
                return;
            if (response.ID == InvalidID)
                return;

            if (!response.InternalAwaitable.IsCompleted)
                response.InternalAwaitable.Cancel();
            StoreHapticEvent(device, EmptyResponse);
        }

        void StoreHapticEvent(UnityEngine.InputSystem.Gamepad gamepad, HapticResponse response)
        {
            if (gamepad == null)
                return;

            _activeVibrations[gamepad] = response;
        }

        async Awaitable PlayInternalV2(
            long responseID,
            HapticRequest request,
            CancellationToken token
        )
        {
            // Cache references to avoid repeated property access
            AmplitudeBreakpoint[] amplitudePoints = request
                .Clip
                .DataModel
                .signals
                .continuous
                .envelopes
                .amplitude;
            FrequencyBreakpoint[] frequencyPoints = request
                .Clip
                .DataModel
                .signals
                .continuous
                .envelopes
                .frequency;
            UnityEngine.InputSystem.Gamepad gamepad = request.GamepadDevice;
            bool shouldLoop = request.ShouldLoop;
            bool useFixedTime = request.UseFixedTime;
            bool applyTimeScale = request.ApplyTimeScale;
            CrossfadeMode fadeMode = request.CrossfadeMode;

            float endTime = (float)amplitudePoints[^1].time;
            float elapsed = 0f;
            int prevAmplitudeIndex = 0;
            int prevFrequencyIndex = 0;

            while (true)
            {
                // Check for cancellation
                token.ThrowIfCancellationRequested();

                // Check if time has elapsed
                bool didLoop = false;
                if (elapsed > endTime)
                {
                    // Loop
                    if (shouldLoop)
                    {
                        elapsed -= endTime;
                        didLoop = true;
                    }
                    // Stop
                    else
                    {
                        Stop(gamepad);
                        return;
                    }
                }

                // Determine next amplitude
                float nextAmplitude = CalculateNextValue(
                    amplitudePoints,
                    didLoop,
                    elapsed,
                    ref prevAmplitudeIndex
                );

                // Determine next frequency
                float nextFrequency = CalculateNextValue(
                    frequencyPoints,
                    didLoop,
                    elapsed,
                    ref prevFrequencyIndex
                );

                // Set Haptics
                float amountHigh = nextFrequency;
                float amountLow = 1f - amountHigh;

                // Apply crossfade mode
                float lowFrequencySpeed;
                float highFrequencySpeed;
                if (fadeMode == CrossfadeMode.EqualPower)
                {
                    // Equal-power crossfade: use sqrt to maintain constant perceived intensity
                    lowFrequencySpeed = nextAmplitude * Mathf.Sqrt(amountLow);
                    highFrequencySpeed = nextAmplitude * Mathf.Sqrt(amountHigh);
                }
                else // CrossfadeMode.Linear
                {
                    // Linear crossfade: use animation curve for custom shaping
                    lowFrequencySpeed = nextAmplitude * motorCrossfadeCurve.Evaluate(amountLow);
                    highFrequencySpeed = nextAmplitude * motorCrossfadeCurve.Evaluate(amountHigh);
                }

                if (applyTimeScale)
                {
                    lowFrequencySpeed *= Time.timeScale;
                    highFrequencySpeed *= Time.timeScale;
                }
                gamepad.SetMotorSpeeds(lowFrequencySpeed, highFrequencySpeed);

                // Wait for next frame
                if (useFixedTime)
                {
                    await Awaitable.FixedUpdateAsync(token);
                    elapsed += applyTimeScale ? Time.fixedDeltaTime : Time.fixedUnscaledDeltaTime;
                }
                else
                {
                    await Awaitable.NextFrameAsync(token);
                    elapsed += applyTimeScale ? Time.deltaTime : Time.unscaledDeltaTime;
                }

                // Make sure we haven't been cancelled or stopped
                if (
                    !_activeVibrations.TryGetValue(gamepad, out HapticResponse response)
                    || response.ID != responseID
                )
                {
                    return;
                }
            }
        }

        float CalculateNextValue(
            Breakpoint[] points,
            bool didLoop,
            float elapsed,
            ref int prevIndex
        )
        {
            // If we just looped, reset to search from the beginning
            if (didLoop)
                prevIndex = 0;

            // Find the segment we're currently in by searching for the next breakpoint after elapsed time
            int nextIndex = prevIndex;

            // Search forward from our cached position to find the next breakpoint
            while (nextIndex < points.Length && (float)points[nextIndex].time <= elapsed)
                nextIndex++;

            // If we've gone past the last breakpoint, wrap to the first one (for looping clips)
            if (nextIndex >= points.Length)
                nextIndex = 0;

            // Determine the previous breakpoint (the one before nextIndex)
            int currentPrevIndex = nextIndex > 0 ? nextIndex - 1 : points.Length - 1;

            // Get the time and value at both breakpoints
            float prevTime = (float)points[currentPrevIndex].time;
            float prevValue = (float)points[currentPrevIndex].Value;
            float nextTime = (float)points[nextIndex].time;
            float nextValue = (float)points[nextIndex].Value;

            // Calculate interpolation factor between the two breakpoints
            float t;

            // Handle wraparound case where we're interpolating from last point to first point
            if (currentPrevIndex > nextIndex)
            {
                // Add the clip duration to nextTime and elapsed to maintain monotonic time progression
                float clipDuration = (float)points[^1].time;
                t = Mathf.InverseLerp(prevTime, nextTime + clipDuration, elapsed + clipDuration);
            }
            else
            {
                // Normal case: simple interpolation
                t = Mathf.InverseLerp(prevTime, nextTime, elapsed);
            }

            // Interpolate to get the final value
            float result = Mathf.Lerp(prevValue, nextValue, t);

            // Cache the next index for the next frame (optimization to avoid searching from 0)
            prevIndex = nextIndex;

            return result;
        }
        #endregion
    }
}
