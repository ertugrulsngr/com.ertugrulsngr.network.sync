using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace NetworkSync.Core.Latency
{
    public sealed class NetworkLatencyService
    {
        private readonly LatencySettings _settings;
        private readonly Dictionary<ulong, LatencyMetrics> _latencyMetrics = new Dictionary<ulong, LatencyMetrics>();

        private NetworkManager _networkManager;
        private double _rttSampleTimer;

        public NetworkLatencyService(LatencySettings settings)
        {
            _settings = settings;
        }

        public IReadOnlyDictionary<ulong, LatencyMetrics> LatencyMetrics => _latencyMetrics;

        /// <summary>Latency metrics for the local client, or null if unavailable.</summary>
        public LatencyMetrics LocalLatencyMetrics
        {
            get
            {
                if (_networkManager == null) return null;
                return _latencyMetrics.TryGetValue(_networkManager.LocalClientId, out LatencyMetrics metrics)
                    ? metrics
                    : null;
            }
        }

        public void OnSessionStart(NetworkManager networkManager)
        {
            _networkManager = networkManager;
            _rttSampleTimer = _settings.RttSampleIntervalSec;
            EnsureLatencyMetrics(_networkManager.LocalClientId);
        }

        public void OnSessionDestroyed()
        {
            _latencyMetrics.Clear();
            _networkManager = null;
            _rttSampleTimer = 0d;
        }

        public void OnClientConnected(ulong clientId)
        {
            EnsureLatencyMetrics(clientId);
        }

        public void OnClientDisconnected(ulong clientId)
        {
            _latencyMetrics.Remove(clientId);
        }

        public void Update()
        {
            _rttSampleTimer += Time.unscaledDeltaTime;
            if (_rttSampleTimer < _settings.RttSampleIntervalSec) return;

            _rttSampleTimer = 0d;
            UpdateAllLatencyMetrics();
        }

        private void UpdateAllLatencyMetrics()
        {
            foreach (KeyValuePair<ulong, LatencyMetrics> pair in _latencyMetrics)
            {
                UpdateLatencyMetrics(pair.Key, pair.Value);
            }
        }

        private void UpdateLatencyMetrics(ulong clientId, LatencyMetrics latencyMetrics)
        {
            NetworkTransport networkTransport = _networkManager.NetworkConfig.NetworkTransport;
            ulong serverClientId = networkTransport.ServerClientId;

            if (clientId == serverClientId) return;

            ulong rttClientId = clientId == _networkManager.LocalClientId
                ? serverClientId
                : clientId;

            double currentRttMs = networkTransport.GetCurrentRtt(rttClientId);
            latencyMetrics.UpdateMetrics(
                currentRttMs,
                _settings.RttSmoothingFactor,
                _settings.RttVarianceSmoothingFactor);
        }

        private void EnsureLatencyMetrics(ulong clientId)
        {
            if (_latencyMetrics.ContainsKey(clientId)) return;

            uint tickRate = _networkManager.NetworkTickSystem.TickRate;
            _latencyMetrics[clientId] = new LatencyMetrics(tickRate);
        }
    }
}
