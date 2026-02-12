using System;
using System.Collections.Generic;
using Studio.ShortSleeve.UnityMetaHaptics.Common;
using UnityEngine;

namespace Studio.ShortSleeve.UnityMetaHaptics.SDL2
{
    /// <summary>
    /// Plays haptic clips using SDL_GameControllerRumble.
    /// Clips are sampled at 25 FPS to prevent Bluetooth buffer saturation.
    /// </summary>
    public class SDL2RumblePlayer : IHapticsPlayer
    {
        const long InvalidID = -1;

        // Rumble duration matches frame interval (40ms at 25 FPS) - exact 1:1 ratio
        // This prevents any packet stacking in Bluetooth buffer
        const uint RumbleDurationMs = 40;

        long _idCounter;
        readonly Dictionary<SDL2GameController, ActivePlayback> _activePlaybacks = new();
        readonly List<SDL2GameController> _toRemove = new();

        class ActivePlayback
        {
            public long ID;
            public HapticClip Clip;
            public float StartTime;
            public bool ShouldLoop;
            public int LastFrameIndex;
        }

        /// <summary>
        /// Starts playback of a haptic clip on the specified controller.
        /// </summary>
        public HapticResponse Play(HapticRequest request, SDL2GameController controller)
        {
            if (request.Clip == null)
                throw new ArgumentNullException(nameof(request.Clip));

            if (controller == null || !controller.IsValid)
            {
                Debug.LogError("SDL2RumblePlayer: Invalid controller");
                return new HapticResponse(InvalidID, request.GamepadDevice, null, this);
            }

            long id = _idCounter++;

            ActivePlayback playback = new()
            {
                ID = id,
                Clip = request.Clip,
                StartTime = Time.time,
                ShouldLoop = request.ShouldLoop,
                LastFrameIndex = -1, // Start before first frame
            };

            // Stop any existing playback on this controller
            if (_activePlaybacks.ContainsKey(controller))
            {
                controller.StopRumble();
            }

            _activePlaybacks[controller] = playback;
            return new HapticResponse(id, request.GamepadDevice, null, this);
        }

        /// <summary>
        /// Updates all active playbacks. Call this from Update() in SDL2HapticsSystem.
        /// Clips are sampled at 25 FPS, so we only send new rumble commands when the frame changes.
        /// </summary>
        public void Update()
        {
            _toRemove.Clear();

            foreach (KeyValuePair<SDL2GameController, ActivePlayback> kvp in _activePlaybacks)
            {
                SDL2GameController controller = kvp.Key;
                ActivePlayback playback = kvp.Value;

                float elapsed = Time.time - playback.StartTime;
                int frameIndex = Mathf.FloorToInt(elapsed * HapticClip.SAMPLE_RATE);

                // Check if clip finished
                if (frameIndex >= playback.Clip.FrameCount)
                {
                    if (playback.ShouldLoop)
                    {
                        // Loop back to start
                        playback.StartTime = Time.time;
                        playback.LastFrameIndex = -1;
                        frameIndex = 0;
                    }
                    else
                    {
                        // Playback finished
                        controller.StopRumble();
                        _toRemove.Add(controller);
                        continue;
                    }
                }

                // Only send rumble command if frame changed (natural throttling at 25 FPS)
                if (frameIndex != playback.LastFrameIndex)
                {
                    RumbleKeyframe frame = playback.Clip.GetFrameAt(frameIndex);
                    ushort lowIntensity = (ushort)(frame.lowFreqAmp * 65535);
                    ushort highIntensity = (ushort)(frame.highFreqAmp * 65535);

                    controller.Rumble(lowIntensity, highIntensity, RumbleDurationMs);
                    playback.LastFrameIndex = frameIndex;
                }
            }

            // Remove finished playbacks
            foreach (SDL2GameController controller in _toRemove)
            {
                _activePlaybacks.Remove(controller);
            }
        }

        public void Stop(HapticResponse response)
        {
            if (!IsValid(response))
                return;

            foreach (KeyValuePair<SDL2GameController, ActivePlayback> kvp in _activePlaybacks)
            {
                if (kvp.Value.ID == response.ID)
                {
                    // Remove from active playbacks to prevent Update() from restarting
                    _activePlaybacks.Remove(kvp.Key);

                    // Stop rumble
                    kvp.Key.StopRumble();
                    return;
                }
            }
        }

        /// <summary>
        /// Stops all active playbacks.
        /// </summary>
        public void StopAll()
        {
            foreach (SDL2GameController controller in _activePlaybacks.Keys)
            {
                controller.StopRumble();
            }
            _activePlaybacks.Clear();
        }

        public bool IsValid(HapticResponse response)
        {
            if (response.ID == InvalidID)
                return false;

            foreach (ActivePlayback playback in _activePlaybacks.Values)
            {
                if (playback.ID == response.ID)
                    return true;
            }

            return false;
        }
    }
}
