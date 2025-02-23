// using System.Collections.Generic;
// using static Studio.ShortSleeve.UnityMetaHaptics.Editor.HapticFile;

namespace Studio.ShortSleeve.UnityMetaHaptics.Editor
{
    // This is not fully implemented. I stopped halfway through when I started to think the original library maybe wasn't written correctly.
    // Anyone is welcome to take a crack at this:
    // https://github.com/Lofelt/NiceVibrations/blob/main/core/datamodel/src/emphasis.rs
    public static class Emphasizer
    {
        // // Render the emphasis of breakpoints into the continuous amplitude signal.
        // //
        // // Some systems like Android and Unity's Gamepad do not have support for
        // // transients, therefore emphasis needs to be simulated by modifying the
        // // continuous amplitude signal instead.
        // public static AmplitudeBreakpoint[] EmphasizeAmplitudeBreakpoints(
        //     AmplitudeBreakpoint[] amplitude
        // )
        // {
        //     // Find next emphasis breakpoint
        //     AmplitudeBreakpoint nextEmphasis = FindNextEmphasizedBreakpoint(amplitude);
        //     AmplitudeBreakpoint prevEmphasis = null;
        //     List<AmplitudeBreakpoint> output = new();
        //     for (int i = 0; i < amplitude.Length; i++)
        //     {
        //         AmplitudeBreakpoint amplitudeBreakpoint = amplitude[i];
        //         // Normal breakpoint
        //         if (amplitudeBreakpoint.emphasis == null)
        //         {
        //             ProcessNormalBreakpoint(
        //                 output,
        //                 amplitudeBreakpoint,
        //                 prevEmphasis,
        //                 nextEmphasis
        //             );
        //         }
        //         // Emphasis breakpoint
        //         else
        //         {
        //             ProcessEmphasisBreakpoint(output, amplitudeBreakpoint, i);
        //             prevEmphasis = nextEmphasis;
        //             nextEmphasis = FindNextEmphasizedBreakpoint(amplitude, i);
        //         }
        //     }

        //     // Return result
        //     return output.ToArray();
        // }

        // static AmplitudeBreakpoint FindNextEmphasizedBreakpoint(
        //     AmplitudeBreakpoint[] amplitude,
        //     int startingIndex = 0
        // )
        // {
        //     for (int i = startingIndex; i < amplitude.Length; i++)
        //     {
        //         AmplitudeBreakpoint amplitudeBreakpoint = amplitude[i];
        //         if (amplitudeBreakpoint.emphasis != null)
        //             return amplitudeBreakpoint;
        //     }
        //     return null;
        // }

        // static void ProcessNormalBreakpoint(
        //     List<AmplitudeBreakpoint> output,
        //     AmplitudeBreakpoint breakpoint,
        //     AmplitudeBreakpoint prevEmphasis,
        //     AmplitudeBreakpoint nextEmphasis
        // )
        // {
        //     bool skipDueToDuckingBefore = false;
        //     if (nextEmphasis != null)
        //     {
        //         double duckingBeforeStart = nextEmphasis.time;
        //         double duckingBeforeEnd = nextEmphasis.time;
        //         if (breakpoint.time >= duckingBeforeStart && breakpoint.time <= duckingBeforeEnd)
        //             skipDueToDuckingBefore = true;
        //     }
        //     bool skipDueToDuckingAfter = false;
        //     if (prevEmphasis != null)
        //     {
        //         double duckingAfterStart = prevEmphasis.time;
        //         double duckingAfterEnd = prevEmphasis.time;
        //         if (breakpoint.time >= duckingAfterStart && breakpoint.time <= duckingAfterEnd)
        //             skipDueToDuckingAfter = true;
        //     }
        //     if (!skipDueToDuckingBefore && !skipDueToDuckingAfter)
        //         output.Add(breakpoint);
        // }

        // static void ProcessEmphasisBreakpoint(
        //     List<AmplitudeBreakpoint> output,
        //     AmplitudeBreakpoint breakpoint,
        //     int index
        // ) { }
    }

    // Direct Play Experiment
    // public class Experiment
    // {
    //     async Awaitable PlayInternalV2(
    //         long responseID,
    //         GamepadHapticRequest request,
    //         CancellationToken token
    //     )
    //     {
    //         // Initialize
    //         AmplitudeBreakpoint[] amplitudePoints = request
    //             .Clip
    //             .dataModel
    //             .signals
    //             .continuous
    //             .envelopes
    //             .amplitude;
    //         FrequencyBreakpoint[] frequencyPoints = request
    //             .Clip
    //             .dataModel
    //             .signals
    //             .continuous
    //             .envelopes
    //             .frequency;
    //         float endTime = (float)amplitudePoints[^1].time;
    //         float elapsed = 0f;
    //         int prevAmplitudeIndex = 0;
    //         int prevFrequencyIndex = 0;
    //         while (true)
    //         {
    //             // Loop if needed
    //             bool didLoop = false;
    //             if (request.ShouldLoop && elapsed > endTime)
    //             {
    //                 elapsed -= endTime;
    //                 didLoop = true;
    //             }

