using System.Collections.Generic;
using static Studio.ShortSleeve.UnityMetaHaptics.Common.DataModel;

namespace Studio.ShortSleeve.UnityMetaHaptics.Editor
{
    public struct EmphasisParameters
    {
        public double DuckingAmplitude;
        public double EmphasisLengthSeconds;
        public double DuckingAfterDurationSeconds;
        public double DuckingBeforeDurationSeconds;
    }

    // https://github.com/Lofelt/NiceVibrations/blob/main/core/datamodel/src/emphasis.rs
    public static class Emphasizer
    {
        #region Constants
        const double EmphasisAmplitude = 1d;
        #endregion

        // Render the emphasis of breakpoints into the continuous amplitude signal.
        //
        // Some systems like Android and Unity's Gamepad do not have support for
        // transients, therefore emphasis needs to be simulated by modifying the
        // continuous amplitude signal instead.
        public static AmplitudeBreakpoint[] EmphasizeAmplitudeBreakpoints(
            EmphasisParameters parameters,
            AmplitudeBreakpoint[] amplitude
        )
        {
            // Find next emphasis breakpoint
            AmplitudeBreakpoint nextEmphasis = FindNextEmphasizedBreakpoint(amplitude);
            AmplitudeBreakpoint prevEmphasis = null;
            List<AmplitudeBreakpoint> output = new();
            for (int i = 0; i < amplitude.Length; i++)
            {
                AmplitudeBreakpoint amplitudeBreakpoint = amplitude[i];
                // Normal breakpoint
                if (amplitudeBreakpoint.emphasis == null)
                {
                    ProcessNormalBreakpoint(
                        parameters,
                        output,
                        amplitudeBreakpoint,
                        prevEmphasis,
                        nextEmphasis
                    );
                }
                // Emphasis breakpoint
                else
                {
                    ProcessEmphasisBreakpoint(
                        parameters,
                        amplitude,
                        output,
                        amplitudeBreakpoint,
                        i
                    );
                    prevEmphasis = nextEmphasis;
                    nextEmphasis = FindNextEmphasizedBreakpoint(amplitude, i);
                }
            }

            // Return result
            return output.ToArray();
        }

        // A normal breakpoint is either appended to self.result or skipped.
        // It is skipped if the breakpoint is within a ducking area or an emphasis
        // area, i.e. if it is either closely before or closely after a breakpoint
        // with emphasis.
        static void ProcessNormalBreakpoint(
            EmphasisParameters parameters,
            List<AmplitudeBreakpoint> output,
            AmplitudeBreakpoint breakpoint,
            AmplitudeBreakpoint prevEmphasis,
            AmplitudeBreakpoint nextEmphasis
        )
        {
            bool skipDueToDuckingBefore = false;
            if (nextEmphasis != null)
            {
                double duckingBeforeStart = Max(
                    0f,
                    nextEmphasis.time - parameters.DuckingBeforeDurationSeconds
                );
                if (breakpoint.time >= duckingBeforeStart && breakpoint.time <= nextEmphasis.time)
                    skipDueToDuckingBefore = true;
            }

            bool skipDueToDuckingAfter = false;
            if (prevEmphasis != null)
            {
                double emphasisEnd = prevEmphasis.time + parameters.EmphasisLengthSeconds;
                double duckingAfterEnd = emphasisEnd + parameters.DuckingAfterDurationSeconds;
                if (breakpoint.time >= prevEmphasis.time && breakpoint.time <= duckingAfterEnd)
                    skipDueToDuckingAfter = true;
            }
            if (!skipDueToDuckingBefore && !skipDueToDuckingAfter)
                output.Add(breakpoint);
        }

        // For a breakpoint with emphasis, new breakpoints for the ducking areas (before and after)
        // and for the emphasis area are appended to self.result.
        static void ProcessEmphasisBreakpoint(
            EmphasisParameters parameters,
            AmplitudeBreakpoint[] amplitude,
            List<AmplitudeBreakpoint> output,
            AmplitudeBreakpoint breakpoint,
            int index
        )
        {
            ProcessDuckingBeforeArea(parameters, amplitude, output, breakpoint, index);
            ProcessEmphasisAndDuckingAfterArea(parameters, amplitude, output, breakpoint, index);
        }

