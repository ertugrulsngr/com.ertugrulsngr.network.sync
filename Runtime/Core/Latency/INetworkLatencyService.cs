using System.Collections.Generic;

namespace NetworkSync.Core.Latency
{
    /// <summary>Read-only latency metrics used by sync behaviours and gameplay code.</summary>
    public interface INetworkLatencyService
    {
        /// <summary>Per-client latency metrics.</summary>
        IReadOnlyDictionary<ulong, LatencyMetrics> LatencyMetrics { get; }

        /// <summary>Latency metrics for the local client, or null if unavailable.</summary>
        LatencyMetrics LocalLatencyMetrics { get; }
    }
}
