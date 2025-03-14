using System;

namespace Studio.ShortSleeve.UnityMetaHaptics.Common
{
    // https://github.com/Lofelt/NiceVibrations/blob/main/core/datamodel/src/v1.rs
    [Serializable]
    public class DataModel
    {
        public Version version;
        public Metadata metadata;
        public Signals signals;

        [Serializable]
        public class Version
        {
            public uint major;
            public uint minor;
            public uint patch;
        }

        [Serializable]
        public class Metadata
        {
            public string editor;
            public string author;
            public string source;
            public string project;
            public string[] tags;
            public string description;
        }

        [Serializable]
        public class Signals
        {
            public SignalContinuous continuous;
        }

        [Serializable]
        public class SignalContinuous
        {
            public Envelopes envelopes;
        }

        [Serializable]
        public class Envelopes
        {
            public AmplitudeBreakpoint[] amplitude;
            public FrequencyBreakpoint[] frequency;
        }

        [Serializable]
        public abstract class Breakpoint
        {
            public double time;
            public abstract double Value { get; }
        }

        [Serializable]
        public class AmplitudeBreakpoint : Breakpoint
        {
            public double amplitude;
            public Emphasis emphasis;
            public override double Value => amplitude;
        }

        [Serializable]
        public class FrequencyBreakpoint : Breakpoint
        {
            public double frequency;
            public override double Value => frequency;
        }

        [Serializable]
        public class Emphasis
        {
            public double amplitude;
            public double frequency;
        }
    }
}
