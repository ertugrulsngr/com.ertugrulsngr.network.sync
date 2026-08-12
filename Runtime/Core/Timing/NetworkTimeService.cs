using System;
using Unity.Netcode;
using NetworkSync.Core.Latency;

namespace NetworkSync.Core.Timing
{
    /// <summary>Network timing used by sync behaviours.</summary>
    public sealed class NetworkTimeService
    {
        /// <summary>NGO's default client server-time buffer in seconds.</summary>
        public const double NgoDefaultServerTimeOffsetSec = 0.05d;

        private readonly TimingSettings _settings;
        private NetworkManager _networkManager;

        public NetworkTimeService(TimingSettings settings)
        {
            _settings = settings;
        }

        /// <summary>Server network time (unbuffered on clients).</summary>
        public NetworkTime ServerTime { get; private set; }

        /// <summary>Time used when sampling the interpolation buffer.</summary>
        public NetworkTime InterpolationTime { get; private set; }

        /// <summary>Estimated server time when a payload sent now would be received.</summary>
        public NetworkTime ServerReceiveTime { get; private set; }

        /// <summary>Tick rate for the active session.</summary>
        public uint TickRate => _networkManager.NetworkTickSystem.TickRate;

        /// <summary>
        /// Raised when the service's server timeline advances by one or more ticks.
        /// </summary>
        public event Action Tick;

        public void OnSessionStart(NetworkManager networkManager)
        {
            _networkManager = networkManager;
            UpdateServerTime();
            UpdateEstimatedTimes(ServerTime, null);
        }

        public void OnSessionDestroyed()
        {
            _networkManager = null;
            ServerTime = default;
            InterpolationTime = default;
            ServerReceiveTime = default;
        }

        public void Update(LatencyMetrics localLatencyMetrics)
        {
            int previousTick = ServerTime.Tick;
            UpdateServerTime();
            int currentTick = ServerTime.Tick;

            NetworkTime cachedServerTime = ServerTime;
            for (int tick = previousTick + 1; tick <= currentTick; tick++)
            {
                ServerTime = new NetworkTime(TickRate, tick, cachedServerTime.TickOffset);
                UpdateEstimatedTimes(ServerTime, localLatencyMetrics);
                Tick?.Invoke();
            }

            ServerTime = cachedServerTime;
            UpdateEstimatedTimes(ServerTime, localLatencyMetrics);
        }

        private void UpdateServerTime()
        {
            double rawServerTimeSec = _networkManager.ServerTime.Time;

            if (!_networkManager.IsServer)
            {
                // NGO delays client ServerTime by a safety buffer; restore unbuffered server time.
                rawServerTimeSec += NgoDefaultServerTimeOffsetSec;
            }

            ServerTime = new NetworkTime(TickRate, rawServerTimeSec);
        }

        private void UpdateEstimatedTimes(NetworkTime serverTime, LatencyMetrics localLatencyMetrics)
        {
            int smoothedRttTicks = localLatencyMetrics != null ? localLatencyMetrics.SmoothedRttTicks : 0;
            int estimatedServerReceiveTick = serverTime.Tick + smoothedRttTicks;

            ServerReceiveTime = new NetworkTime(TickRate, estimatedServerReceiveTick, serverTime.TickOffset);
            InterpolationTime = new NetworkTime(
                TickRate,
                serverTime.Time + (_settings.InterpolationDelayTicks / TickRate));
        }
    }
}
