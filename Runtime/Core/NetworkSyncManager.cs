using System;
using Unity.Netcode;
using UnityEngine;
using NetworkSync.Core.Latency;
using NetworkSync.Core.Timing;

namespace NetworkSync.Core
{
    /// <summary>Scene entry point for Network Sync services and settings.</summary>
    public class NetworkSyncManager : MonoBehaviour, INetworkUpdateSystem
    {
        private const NetworkUpdateStage UpdateStage = NetworkUpdateStage.PreUpdate;

        public static NetworkSyncManager Instance { get; private set; }

        /// <summary>Raised when a network session becomes active (server and/or client started).</summary>
        public event Action SessionCreated;

        /// <summary>Raised when the local network session ends.</summary>
        public event Action SessionDestroyed;

        [SerializeField]
        private TimingSettings _timingSettings = new TimingSettings();

        [SerializeField]
        private LatencySettings _latencySettings = new LatencySettings();

        private NetworkTimeService _timeService;
        private NetworkLatencyService _latencyService;
        private bool _sessionActive;

        /// <summary>Shared timing service for sync behaviours.</summary>
        public INetworkTimeService TimeService => _timeService;

        /// <summary>Shared latency service for sync behaviours.</summary>
        public INetworkLatencyService LatencyService => _latencyService;

        /// <summary>Whether a network session is currently active.</summary>
        public bool IsSessionActive => _sessionActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"{nameof(NetworkSyncManager)} already exists. Destroying duplicate.", this);
                Destroy(this);
                return;
            }

            Instance = this;
            _timeService = new NetworkTimeService(_timingSettings);
            _latencyService = new NetworkLatencyService(_latencySettings);
        }

        private void Start()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            networkManager.OnServerStarted += HandleSessionStart;
            networkManager.OnClientStarted += HandleSessionStart;
            networkManager.OnServerStopped += HandleSessionStop;
            networkManager.OnClientStopped += HandleSessionStop;
        }

        private void OnDestroy()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            UnregisterSessionCallbacks(networkManager);

            if (networkManager != null)
            {
                networkManager.OnServerStarted -= HandleSessionStart;
                networkManager.OnClientStarted -= HandleSessionStart;
                networkManager.OnServerStopped -= HandleSessionStop;
                networkManager.OnClientStopped -= HandleSessionStop;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            if (!_sessionActive) return;

            _latencyService.Update();
            _timeService.Update(_latencyService.LocalLatencyMetrics);
        }

        private void HandleSessionStart()
        {
            if (_sessionActive) return;

            _sessionActive = true;
            NetworkManager networkManager = NetworkManager.Singleton;
            RegisterSessionCallbacks(networkManager);

            _latencyService.OnSessionStart(networkManager);
            _timeService.OnSessionStart(networkManager);

            SessionCreated?.Invoke();
        }

        private void HandleSessionStop(bool isHost)
        {
            if (!_sessionActive) return;

            _sessionActive = false;
            NetworkManager networkManager = NetworkManager.Singleton;
            UnregisterSessionCallbacks(networkManager);

            _timeService.OnSessionDestroyed();
            _latencyService.OnSessionDestroyed();
            SessionDestroyed?.Invoke();
        }

        private void RegisterSessionCallbacks(NetworkManager networkManager)
        {
            UnregisterSessionCallbacks(networkManager);
            networkManager.OnClientConnectedCallback += HandleClientConnected;
            networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            this.RegisterNetworkUpdate(UpdateStage);
        }

        private void UnregisterSessionCallbacks(NetworkManager networkManager)
        {
            this.UnregisterNetworkUpdate(UpdateStage);
            if (networkManager == null) return;
            networkManager.OnClientConnectedCallback -= HandleClientConnected;
            networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        private void HandleClientConnected(ulong clientId)
        {
            _latencyService.OnClientConnected(clientId);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            _latencyService.OnClientDisconnected(clientId);
        }
    }
}
