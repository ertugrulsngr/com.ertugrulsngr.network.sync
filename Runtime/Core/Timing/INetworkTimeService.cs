using System;
using Unity.Netcode;

namespace NetworkSync.Core.Timing
{
    /// <summary>Read-only network timing used by sync behaviours and gameplay code.</summary>
    public interface INetworkTimeService
    {
        /// <summary>Server network time (unbuffered on clients).</summary>
        NetworkTime ServerTime { get; }

        /// <summary>Time used when sampling the interpolation buffer.</summary>
        NetworkTime InterpolationTime { get; }

        /// <summary>Estimated server time when a payload sent now would be received.</summary>
        NetworkTime ServerReceiveTime { get; }

        /// <summary>Tick rate for the active session.</summary>
        uint TickRate { get; }

        /// <summary>
        /// Raised when the service's server timeline advances by one or more ticks.
        /// </summary>
        event Action Tick;
    }
}
