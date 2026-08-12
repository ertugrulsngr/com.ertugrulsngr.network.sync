using System;
using UnityEngine;

namespace NetworkSync.Core.Timing
{
    /// <summary>Inspector-tunable timing options for network sync.</summary>
    [Serializable]
    public class TimingSettings
    {
        [Tooltip("Added to server tick when sampling the interpolation buffer. Negative delays sampling.")]
        public double InterpolationDelayTicks = -2d;
    }
}
