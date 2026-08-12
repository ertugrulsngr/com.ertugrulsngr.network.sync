using System;

namespace NetworkSync.Core.Latency
{
    /// <summary>RTT statistics for a single endpoint.</summary>
    public sealed class LatencyMetrics
    {
        private readonly uint _tickRate;
        private bool _hasInitialRtt;

        public double LatestRttMs { get; private set; }
        public double LatestHalfRttMs { get; private set; }
        public int LatestRttTicks { get; private set; }
        public int LatestHalfRttTicks { get; private set; }

        public double RttDeltaMs { get; private set; }
        public int RttDeltaTicks { get; private set; }

        public double SmoothedRttMs { get; private set; }
        public double SmoothedHalfRttMs { get; private set; }
        public double RttVarianceMs { get; private set; }

        public int SmoothedRttTicks { get; private set; }
        public int SmoothedHalfRttTicks { get; private set; }
        public int RttVarianceTicks { get; private set; }

        public LatencyMetrics(uint tickRate)
        {
            _tickRate = tickRate;
        }

        /// <summary>
        /// Updates raw and smoothed RTT metrics from a new sample (RFC 6298–style smoothing).
        /// </summary>
        public void UpdateMetrics(double newRttMs, double rttSmoothingFactor, double rttVarianceSmoothingFactor)
        {
            LatestRttMs = newRttMs;
            LatestHalfRttMs = LatestRttMs / 2d;

            LatestRttTicks = (int)Math.Ceiling((LatestRttMs / 1000d) * _tickRate);
            LatestHalfRttTicks = (int)Math.Ceiling((LatestHalfRttMs / 1000d) * _tickRate);

            if (!_hasInitialRtt)
            {
                SmoothedRttMs = LatestRttMs;
                RttVarianceMs = LatestRttMs / 2d;
                RttDeltaMs = 0d;
                RttDeltaTicks = 0;
                _hasInitialRtt = true;
            }
            else
            {
                RttDeltaMs = Math.Abs(SmoothedRttMs - LatestRttMs);
                RttDeltaTicks = (int)Math.Ceiling((RttDeltaMs / 1000d) * _tickRate);

                RttVarianceMs = (1d - rttVarianceSmoothingFactor) * RttVarianceMs
                    + rttVarianceSmoothingFactor * RttDeltaMs;
                SmoothedRttMs = (1d - rttSmoothingFactor) * SmoothedRttMs
                    + rttSmoothingFactor * LatestRttMs;
            }

            SmoothedHalfRttMs = SmoothedRttMs / 2d;

            RttVarianceTicks = (int)Math.Ceiling((RttVarianceMs / 1000d) * _tickRate);
            SmoothedRttTicks = (int)Math.Ceiling((SmoothedRttMs / 1000d) * _tickRate);
            SmoothedHalfRttTicks = (int)Math.Ceiling((SmoothedHalfRttMs / 1000d) * _tickRate);
        }
    }
}
