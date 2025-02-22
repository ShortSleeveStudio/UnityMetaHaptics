using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Studio.ShortSleeve.UnityMetaHaptics
{
    public class GamepadHaptics : MonoBehaviour
    {
        #region Constants
        const long InvalidID = -1;
        #endregion

        #region Static Fields
        static long IDCounter = 0;
        static readonly GamepadHapticResponse EmptyResponse = new(InvalidID, null, null, null);
        #endregion

        #region State
        HashSet<Gamepad> _gamepadSet;
        Dictionary<Gamepad, GamepadHapticResponse> _activeVibrations;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            // Populate device map
            _gamepadSet = new();
            _activeVibrations = new();
        }
        #endregion

        #region Public API
        public GamepadHapticResponse Play(
            GamepadHapticRequest request,
            CancellationToken token = default
        )
        {
            Awaitable awaitable = PlayInternal(request, token);
            GamepadHapticResponse response =
                new(id: IDCounter++, gamepad: request.Gamepad, awaitable: awaitable, parent: this);
            StoreHapticEvent(request.Gamepad, response);
            return response;
        }

        public bool IsValid(GamepadHapticResponse response)
        {
            if (
                !_activeVibrations.TryGetValue(response.Device, out GamepadHapticResponse val)
                || val.ID == InvalidID
                || val.ID != response.ID
            )
                return false;
            return true;
        }

        public void Stop(GamepadHapticResponse response)
        {
            if (!IsValid(response))
                return;
            Stop(response.Device);
        }

        public void StopAll()
        {
            foreach (Gamepad device in _gamepadSet)
                Stop(device);
        }
        #endregion

        #region Private API
        void Stop(Gamepad device)
        {
            device.SetMotorSpeeds(0f, 0f);
            if (
                !_activeVibrations.TryGetValue(device, out GamepadHapticResponse response)
                || response.ID == InvalidID
            )
                return;
            if (!response.InternalAwaitable.IsCompleted)
                response.InternalAwaitable.Cancel();
            StoreHapticEvent(device, EmptyResponse);
        }

        async Awaitable PlayInternal(GamepadHapticRequest request, CancellationToken token)
        {
            // Initialize
            long elapsedMs = 0;
            int rumbleIndex = 0;
            long rumbleOffsetMs = 0;
            bool setHapticsForThisIndex = false;
            float previousTimeScale = Time.timeScale;

            while (true)
            {
                long timeThisIndex = request.Clip.durationsMs[rumbleIndex];
                long timeWaitedThisIndex = elapsedMs - rumbleOffsetMs;
                long durationToWaitMs = timeThisIndex - timeWaitedThisIndex;

                // Check if we need to increment the rumble index
                if (durationToWaitMs < 0)
                {
                    // Reset set haptics flag
                    setHapticsForThisIndex = false;

                    // Continue through the timeseries data
                    rumbleOffsetMs += request.Clip.durationsMs[rumbleIndex++];

                    // Check if we need to loop, or exit
                    if (rumbleIndex == request.Clip.durationsMs.Length)
                    {
                        // We've finished
                        if (!request.ShouldLoop)
                        {
                            Stop(request.Gamepad);
                            return;
                        }

                        // Loop
                        rumbleIndex = 0;
                        rumbleOffsetMs = 0;
                        elapsedMs = -durationToWaitMs; // add the error to elapsed (which would otherwise be zero since we're restarting)
                    }
                    continue;
                }

                // Check if we care about timescale
                if (!Mathf.Approximately(previousTimeScale, Time.timeScale))
                {
                    previousTimeScale = Time.timeScale;
                    if (request.ApplyTimeScale)
                        setHapticsForThisIndex = false;
                }

                // Set haptics for this index if needed
                if (!setHapticsForThisIndex)
                {
                    setHapticsForThisIndex = true;

                    // Lookup haptics frequencies
                    float lowFrequencySpeed = request.Clip.lowFrequencyMotorSpeeds[rumbleIndex];
                    float highFrequencySpeed = request.Clip.highFrequencyMotorSpeeds[rumbleIndex];
                    if (request.ApplyTimeScale)
                    {
                        lowFrequencySpeed *= previousTimeScale;
                        highFrequencySpeed *= previousTimeScale;
                    }

                    // Play haptics
                    request.Gamepad.SetMotorSpeeds(lowFrequencySpeed, highFrequencySpeed);
                }

                // We must continue waiting
                if (request.UseFixedTime)
                {
                    await Awaitable.FixedUpdateAsync(token);
                    if (request.ApplyTimeScale)
                        elapsedMs += (long)(Time.fixedDeltaTime * 1000);
                    else
                        elapsedMs += (long)(Time.fixedUnscaledDeltaTime * 1000);
                }
                else
                {
                    await Awaitable.NextFrameAsync(token);
                    if (request.ApplyTimeScale)
                        elapsedMs += (long)(Time.deltaTime * 1000);
                    else
                        elapsedMs += (long)(Time.unscaledDeltaTime * 1000);
                }

                // Make sure we haven't been cancelled
                if (
                    !_activeVibrations.TryGetValue(
                        request.Gamepad,
                        out GamepadHapticResponse response
                    )
                    || response.ID == InvalidID
                )
                {
                    return;
                }
            }
        }

        void StoreHapticEvent(Gamepad gamepad, GamepadHapticResponse response)
        {
            _gamepadSet.Add(gamepad);
            _activeVibrations[gamepad] = response;
        }
        #endregion
    }
}