        // Appends the breakpoints of the ducking before area to self.result.
        //
        // The ducking before area has up to 3 breakpoints:
        // 1. A breakpoint at the start of ducking before, with the amplitude the continuous
        //    amplitude signal would have had normally
        // 2. A breakpoint at the start of ducking before, with amplitude 0
        // 3. A breakpoint at the end of ducking before, with amplitude 0
        static void ProcessDuckingBeforeArea(
            EmphasisParameters parameters,
            AmplitudeBreakpoint[] amplitude,
            List<AmplitudeBreakpoint> output,
            AmplitudeBreakpoint emphasisBreakpoint,
            int emphasisIndex
        )
        {
            double lastTime = output.Count > 0 ? output[^1].time : 0d;
            if (emphasisBreakpoint.time <= lastTime)
                return;

            double duckingBeforeStart = Max(
                lastTime,
                Max(0d, emphasisBreakpoint.time - parameters.DuckingBeforeDurationSeconds)
            );
            int indexBeforeDuckingBefore = FindNextBreakpointBeforeTime(
                amplitude,
                duckingBeforeStart,
                emphasisIndex
            );

            // Breakpoint 1: Start of ducking before
            // The amplitude needs to be interpolated from the breakpoint before and after that point.
            if (indexBeforeDuckingBefore != -1)
            {
                AmplitudeBreakpoint breakpointBeforeDuckingBefore = amplitude[
                    indexBeforeDuckingBefore
                ];
                AmplitudeBreakpoint breakpointInDuckingBefore = amplitude[
                    indexBeforeDuckingBefore + 1
                ];
                AmplitudeBreakpoint breakpointAtDuckingBeforeStart = InterpolateBreakpoints(
                    breakpointBeforeDuckingBefore,
                    breakpointInDuckingBefore,
                    duckingBeforeStart
                );
                output.Add(breakpointAtDuckingBeforeStart);
            }

            // Breakpoint 2: Start of ducking before, amplitude 0
            output.Add(
                new AmplitudeBreakpoint()
                {
                    time = duckingBeforeStart,
                    amplitude = parameters.DuckingAmplitude,
                }
            );

            // Breakpoint 3: End of ducking before, amplitude 0
            output.Add(
                new AmplitudeBreakpoint()
                {
                    time = emphasisBreakpoint.time,
                    amplitude = parameters.DuckingAmplitude,
                }
            );
        }