    //             // Determine next amplitude
    //             float nextAmplitude = CalculateNextValue(
    //                 amplitudePoints,
    //                 didLoop,
    //                 elapsed,
    //                 ref prevAmplitudeIndex
    //             );

    //             // Determine next frequency
    //             float nextFrequency = CalculateNextValue(
    //                 frequencyPoints,
    //                 didLoop,
    //                 elapsed,
    //                 ref prevFrequencyIndex
    //             );

    //             // Set Haptics
    //             float amountHigh = nextFrequency;
    //             float amountLow = 1f - amountHigh;
    //             float lowFrequencySpeed = nextAmplitude * motorCrossfadeCurve.Evaluate(amountLow);
    //             float highFrequencySpeed = nextAmplitude * motorCrossfadeCurve.Evaluate(amountHigh);
    //             if (request.ApplyTimeScale)
    //             {
    //                 lowFrequencySpeed *= Time.timeScale;
    //                 highFrequencySpeed *= Time.timeScale;
    //             }
    //             request.Gamepad.SetMotorSpeeds(lowFrequencySpeed, highFrequencySpeed);

    //             // We must continue waiting
    //             if (request.UseFixedTime)
    //             {
    //                 await Awaitable.FixedUpdateAsync(token);
    //                 if (request.ApplyTimeScale)
    //                     elapsed += Time.fixedDeltaTime;
    //                 else
    //                     elapsed += Time.fixedUnscaledDeltaTime;
    //             }
    //             else
    //             {
    //                 await Awaitable.NextFrameAsync(token);
    //                 if (request.ApplyTimeScale)
    //                     elapsed += Time.deltaTime;
    //                 else
    //                     elapsed += Time.unscaledDeltaTime;
    //             }

    //             // Make sure we haven't been cancelled
    //             if (
    //                 !_activeVibrations.TryGetValue(
    //                     request.Gamepad,
    //                     out GamepadHapticResponse response
    //                 )
    //                 || response.ID != responseID
    //             )
    //             {
    //                 return;
    //             }
    //         }
    //     }

    //     float CalculateNextValue(
    //         Breakpoint[] points,
    //         bool didLoop,
    //         float elapsed,
    //         ref int prevIndex
    //     )
    //     {
    //         int nextIndex;
    //         int i = prevIndex;
    //         while (true)
    //         {
    //             // Iterate until we find the next index
    //             float currentTime = (float)points[i].time;
    //             if (currentTime < elapsed)
    //             {
    //                 // Continue interating
    //                 i++;
    //                 continue;
    //             }
    //             else if (currentTime >= elapsed)
    //             {
    //                 if (didLoop)
    //                 {
    //                     // We loop if needed, and continue adding points
    //                     if (++i == points.Length)
    //                     {
    //                         didLoop = false;
    //                         i = 0;
    //                     }
    //                     continue;
    //                 }
    //                 else
    //                 {
    //                     // We found the next index
    //                     nextIndex = i;
    //                     break;
    //                 }
    //             }
    //         }
    //         // Set previous index
    //         if ((nextIndex - 1) < 0)
    //             prevIndex = points.Length - 1;
    //         else
    //             prevIndex = nextIndex - 1;

    //         // Determine next value
    //         float nextValue;
    //         float normalizedDistanceToNextPoint = 0;
    //         if (prevIndex > nextIndex)
    //         {
    //             float nextTime = (float)points[nextIndex].time + (float)points[^1].time;
    //             float prevTime = (float)points[prevIndex].time;
    //             normalizedDistanceToNextPoint = Mathf.InverseLerp(
    //                 prevTime,
    //                 nextTime,
    //                 elapsed + (float)points[^1].time
    //             );
    //         }
    //         else if (prevIndex <= nextIndex)
    //         {
    //             float nextTime = (float)points[nextIndex].time;
    //             float prevTime = (float)points[prevIndex].time;
    //             normalizedDistanceToNextPoint = Mathf.InverseLerp(prevTime, nextTime, elapsed);
    //         }
    //         nextValue = Mathf.Lerp(
    //             (float)points[prevIndex].Value,
    //             (float)points[nextIndex].Value,
    //             normalizedDistanceToNextPoint
    //         );

    //         // Update previous index
    //         prevIndex = nextIndex;
    //         return nextValue;
    //     }
    // }
}
