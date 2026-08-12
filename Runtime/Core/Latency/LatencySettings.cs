using System;
using UnityEngine;

namespace NetworkSync.Core.Latency
{
    /// <summary>Inspector-tunable latency measurement options.</summary>
    [Serializable]
    public class LatencySettings
    {
        [Tooltip("How much a new RTT sample affects the smoothed average.")]
        public double RttSmoothingFactor = 0.125d;

        [Tooltip("How much a sudden RTT change affects variance.")]
        public double RttVarianceSmoothingFactor = 0.25d;

        [Tooltip("Seconds between RTT samples.")]
        public double RttSampleIntervalSec = 0.1d;
    }
}