        // Appends the breakpoints of the emphasis area and the ducking after area
        // to self.result.
        //
        // The emphasis and ducking after areas have up to 5 breakpoints:
        // 1. A breakpoint at the start of emphasis, with amplitude 1.0
        // 2. A breakpoint at the end of emphasis, with amplitude 1.0
        // 3. A breakpoint at the start of ducking after, with amplitude 0.0
        // 4. A breakpoint at the end of ducking after, with amplitude 0.0
        // 5. A breakpoint at the end of ducking after, with the amplitude the continuous
        //    amplitude signal would have had normally
        static void ProcessEmphasisAndDuckingAfterArea(
            EmphasisParameters parameters,
            AmplitudeBreakpoint[] amplitude,
            List<AmplitudeBreakpoint> output,
            AmplitudeBreakpoint emphasisBreakpoint,
            int emphasisIndex
        )
        {
            double lastTime = output.Count > 0 ? output[^1].time : 0d;
            double emphasisStart = Max(lastTime, emphasisBreakpoint.time);
            double emphasisEnd = Max(
                lastTime,
                emphasisBreakpoint.time + parameters.EmphasisLengthSeconds
            );

            // If the emphasis has a duration of 0ms, return right away without adding
            // any emphasis or ducking after.
            // This case can happen if the emphasis falls completely into the ducking
            // after range of the previous emphasis breakpoint.
            if (emphasisEnd - emphasisStart <= double.Epsilon)
                return;

            // Breakpoint 1: Start of emphasis, amplitude 1.0
            output.Add(
                new AmplitudeBreakpoint() { time = emphasisStart, amplitude = EmphasisAmplitude }
            );

            // Breakpoint 2: End of emphasis, amplitude 1.0
            output.Add(
                new AmplitudeBreakpoint() { time = emphasisEnd, amplitude = EmphasisAmplitude }
            );

            // Don't bother adding the ducking after breakpoints if this emphasis breakpoint
            // is the last breakpoint of the clip. The motor will be turned off after the clip
            // ends anyway.
            if (emphasisIndex == amplitude.Length - 1)
                return;

            // Breakpoint 3: Start of ducking after, amplitude = ducking amplitude
            double duckingAfterStart = emphasisEnd;
            output.Add(
                new AmplitudeBreakpoint()
                {
                    time = duckingAfterStart,
                    amplitude = parameters.DuckingAmplitude
                }
            );

            // Breakpoint 4: End of ducking after, amplitude 0
            double duckingAfterEnd = duckingAfterStart + parameters.DuckingAfterDurationSeconds;
            output.Add(
                new AmplitudeBreakpoint() { time = duckingAfterEnd, amplitude = parameters.DuckingAmplitude }
            );

            // Breakpoint 5: End of ducking after
            // The amplitude needs to be interpolated from the breakpoint before and after that point.
            if (emphasisIndex < amplitude.Length - 1)
            {
                int startIndex = emphasisIndex + 1;
                int indexAfterDuckingAfter = FindNextBreakpointBeforeTime(
                    amplitude,
                    duckingAfterEnd,
                    startIndex
                );
                if (indexAfterDuckingAfter != -1 && indexAfterDuckingAfter < amplitude.Length)
                {
                    // Note: FindNextBreakpointBeforeTime returns absolute index, not relative to startIndex
                    AmplitudeBreakpoint breakpointAfterDuckingAfter = amplitude[
                        indexAfterDuckingAfter
                    ];
                    AmplitudeBreakpoint breakpointInEmphasisOrDuckingAfter = amplitude[
                        indexAfterDuckingAfter - 1
                    ];
                    AmplitudeBreakpoint breakpointAtDuckingAfterEnd = InterpolateBreakpoints(
                        breakpointInEmphasisOrDuckingAfter,
                        breakpointAfterDuckingAfter,
                        duckingAfterEnd
                    );
                    output.Add(breakpointAtDuckingAfterEnd);
                }
            }
        }

        #region Helpers
        static double Max(double a, double b) => (a > b) ? a : b;

        static AmplitudeBreakpoint FindNextEmphasizedBreakpoint(
            AmplitudeBreakpoint[] amplitude,
            int startingIndex = 0
        )
        {
            for (int i = startingIndex; i < amplitude.Length; i++)
            {
                AmplitudeBreakpoint amplitudeBreakpoint = amplitude[i];
                if (amplitudeBreakpoint.emphasis != null)
                    return amplitudeBreakpoint;
            }
            return null;
        }

        static int FindNextBreakpointBeforeTime(
            AmplitudeBreakpoint[] amplitude,
            double time,
            int startingIndex = 0
        )
        {
            for (int i = amplitude.Length - 1; i > startingIndex; i--)
            {
                AmplitudeBreakpoint amplitudeBreakpoint = amplitude[i];
                if (amplitudeBreakpoint.time < time)
                    return i;
            }
            return -1;
        }

        static AmplitudeBreakpoint InterpolateBreakpoints(
            AmplitudeBreakpoint breakpointA,
            AmplitudeBreakpoint breakpointB,
            double time
        )
        {
            double amplitudeValue;
            double timeDiff = breakpointB.time - breakpointA.time;
            if (timeDiff == 0d)
            {
                amplitudeValue = breakpointB.amplitude;
            }
            else
            {
                double ampDiff = breakpointB.amplitude - breakpointA.amplitude;
                double factor = (time - breakpointA.time) / timeDiff;
                amplitudeValue = breakpointA.amplitude + ampDiff * factor;
            }

            return new AmplitudeBreakpoint() { time = time, amplitude = amplitudeValue };
        }
        #endregion
    }
}
