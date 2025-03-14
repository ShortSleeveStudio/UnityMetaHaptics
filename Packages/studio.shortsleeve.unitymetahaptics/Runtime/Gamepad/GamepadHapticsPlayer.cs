using System.Collections.Generic;
using System.Threading;
using Studio.ShortSleeve.UnityMetaHaptics.Common;
using UnityEngine;
using static Studio.ShortSleeve.UnityMetaHaptics.Common.DataModel;

namespace Studio.ShortSleeve.UnityMetaHaptics.Gamepad
{
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
        [SerializeField]
        AnimationCurve motorCrossfadeCurve;
        #endregion

        #region State
        HashSet<UnityEngine.InputSystem.Gamepad> _gamepadSet;
        Dictionary<UnityEngine.InputSystem.Gamepad, HapticResponse> _activeVibrations;
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
        public HapticResponse Play(HapticRequest request, CancellationToken token = default)
        {
            if (IsBusy(request.GamepadDevice))
                Stop(request.GamepadDevice);

            long ID = IDCounter++;
            HapticResponse response =
                new(
                    id: ID,
                    gamepad: request.GamepadDevice,
                    awaitable: PlayInternalV2(ID, request, token),
                    parent: this
                );
            StoreHapticEvent(request.GamepadDevice, response);
            return response;
        }

        public bool IsBusy(UnityEngine.InputSystem.Gamepad gamepad)
        {
            if (
                _activeVibrations.TryGetValue(gamepad, out HapticResponse val)
                || val.ID != InvalidID
            )
                return true;
            return false;
        }

        public bool IsValid(HapticResponse response)
        {
            if (
                !_activeVibrations.TryGetValue(response.GamepadDevice, out HapticResponse val)
                || val.ID == InvalidID
                || val.ID != response.ID
            )
                return false;
            return true;
        }

        public void Stop(HapticResponse response)
        {
            if (!IsValid(response))
                return;
            Stop(response.GamepadDevice);
        }

        public void StopAll()
        {
            foreach (UnityEngine.InputSystem.Gamepad device in _gamepadSet)
                Stop(device);
        }
        #endregion

        #region Private API
        void Stop(UnityEngine.InputSystem.Gamepad device)
        {
            device.SetMotorSpeeds(0f, 0f);
            if (
                !_activeVibrations.TryGetValue(device, out HapticResponse response)
                || response.ID == InvalidID
            )
                return;
            if (!response.InternalAwaitable.IsCompleted)
                response.InternalAwaitable.Cancel();
            StoreHapticEvent(device, EmptyResponse);
            device.SetMotorSpeeds(0f, 0f);
        }

        void StoreHapticEvent(UnityEngine.InputSystem.Gamepad gamepad, HapticResponse response)
        {
            _gamepadSet.Add(gamepad);
            _activeVibrations[gamepad] = response;
        }

        async Awaitable PlayInternalV2(
            long responseID,
            HapticRequest request,
            CancellationToken token
        )
        {
            // Initialize
            AmplitudeBreakpoint[] amplitudePoints = request
                .Clip
                .dataModel
                .signals
                .continuous
                .envelopes
                .amplitude;
            FrequencyBreakpoint[] frequencyPoints = request
                .Clip
                .dataModel
                .signals
                .continuous
                .envelopes
                .frequency;
            float endTime = (float)amplitudePoints[^1].time;
            float elapsed = 0f;
            int prevAmplitudeIndex = 0;
            int prevFrequencyIndex = 0;
            while (true)
            {
                // Check if time has elapsed
                bool didLoop = false;
                if (elapsed > endTime)
                {
                    // Loop
                    if (request.ShouldLoop)
                    {
                        elapsed -= endTime;
                        didLoop = true;
                    }
                    // Stop
                    else
                    {
                        Stop(request.GamepadDevice);
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
                float lowFrequencySpeed = nextAmplitude * motorCrossfadeCurve.Evaluate(amountLow);
                float highFrequencySpeed = nextAmplitude * motorCrossfadeCurve.Evaluate(amountHigh);
                if (request.ApplyTimeScale)
                {
                    lowFrequencySpeed *= Time.timeScale;
                    highFrequencySpeed *= Time.timeScale;
                }
                request.GamepadDevice.SetMotorSpeeds(lowFrequencySpeed, highFrequencySpeed);

                // We must continue waiting
                if (request.UseFixedTime)
                {
                    await Awaitable.FixedUpdateAsync(token);
                    if (request.ApplyTimeScale)
                        elapsed += Time.fixedDeltaTime;
                    else
                        elapsed += Time.fixedUnscaledDeltaTime;
                }
                else
                {
                    await Awaitable.NextFrameAsync(token);
                    if (request.ApplyTimeScale)
                        elapsed += Time.deltaTime;
                    else
                        elapsed += Time.unscaledDeltaTime;
                }

                // Make sure we haven't been cancelled
                if (
                    !_activeVibrations.TryGetValue(
                        request.GamepadDevice,
                        out HapticResponse response
                    )
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
            int nextIndex;
            int i = prevIndex;
            while (true)
            {
                // Iterate until we find the next index
                float currentTime = (float)points[i].time;
                if (currentTime < elapsed)
                {
                    // Continue interating
                    i++;
                    continue;
                }
                else if (currentTime >= elapsed)
                {
                    if (didLoop)
                    {
                        // We loop if needed, and continue adding points
                        if (++i == points.Length)
                        {
                            didLoop = false;
                            i = 0;
                        }
                        continue;
                    }
                    else
                    {
                        // We found the next index
                        nextIndex = i;
                        break;
                    }
                }
            }
            // Set previous index
            if ((nextIndex - 1) < 0)
                prevIndex = points.Length - 1;
            else
                prevIndex = nextIndex - 1;

            // Determine next value
            float nextValue;
            float normalizedDistanceToNextPoint = 0;
            if (prevIndex > nextIndex)
            {
                float nextTime = (float)points[nextIndex].time + (float)points[^1].time;
                float prevTime = (float)points[prevIndex].time;
                normalizedDistanceToNextPoint = Mathf.InverseLerp(
                    prevTime,
                    nextTime,
                    elapsed + (float)points[^1].time
                );
            }
            else if (prevIndex <= nextIndex)
            {
                float nextTime = (float)points[nextIndex].time;
                float prevTime = (float)points[prevIndex].time;
                normalizedDistanceToNextPoint = Mathf.InverseLerp(prevTime, nextTime, elapsed);
            }
            nextValue = Mathf.Lerp(
                (float)points[prevIndex].Value,
                (float)points[nextIndex].Value,
                normalizedDistanceToNextPoint
            );

            // Update previous index
            prevIndex = nextIndex;
            return nextValue;
        }
        #endregion
    }
}
