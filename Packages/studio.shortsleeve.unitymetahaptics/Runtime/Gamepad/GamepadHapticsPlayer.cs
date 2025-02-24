using System.Collections.Generic;
using System.Threading;
using Studio.ShortSleeve.UnityMetaHaptics.Common;
using UnityEngine;

namespace Studio.ShortSleeve.UnityMetaHaptics.Gamepad
{
    public class GamepadHapticsPlayer
        : MonoBehaviour,
            IHapticsPlayer<UnityEngine.InputSystem.Gamepad>
    {
        #region Constants
        const long InvalidID = -1;
        #endregion

        #region Static Fields
        static long IDCounter = 0;
        static readonly GamepadHaptic<UnityEngine.InputSystem.Gamepad> EmptyResponse =
            new(InvalidID, null, null, null);
        #endregion

        #region Inspector
        [SerializeField]
        AnimationCurve motorCrossfadeCurve;
        #endregion

        #region State
        HashSet<UnityEngine.InputSystem.Gamepad> _gamepadSet;
        Dictionary<
            UnityEngine.InputSystem.Gamepad,
            GamepadHaptic<UnityEngine.InputSystem.Gamepad>
        > _activeVibrations;
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
        public GamepadHaptic<UnityEngine.InputSystem.Gamepad> Play(
            HapticRequest<UnityEngine.InputSystem.Gamepad> request,
            CancellationToken token = default
        )
        {
            if (IsBusy(request.Device))
                Stop(request.Device);

            long ID = IDCounter++;
            GamepadHaptic<UnityEngine.InputSystem.Gamepad> response =
                new(
                    id: ID,
                    device: request.Device,
                    awaitable: PlayInternal(ID, request, token),
                    parent: this
                );
            StoreHapticEvent(request.Device, response);
            return response;
        }

        public bool IsBusy(UnityEngine.InputSystem.Gamepad gamepad)
        {
            if (
                _activeVibrations.TryGetValue(
                    gamepad,
                    out GamepadHaptic<UnityEngine.InputSystem.Gamepad> val
                )
                || val.ID != InvalidID
            )
                return true;
            return false;
        }

        public bool IsValid(GamepadHaptic<UnityEngine.InputSystem.Gamepad> response)
        {
            if (
                !_activeVibrations.TryGetValue(
                    response.Device,
                    out GamepadHaptic<UnityEngine.InputSystem.Gamepad> val
                )
                || val.ID == InvalidID
                || val.ID != response.ID
            )
                return false;
            return true;
        }

        public void Stop(GamepadHaptic<UnityEngine.InputSystem.Gamepad> response)
        {
            if (!IsValid(response))
                return;
            Stop(response.Device);
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
                !_activeVibrations.TryGetValue(
                    device,
                    out GamepadHaptic<UnityEngine.InputSystem.Gamepad> response
                )
                || response.ID == InvalidID
            )
                return;
            if (!response.InternalAwaitable.IsCompleted)
                response.InternalAwaitable.Cancel();
            StoreHapticEvent(device, EmptyResponse);
        }

        async Awaitable PlayInternal(
            long responseID,
            HapticRequest<UnityEngine.InputSystem.Gamepad> request,
            CancellationToken token
        )
        {
            // Initialize
            long elapsedMs = 0;
            int rumbleIndex = 0;
            long rumbleOffsetMs = 0;
            bool setHapticsForThisIndex = false;
            float previousTimeScale = Time.timeScale;

            while (true)
            {
                long timeThisIndex = request.Clip.gamepadRumble.durationsMs[rumbleIndex];
                long timeWaitedThisIndex = elapsedMs - rumbleOffsetMs;
                long durationToWaitMs = timeThisIndex - timeWaitedThisIndex;

                // Check if we need to increment the rumble index
                if (durationToWaitMs < 0)
                {
                    // Reset set haptics flag
                    setHapticsForThisIndex = false;

                    // Continue through the timeseries data
                    rumbleOffsetMs += request.Clip.gamepadRumble.durationsMs[rumbleIndex++];

                    // Check if we need to loop, or exit
                    if (rumbleIndex == request.Clip.gamepadRumble.durationsMs.Length)
                    {
                        // We've finished
                        if (!request.ShouldLoop)
                        {
                            Stop(request.Device);
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
                    float strength = request.Clip.gamepadRumble.amplitude[rumbleIndex];
                    float amountHigh = request.Clip.gamepadRumble.frequency[rumbleIndex];
                    float amountLow = 1f - amountHigh;
                    float lowFrequencySpeed = strength * motorCrossfadeCurve.Evaluate(amountLow);
                    float highFrequencySpeed = strength * motorCrossfadeCurve.Evaluate(amountHigh);
                    if (request.ApplyTimeScale)
                    {
                        lowFrequencySpeed *= previousTimeScale;
                        highFrequencySpeed *= previousTimeScale;
                    }

                    // Play haptics
                    request.Device.SetMotorSpeeds(lowFrequencySpeed, highFrequencySpeed);
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
                        request.Device,
                        out GamepadHaptic<UnityEngine.InputSystem.Gamepad> response
                    )
                    || response.ID != responseID
                )
                {
                    return;
                }
            }
        }

        void StoreHapticEvent(
            UnityEngine.InputSystem.Gamepad gamepad,
            GamepadHaptic<UnityEngine.InputSystem.Gamepad> response
        )
        {
            _gamepadSet.Add(gamepad);
            _activeVibrations[gamepad] = response;
        }
        #endregion
    }
}
